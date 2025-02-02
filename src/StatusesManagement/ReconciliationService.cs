using StatusesStorage;
using StatusesStorage.Entities;

namespace StatusesManagement;

public class ReconciliationService
{
	private readonly VacationStatusRepository _statusRepository;
	private readonly UnitOfWork _unitOfWork;

	public ReconciliationService(VacationStatusRepository statusRepository, UnitOfWork unitOfWork)
	{
		_statusRepository = statusRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task StartReconciliationProcessAsync(int employeeId, int projectId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken)
	{
		var request = _statusRepository.AddVacationRequest(employeeId, projectId, startDate, endDate);

		// Отправляем уведомление
		// _senderService.SendNewRequestNotification(request.ProjectId, request.EmployeeId, request.Id);

		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public VacationRequest? GetRequest(int id)
		=> _statusRepository.GetVacationRequest(id);

	public VacationRequest? ApproveByHead(int id, int approverId)
	{
		var request = _statusRepository.GetVacationRequest(id);
		if (request is null)
		{
			return null;
		}

		request.Statuses.Add(new VacationStatusLogEntry(request.Id, VacationRequestStatus.ApprovedByHead));

		return request;
	}

	public VacationRequest? ApproveByCustomer(int id, int approverId)
	{
		var request = _statusRepository.GetVacationRequest(id);
		if (request is null)
		{
			return null;
		}

		request.Statuses.Add(new VacationStatusLogEntry(request.Id, VacationRequestStatus.ApprovedByCustomer));
		return request;
	}

	public Task<VacationRequest[]> GetTeamVacationsAsync(int projectId, CancellationToken cancellationToken)
		=> _statusRepository.GetProjectTeamVacationsAsync(projectId, cancellationToken);

	public async Task CancelRequestAsync(int id)
	{
		var request = _statusRepository.GetVacationRequest(id);
		if (request is not null)
		{
			_statusRepository.RemoveVacationRequest(request);
			await _unitOfWork.SaveChangesAsync(CancellationToken.None);
		}
	}

	public async Task<VacationRequest?> GetEmployeeVacationsAsync(int employeeId, CancellationToken cancellationToken)
	{
		var request = await _statusRepository.GetEmployeeVacationsAsync(employeeId, cancellationToken);
		return request;
	}
}