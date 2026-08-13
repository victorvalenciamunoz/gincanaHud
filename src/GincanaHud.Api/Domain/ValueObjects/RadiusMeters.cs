using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class RadiusMeters : IEquatable<RadiusMeters>
{
	public const double Default = 25;
	public const double Max = 10_000;

	private RadiusMeters(double value) => Value = value;

	public double Value { get; }

	public static ErrorOr<RadiusMeters> Create(double value, double fallback = Default)
	{
		var radius = value > 0 ? value : fallback;
		if (radius <= 0)
			return Error.Validation(code: "Radius.Invalid", description: "Radio debe ser mayor que 0.");
		if (radius > Max)
			return Error.Validation(code: "Radius.TooLarge", description: $"Radio máx. {Max} m.");
		return new RadiusMeters(radius);
	}

	public static RadiusMeters FromPersistence(double value) => new(value);

	public bool Equals(RadiusMeters? other) => other is not null && Value.Equals(other.Value);
	public override bool Equals(object? obj) => obj is RadiusMeters other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode();
}
