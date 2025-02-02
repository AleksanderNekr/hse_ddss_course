using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace StatusesStorage.IntegrationTests;

public class VacationStatusRepositoryTests : IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly AppDbContext _dbContext;
	private readonly DbContextOptions<AppDbContext> _dbContextOptions;
	private readonly VacationStatusRepository _repository;

	public VacationStatusRepositoryTests()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		_dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.Options;

		_dbContext = new AppDbContext(_dbContextOptions);
		_dbContext.Database.EnsureCreated();

		_repository = new VacationStatusRepository(_dbContext);
	}

	public void Dispose()
	{
		_dbContext.Dispose();
		_connection.Dispose();
	}

	[Fact]
	public void AddVacationRequest_ShouldAddRequest()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);

		// Act
		var request = _repository.AddVacationRequest(employeeId, projectId, startDate, endDate);
		_dbContext.SaveChanges();

		// Assert
		var retrievedRequest = _dbContext.VacationRequests.Find(request.Id);
		Assert.NotNull(retrievedRequest);
		Assert.Equal(employeeId, retrievedRequest.EmployeeId);
		Assert.Equal(projectId, retrievedRequest.ProjectId);
		Assert.Equal(startDate, retrievedRequest.StartDate);
		Assert.Equal(endDate, retrievedRequest.EndDate);
	}

	[Fact]
	public void GetVacationRequest_ShouldReturnRequest()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);
		var request = _repository.AddVacationRequest(employeeId, projectId, startDate, endDate);
		_dbContext.SaveChanges();

		// Act
		var retrievedRequest = _repository.GetVacationRequest(request.Id);

		// Assert
		Assert.NotNull(retrievedRequest);
		Assert.Equal(request.Id, retrievedRequest.Id);
	}

	[Fact]
	public async Task GetProjectTeamVacationsAsync_ShouldReturnRequests()
	{
		// Arrange
		var projectId = 1;
		_repository.AddVacationRequest(1, projectId, new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero));
		_repository.AddVacationRequest(2, projectId, new DateTimeOffset(2023, 12, 5, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 12, 15, 0, 0, 0, TimeSpan.Zero));
		_dbContext.SaveChanges();

		// Act
		var requests = await _repository.GetProjectTeamVacationsAsync(projectId, CancellationToken.None);

		// Assert
		Assert.Equal(2, requests.Length);
	}

	[Fact]
	public void RemoveVacationRequest_ShouldRemoveRequest()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);
		var request = _repository.AddVacationRequest(employeeId, projectId, startDate, endDate);
		_dbContext.SaveChanges();

		// Act
		_repository.RemoveVacationRequest(request);
		_dbContext.SaveChanges();

		// Assert
		var retrievedRequest = _dbContext.VacationRequests.Find(request.Id);
		Assert.Null(retrievedRequest);
	}

	[Fact]
	public async Task GetEmployeeVacationsAsync_ShouldReturnLatestRequest()
	{
		// Arrange
		var employeeId = 1;
		_repository.AddVacationRequest(employeeId, 1, new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero));
		_repository.AddVacationRequest(employeeId, 2, new DateTimeOffset(2023, 12, 15, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2023, 12, 20, 0, 0, 0, TimeSpan.Zero));
		_dbContext.SaveChanges();

		// Act
		var latestRequest = await _repository.GetEmployeeVacationsAsync(employeeId, CancellationToken.None);

		// Assert
		Assert.NotNull(latestRequest);
		Assert.Equal(2, latestRequest.ProjectId);
	}
}