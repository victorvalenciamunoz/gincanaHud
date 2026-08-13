using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class ClueText : IEquatable<ClueText>
{
	public const int MaxLength = 2000;

	private ClueText(string value) => Value = value;

	public string Value { get; }

	public static ErrorOr<ClueText> Create(string? raw, string fallback = "Sin pista.")
	{
		var value = string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
		if (value.Length > MaxLength)
			return Error.Validation(code: "Clue.TooLong", description: $"Clue máx. {MaxLength} caracteres.");
		return new ClueText(value);
	}

	public static ClueText FromPersistence(string value) => new(value);

	public bool Equals(ClueText? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is ClueText other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
	public override string ToString() => Value;
}
