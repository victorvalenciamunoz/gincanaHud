using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class DisplayName : IEquatable<DisplayName>
{
	public const int MaxLength = 100;

	private DisplayName(string value) => Value = value;

	public string Value { get; }

	public static ErrorOr<DisplayName> Create(string? raw)
	{
		var value = raw?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(value))
			return Error.Validation(code: "DisplayName.Empty", description: "DisplayName requerido.");
		if (value.Length > MaxLength)
			return Error.Validation(code: "DisplayName.TooLong", description: $"DisplayName máx. {MaxLength} caracteres.");
		return new DisplayName(value);
	}

	public static DisplayName FromPersistence(string value) => new(value);

	public bool Equals(DisplayName? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is DisplayName other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
	public override string ToString() => Value;
}
