namespace CalendarsClient;

public sealed record GrpcSettings
{
	public const string SectionName = "Grpc";

	public required string CalendarsApiUrl { get; init; }
}