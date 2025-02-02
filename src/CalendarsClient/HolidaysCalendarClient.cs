using System.Globalization;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using WorkCalendars;

namespace CalendarsClient;

public sealed class HolidaysCalendarClient
{
	private readonly HolidaysProvider.HolidaysProviderClient _client;

	public HolidaysCalendarClient(IOptionsSnapshot<GrpcSettings> settings)
	{
		var channel = GrpcChannel.ForAddress(settings.Value.CalendarsApiUrl);
		_client = new HolidaysProvider.HolidaysProviderClient(channel);
	}

	public async Task<HolidaysModel> GetHolidaysAsync(int year, CountryType country, CancellationToken cancellationToken)
	{
		if (year < 2010)
		{
			throw new ArgumentOutOfRangeException(nameof(year), "Year should be greater than 2010");
		}

		var response = country switch
		{
			CountryType.Russia => await _client.GetHolidaysAsync(new GetHolidaysRequest { Year = year, Country = Country.Ru }, new CallOptions(cancellationToken: cancellationToken)),
			CountryType.Montenegro => await _client.GetHolidaysAsync(new GetHolidaysRequest { Year = year, Country = Country.Me }, new CallOptions(cancellationToken: cancellationToken)),
			_ => throw new ArgumentOutOfRangeException(nameof(country), country, null),
		};

		return new HolidaysModel(
			response.Holidays.Select(h => h.ToDate()).ToArray(),
			response.PreHolidays.Select(h => h.ToDate()).ToArray());
	}
}

file static class DateOnlyExtensions
{
	/// <summary>
	///     Parses for example: 1.1.2024
	/// </summary>
	public static DateOnly ToDate(this string date)
		=> DateOnly.ParseExact(date, "d.M.yyyy", CultureInfo.InvariantCulture);
}

public record HolidaysModel(DateOnly[] Holidays, DateOnly[] PreHolidays);

public enum CountryType
{
	Russia = 0,
	Montenegro = 1,
}