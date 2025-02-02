namespace StatusesStorage.Entities;

public enum VacationRequestStatus
{
	Unknown = 0,
	Sent = 1,
	Read = 2,
	ApprovedByHead = 3,
	ApprovedByCustomer = 4,
	Approved = 5,
}