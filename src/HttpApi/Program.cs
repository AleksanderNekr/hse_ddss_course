using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using CalendarsClient;
using HttpApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StatusesManagement;
using StatusesStorage;
using StatusesStorage.Entities;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
	options =>
	{
		options.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "v1" });

		options.AddSecurityDefinition(
			"Bearer",
			new OpenApiSecurityScheme
			{
				Description = "JWT Authorization header using the Bearer scheme.",
				Name = "Authorization",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
			});

		options.AddSecurityRequirement(
			new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "Bearer",
						},
						Scheme = "oauth2",
						Name = "Bearer",
						In = ParameterLocation.Header,
					},
					Array.Empty<string>()
				},
			});
	});

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(
	optionsBuilder =>
	{
		optionsBuilder.UseSqlite(connectionString);
	});

builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddScoped<VacationStatusRepository>();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<HolidaysCalendarClient>();

builder.Services.AddOptionsWithValidateOnStart<GrpcSettings>()
	.Bind(builder.Configuration.GetSection(GrpcSettings.SectionName))
	.ValidateDataAnnotations();

builder.Services.AddAuthentication(
		options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
	.AddJwtBearer(
		options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = builder.Configuration["Jwt:Issuer"],
				ValidAudience = builder.Configuration["Jwt:Audience"],
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)),
			};
		});
builder.Services.AddAuthorization();

const string headPolicy = "HeadPolicy";
const string customerPolicy = "CustomerPolicy";
const string officeManagerPolicy = "OfficeManagerPolicy";
builder.Services.AddAuthorizationBuilder()
	.AddPolicy(headPolicy, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireClaim(ClaimTypes.Role, Role.Head.ToString()))
	.AddPolicy(customerPolicy, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireClaim(ClaimTypes.Role, Role.Customer.ToString()))
	.AddPolicy(officeManagerPolicy, policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireClaim(ClaimTypes.Role, Role.OfficeManager.ToString()));

var app = builder.Build();

app.Use(
	async (context, next) =>
	{
		await next();

		if (context.Response.StatusCode == 403)
		{
			Console.WriteLine("Forbidden: " + context.User.Identity?.Name);
			Console.WriteLine("Roles: " + string.Join(", ", context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));
		}
	});

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("auth")
	.WithTags("Auth");

auth.MapPost(
	"/register",
	async (
		[FromBody] RegisterRequest registration,
		[FromServices] AppDbContext dbContext,
		[FromServices] IConfiguration configuration) =>
	{
		// Validate the request
		if (!MailAddress.TryCreate(registration.Email, out var email))
		{
			return Extensions.CreateValidationProblem("400", "Invalid email address");
		}

		var emailAddress = email.Address.ToLowerInvariant();

		var usernameNormalized = registration.Username.ToUpperInvariant();

		// Check if the user already exists
		if (dbContext.Users.Any(user => user.Email == emailAddress)
			|| dbContext.Users.Any(user => user.UsernameNormalized == usernameNormalized))
		{
			return Extensions.CreateValidationProblem("400", "User already exists");
		}

		// Create the user
		var user = new User
		{
			Email = emailAddress,
			Username = registration.Username,
			UsernameNormalized = usernameNormalized,
			PasswordHash = registration.Password,
			Role = registration.Role,
		};

		// Hash the password
		user.PasswordHash = new PasswordHasher<User>().HashPassword(user, user.PasswordHash);

		// Create access & refresh tokens
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, user.UsernameNormalized),
			new(ClaimTypes.Email, user.Email),
			new(ClaimTypes.Role, user.Role.ToString()),
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var token = new JwtSecurityToken(
			configuration["Jwt:Issuer"],
			configuration["Jwt:Audience"],
			claims,
			expires: DateTime.Now.AddMinutes(30),
			signingCredentials: credentials);

		var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

		token = new JwtSecurityToken(
			configuration["Jwt:Issuer"],
			configuration["Jwt:Audience"],
			claims,
			expires: DateTime.Now.AddMinutes(60),
			signingCredentials: credentials);

		var refreshToken = new JwtSecurityTokenHandler().WriteToken(token);

		// Add the user to the database
		dbContext.Users.Add(user);
		await dbContext.SaveChangesAsync();

		return Results.Ok(
			new
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
			});
	});

auth.MapPost(
	"/login",
	async (
		[FromBody] LoginRequest login,
		[FromServices] AppDbContext dbContext,
		[FromServices] IConfiguration configuration) =>
	{
		// Find the user
		var usernameNormalized = login.Username.ToUpperInvariant();
		var user = await dbContext.Users.FirstOrDefaultAsync(user => user.UsernameNormalized == usernameNormalized);

		if (user is null)
		{
			return Results.NotFound();
		}

		// Verify the password
		if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, login.Password) == PasswordVerificationResult.Failed)
		{
			return Results.Unauthorized();
		}

		// Create access & refresh tokens
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, user.UsernameNormalized),
			new(ClaimTypes.Email, user.Email),
			new(ClaimTypes.Role, user.Role.ToString()),
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var token = new JwtSecurityToken(
			configuration["Jwt:Issuer"],
			configuration["Jwt:Audience"],
			claims,
			expires: DateTime.Now.AddMinutes(30),
			signingCredentials: credentials);

		var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

		token = new JwtSecurityToken(
			configuration["Jwt:Issuer"],
			configuration["Jwt:Audience"],
			claims,
			expires: DateTime.Now.AddMinutes(60),
			signingCredentials: credentials);

		var refreshToken = new JwtSecurityTokenHandler().WriteToken(token);

		return Results.Ok(
			new
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
			});
	});

