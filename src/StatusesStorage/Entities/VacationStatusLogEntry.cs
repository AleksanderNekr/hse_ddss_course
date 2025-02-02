namespace StatusesStorage.Entities;

public class VacationStatusLogEntry : EntityBase
{
	public VacationStatusLogEntry(int vacationRequestId, VacationRequestStatus status)
	{
		VacationRequestId = vacationRequestId;
		Status = status;
		DateApply = DateTimeOffset.UtcNow;
	}

	public int VacationRequestId { get; private init; }

	public VacationRequestStatus Status { get; private set; }

	public DateTimeOffset? ApprovalDate { get; private init; }

	public DateTimeOffset? RejectionDate { get; private init; }

	public string? RejectionReason { get; private init; }

	public DateTimeOffset DateApply { get; private init; }
}