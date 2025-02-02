using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace HttpApi;

public class Extensions
{
	public static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription)
		=> TypedResults.ValidationProblem(new Dictionary<string, string[]> { { errorCode, [errorDescription] } });

	public static ValidationProblem CreateValidationProblem(IdentityResult result)
	{
		var errorDictionary = new Dictionary<string, string[]>(1);

		foreach (var error in result.Errors)
		{
			string[] newDescriptions;

			if (errorDictionary.TryGetValue(error.Code, out var descriptions))
			{
				newDescriptions = new string[descriptions.Length + 1];
				Array.Copy(descriptions, newDescriptions, descriptions.Length);
				newDescriptions[descriptions.Length] = error.Description;
			}
			else
			{
				newDescriptions = [error.Description];
			}

			errorDictionary[error.Code] = newDescriptions;
		}

		return TypedResults.ValidationProblem(errorDictionary);
	}
}