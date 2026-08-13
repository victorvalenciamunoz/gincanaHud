using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class PoiName : IEquatable<PoiName>
{
	public const int MaxLength = 200;

	private PoiName(string value) => Value = value;

	public string Value { get; }

	public static ErrorOr<PoiName> Create(string? raw, string fallback = "POI")
	{
		var value = string.IsNullOrWhiteSpace(raw) ? fallback.Trim() : raw.Trim();
		if (string.IsNullOrWhiteSpace(value))
			return Error.Validation(code: "PoiName.Empty", description: "Name requerido.");
		if (value.Length > MaxLength)
			return Error.Validation(code: "PoiName.TooLong", description: $"Name máx. {MaxLength} caracteres.");
		return new PoiName(value);
	}

	public static PoiName FromPersistence(string value) => new(value);

	public bool Equals(PoiName? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is PoiName other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
	public override string ToString() => Value;
}