auth.MapGet(
		"/user",
		async ([FromServices] AppDbContext dbContext, ClaimsPrincipal user) =>
		{
			var username = user.FindFirst(ClaimTypes.Name)?.Value;
			var email = user.FindFirst(ClaimTypes.Email)?.Value;

			var dbUser = await dbContext.Users.FirstOrDefaultAsync(x => x.UsernameNormalized == username);

			if (dbUser is null || !dbUser.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
			{
				return Results.NotFound();
			}

			return Results.Ok(
				new
				{
					dbUser.Username,
					dbUser.Email,
					dbUser.Role,
				});
		})
	.RequireAuthorization();

auth.MapGet(
	"/roles",
	() => Results.Ok(
		new
		{
			Items = Enum.GetValues<Role>(),
		}));

app.MapGet("/ping", () => "pong")
	.WithTags("Health");

app.MapGet(
	"vacation_requests/{id:int}",
	(HttpContext httpContext, int id, [FromServices] ReconciliationService reconciliationService) =>
	{
		var request = reconciliationService.GetRequest(id);
		if (request is null)
		{
			return Results.NotFound();
		}

		return Results.Ok(
			new
			{
				request.Id,
				request.ProjectId,
				request.EmployeeId,
				request.StartDate,
				request.EndDate,
				request.CurrentStatus,
			});
	}).RequireAuthorization(headPolicy);

app.MapPost(
		"vacation_requests",
		async ([FromServices] ReconciliationService reconciliationService, [FromBody] VacationRequestModel requestModel, CancellationToken cancellationToken) =>
		{
			await reconciliationService.StartReconciliationProcessAsync(requestModel.EmployeeId, requestModel.ProjectId, requestModel.StartDate, requestModel.EndDate, cancellationToken);
			return Results.Created($"/vacation_requests/{requestModel.EmployeeId}", requestModel);
		})
	.RequireAuthorization(officeManagerPolicy);

app.MapPut(
		"vacation_requests/{id:int}",
		async (int id, [FromBody] UpdateVacationRequestModel requestModel, [FromServices] ReconciliationService reconciliationService, [FromServices] UnitOfWork unitOfWork) =>
		{
			var request = reconciliationService.GetRequest(id);
			if (request is null)
			{
				return Results.NotFound();
			}

			request.UpdateDates(requestModel.StartDate, requestModel.EndDate);
			await unitOfWork.SaveChangesAsync(CancellationToken.None);

			return Results.Ok(request);
		})
	.RequireAuthorization(officeManagerPolicy);

app.MapDelete(
		"vacation_requests/{id:int}",
		async (int id, [FromServices] ReconciliationService reconciliationService) =>
		{
			var request = reconciliationService.GetRequest(id);
			if (request is null)
			{
				return Results.NotFound();
			}

			await reconciliationService.CancelRequestAsync(id);

			return Results.NoContent();
		})
	.RequireAuthorization(officeManagerPolicy);

app.MapPost(
		"vacation_requests/{id:int}/approve_statuses",
		(HttpContext httpContext, int id, [FromServices] ReconciliationService reconciliationService, [FromBody] ApprovalRequestModel requestModel) =>
		{
			var request = requestModel.ApproverType switch
			{
				ApproverType.Head => reconciliationService.ApproveByHead(id, requestModel.ApproverId),
				ApproverType.Customer => reconciliationService.ApproveByCustomer(id, requestModel.ApproverId),
				_ => throw new ArgumentOutOfRangeException(),
			};

			return request is null
				? Results.NotFound()
				: Results.Ok(request);
		})
	.RequireAuthorization(headPolicy);

app.MapGet(
		"vacation_requests/{id:int}/approve_statuses",
		(HttpContext httpContext, int id, [FromServices] ReconciliationService reconciliationService) =>
		{
			var request = reconciliationService.GetRequest(id);
			if (request is null)
			{
				return Results.NotFound();
			}

			return Results.Ok(
				new
				{
					request.CurrentStatus,
				});
		})
	.RequireAuthorization(headPolicy);

app.MapGet(
		"vacation_requests/projects/{projectId:int}",
		async (int projectId, [FromServices] ReconciliationService reconciliationService, CancellationToken cancellationToken) =>
		{
			var vacations = await reconciliationService.GetTeamVacationsAsync(projectId, cancellationToken);
			return Results.Ok(vacations);
		})
	.RequireAuthorization(headPolicy);

app.MapGet(
		"vacation_requests/employee/{employeeId:int}",
		async (int employeeId, [FromServices] ReconciliationService reconciliationService, CancellationToken cancellationToken) =>
		{
			var requests = await reconciliationService.GetEmployeeVacationsAsync(employeeId, cancellationToken);
			return Results.Ok(requests);
		})
	.RequireAuthorization(headPolicy);

var holidays = app.MapGroup("holidays").WithTags("Holiday Calendars");
holidays.MapGet(
	"{year:int}",
	async ([Range(2010, 2100)] int year, [FromQuery] CountryType country, [FromServices] HolidaysCalendarClient client) =>
	{
		var response = await client.GetHolidaysAsync(year, country, CancellationToken.None);
		return Results.Ok(response);
	});
app.Run();

public sealed record RegisterRequest([property: EmailAddress] string Email, string Username, string Password, Role Role);

public sealed record LoginRequest(string Username, string Password);

public record ApprovalRequestModel(int ApproverId, ApproverType ApproverType);

public record VacationRequestModel(int ProjectId, int EmployeeId, DateTimeOffset StartDate, DateTimeOffset EndDate);

public record UpdateVacationRequestModel(DateTimeOffset? StartDate, DateTimeOffset? EndDate);

public enum ApproverType
{
	Unknown = 0,
	Head = 1,
	Customer = 2,
}