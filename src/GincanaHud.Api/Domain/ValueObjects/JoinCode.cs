using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class JoinCode : IEquatable<JoinCode>
{
	public const int MinLength = 4;
	public const int MaxLength = 12;

	private JoinCode(string value) => Value = value;

	public string Value { get; }

	public static ErrorOr<JoinCode> Create(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return Error.Validation(code: "JoinCode.Empty", description: "Código de unión requerido.");

		var normalized = new string(raw.Trim().ToUpperInvariant()
			.Where(c => char.IsAsciiLetterOrDigit(c)).ToArray());

		if (normalized.Length < MinLength || normalized.Length > MaxLength)
		{
			return Error.Validation(
				code: "JoinCode.Length",
				description: $"El código debe tener entre {MinLength} y {MaxLength} caracteres alfanuméricos.");
		}

		return new JoinCode(normalized);
	}

	public static JoinCode Generate(int length = 6)
	{
		const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
		var chars = new char[length];
		for (var i = 0; i < length; i++)
			chars[i] = alphabet[Random.Shared.Next(alphabet.Length)];
		return new JoinCode(new string(chars));
	}

	public bool Equals(JoinCode? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is JoinCode other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
	public override string ToString() => Value;
}
