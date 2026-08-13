namespace GincanaHud.Api.Domain.Activities;

public sealed class ActivityParticipant
{
	private ActivityParticipant() { }

	public Guid ActivityId { get; private set; }
	public Activity Activity { get; private set; } = null!;
	public Guid UserId { get; private set; }
	public Users.User User { get; private set; } = null!;
	public DateTimeOffset JoinedAt { get; private set; }

	internal static ActivityParticipant Create(Guid activityId, Guid userId)
		=> new()
		{
			ActivityId = activityId,
			UserId = userId,
			JoinedAt = DateTimeOffset.UtcNow
		};
}
