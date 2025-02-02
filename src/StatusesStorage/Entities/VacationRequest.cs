using StatusesStorage.Entities.Approvers;

namespace StatusesStorage.Entities;

public class VacationRequest : EntityBase
{
	public VacationRequest(int employeeId, int projectId, DateTimeOffset startDate, DateTimeOffset endDate)
	{
		UpdateDates(startDate, endDate);
		EmployeeId = employeeId;
		ProjectId = projectId;
		Statuses = [new VacationStatusLogEntry(Id, VacationRequestStatus.Sent)];
	}

	public int EmployeeId { get; private init; }

	public int ProjectId { get; private set; }

	public DateTimeOffset StartDate { get; private set; }

	public DateTimeOffset EndDate { get; private set; }

	public ICollection<VacationStatusLogEntry> Statuses { get; }

	public IReadOnlyCollection<ProjectApprover> ProjectApprovers { get; private init; } = [];

	public IReadOnlyCollection<CustomerApprover> CustomerApprovers { get; private init; } = [];

	public VacationStatusLogEntry CurrentStatus => Statuses.OrderByDescending(entry => entry.DateApply).First();

	public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
	{
		startDate ??= StartDate;
		endDate ??= EndDate;
		if (endDate < startDate)
		{
			throw new InvalidOperationException($"Дата начала отпуска {startDate:d} не может быть раньше даты окончания {endDate:d}");
		}

		StartDate = startDate.Value;
		EndDate = endDate.Value;
	}
}