using StatusesStorage.Entities;

namespace StatusesStorage.UnitTests;

public class VacationRequests
{
	[Fact]
	public void CreateVacationRequest_ShouldInitializeCorrectly()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);

		// Act
		var vacationRequest = new VacationRequest(employeeId, projectId, startDate, endDate);

		// Assert
		Assert.Equal(employeeId, vacationRequest.EmployeeId);
		Assert.Equal(projectId, vacationRequest.ProjectId);
		Assert.Equal(startDate, vacationRequest.StartDate);
		Assert.Equal(endDate, vacationRequest.EndDate);
		Assert.Single(vacationRequest.Statuses);
		Assert.Equal(VacationRequestStatus.Sent, vacationRequest.CurrentStatus.Status);
	}

	[Fact]
	public void UpdateDates_ShouldUpdateStartAndEndDate()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);
		var vacationRequest = new VacationRequest(employeeId, projectId, startDate, endDate);

		var newStartDate = new DateTimeOffset(2023, 12, 5, 0, 0, 0, TimeSpan.Zero);
		var newEndDate = new DateTimeOffset(2023, 12, 15, 0, 0, 0, TimeSpan.Zero);

		// Act
		vacationRequest.UpdateDates(newStartDate, newEndDate);

		// Assert
		Assert.Equal(newStartDate, vacationRequest.StartDate);
		Assert.Equal(newEndDate, vacationRequest.EndDate);
	}

	[Fact]
	public void UpdateDates_ShouldThrowException_WhenEndDateIsBeforeStartDate()
	{
		// Arrange
		var employeeId = 1;
		var projectId = 1;
		var startDate = new DateTimeOffset(2023, 12, 1, 0, 0, 0, TimeSpan.Zero);
		var endDate = new DateTimeOffset(2023, 12, 10, 0, 0, 0, TimeSpan.Zero);
		var vacationRequest = new VacationRequest(employeeId, projectId, startDate, endDate);

		var newStartDate = new DateTimeOffset(2023, 12, 15, 0, 0, 0, TimeSpan.Zero);
		var newEndDate = new DateTimeOffset(2023, 12, 5, 0, 0, 0, TimeSpan.Zero);

		// Act & Assert
		var exception = Assert.Throws<InvalidOperationException>(() => vacationRequest.UpdateDates(newStartDate, newEndDate));
		Assert.Equal($"Дата начала отпуска {newStartDate:d} не может быть раньше даты окончания {newEndDate:d}", exception.Message);
	}
}