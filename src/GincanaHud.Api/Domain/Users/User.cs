using ErrorOr;
using GincanaHud.Api.Domain.ValueObjects;

namespace GincanaHud.Api.Domain.Users;

public sealed class User
{
	private User() { }

	public Guid Id { get; private set; }
	public string DisplayName { get; private set; } = "";
	public string? ContactEmail { get; private set; }
	public string? ContactPhone { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }
	public List<Captures.Capture> Captures { get; private set; } = [];
	public List<Activities.ActivityParticipant> Participations { get; private set; } = [];

	public static ErrorOr<User> Register(
		string? displayName,
		string? contactEmail = null,
		string? contactPhone = null,
		Guid? id = null)
	{
		var name = ValueObjects.DisplayName.Create(displayName);
		if (name.IsError)
			return name.Errors;

		var email = NormalizeOptional(contactEmail, 200);
		var phone = NormalizeOptional(contactPhone, 40);

		return new User
		{
			Id = id ?? Guid.NewGuid(),
			DisplayName = name.Value.Value,
			ContactEmail = email,
			ContactPhone = phone,
			CreatedAt = DateTimeOffset.UtcNow
		};
	}

	public void UpdateContact(string? contactEmail, string? contactPhone)
	{
		ContactEmail = NormalizeOptional(contactEmail, 200) ?? ContactEmail;
		ContactPhone = NormalizeOptional(contactPhone, 40) ?? ContactPhone;
	}

	private static string? NormalizeOptional(string? value, int maxLen)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;
		var trimmed = value.Trim();
		return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
	}
}
