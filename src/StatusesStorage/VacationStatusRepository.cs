using Microsoft.EntityFrameworkCore;
using StatusesStorage.Entities;

namespace StatusesStorage;

public class VacationStatusRepository
{
	private readonly AppDbContext _dbContext;

	public VacationStatusRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public VacationRequest AddVacationRequest(int employeeId, int projectId, DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var request = new VacationRequest(employeeId, projectId, startDate, endDate);
		_dbContext.VacationRequests.Add(request);
		return request;
	}

	public VacationRequest? GetVacationRequest(int id)
		=> _dbContext.VacationRequests.Find(id);

	public Task<VacationRequest[]> GetProjectTeamVacationsAsync(int projectId, CancellationToken cancellationToken)
		=> _dbContext.VacationRequests
			.Where(r => r.ProjectId == projectId)
			.ToArrayAsync(cancellationToken);

	public void RemoveVacationRequest(VacationRequest request)
	{
		_dbContext.VacationRequests.Remove(request);
	}

	public async Task<VacationRequest?> GetEmployeeVacationsAsync(int employeeId, CancellationToken cancellationToken)
	{
		return await _dbContext.VacationRequests
			.Where(r => r.EmployeeId == employeeId)
			.OrderByDescending(r => r.Id)
			.FirstOrDefaultAsync(cancellationToken);
	}
}