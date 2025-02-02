using Microsoft.EntityFrameworkCore;
using StatusesStorage.Entities;
using StatusesStorage.Entities.Approvers;

namespace StatusesStorage;

public sealed class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
		Database.EnsureCreated();
	}

	public DbSet<VacationRequest> VacationRequests { get; set; }

	public DbSet<VacationStatusLogEntry> VacationStatusLogEntries { get; set; }

	public DbSet<CustomerApprover> CustomerApprovers { get; set; }

	public DbSet<ProjectApprover> ProjectApprovers { get; set; }

	public DbSet<User> Users { get; set; }
}