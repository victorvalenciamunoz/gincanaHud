using ErrorOr;
using GincanaHud.Api.Domain.ValueObjects;

namespace GincanaHud.Api.Domain.Organizations;

public sealed class Organization
{
	private Organization() { }

	public Guid Id { get; private set; }
	public string Name { get; private set; } = "";
	public DateTimeOffset CreatedAt { get; private set; }
	public List<Activities.Activity> Activities { get; private set; } = [];
	public List<Pois.Poi> Pois { get; private set; } = [];

	public static ErrorOr<Organization> Create(string? name, Guid? id = null)
	{
		var trimmed = name?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(trimmed))
			return Error.Validation(code: "Organization.Name", description: "Nombre de organización requerido.");
		if (trimmed.Length > 200)
			return Error.Validation(code: "Organization.Name", description: "Nombre demasiado largo.");

		return new Organization
		{
			Id = id ?? Guid.NewGuid(),
			Name = trimmed,
			CreatedAt = DateTimeOffset.UtcNow
		};
	}
}
