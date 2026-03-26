namespace Harmonie.Application.Features.Channels.AcknowledgeRead;

public sealed record AcknowledgeReadRequest(Guid? MessageId);
