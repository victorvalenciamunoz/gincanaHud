using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class Points : IEquatable<Points>
{
	public const int Default = 100;
	public const int Max = 100_000;

	private Points(int value) => Value = value;

	public int Value { get; }

	public static ErrorOr<Points> Create(int value, int fallback = Default)
	{
		var points = value > 0 ? value : fallback;
		if (points <= 0)
			return Error.Validation(code: "Points.Invalid", description: "Puntos deben ser mayor que 0.");
		if (points > Max)
			return Error.Validation(code: "Points.TooLarge", description: $"Puntos máx. {Max}.");
		return new Points(points);
	}

	public static Points FromPersistence(int value) => new(value);

	public bool Equals(Points? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is Points other && Equals(other);
	public override int GetHashCode() => Value;
}
