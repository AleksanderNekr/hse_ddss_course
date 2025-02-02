namespace StatusesStorage.Entities.Approvers;

public class Approver : EntityBase
{
	public required string Name { get; init; }

	public int ProjectId { get; private init; }
}