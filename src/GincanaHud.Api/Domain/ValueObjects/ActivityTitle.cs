using ErrorOr;

namespace GincanaHud.Api.Domain.ValueObjects;

public sealed class ActivityTitle : IEquatable<ActivityTitle>
{
	public const int MaxLength = 200;

	private ActivityTitle(string value) => Value = value;

	public string Value { get; }

	public static ErrorOr<ActivityTitle> Create(string? raw)
	{
		var value = raw?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(value))
			return Error.Validation(code: "ActivityTitle.Empty", description: "Title requerido.");
		if (value.Length > MaxLength)
			return Error.Validation(code: "ActivityTitle.TooLong", description: $"Title máx. {MaxLength} caracteres.");
		return new ActivityTitle(value);
	}

	public static ActivityTitle FromPersistence(string value) => new(value);

	public bool Equals(ActivityTitle? other) => other is not null && Value == other.Value;
	public override bool Equals(object? obj) => obj is ActivityTitle other && Equals(other);
	public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
	public override string ToString() => Value;
}
