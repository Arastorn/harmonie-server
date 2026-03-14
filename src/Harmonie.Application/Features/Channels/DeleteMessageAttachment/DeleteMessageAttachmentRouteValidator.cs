using FluentValidation;

namespace Harmonie.Application.Features.Channels.DeleteMessageAttachment;

public sealed class DeleteMessageAttachmentRouteValidator : AbstractValidator<DeleteMessageAttachmentRouteRequest>
{
    public DeleteMessageAttachmentRouteValidator()
    {
        RuleFor(x => x.ChannelId)
            .NotEmpty()
            .WithMessage("Channel ID is required")
            .Must(id => Guid.TryParse(id, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Channel ID must be a valid non-empty GUID");

        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("Message ID is required")
            .Must(id => Guid.TryParse(id, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Message ID must be a valid non-empty GUID");

        RuleFor(x => x.AttachmentId)
            .NotEmpty()
            .WithMessage("Attachment ID is required")
            .Must(id => Guid.TryParse(id, out var parsed) && parsed != Guid.Empty)
            .WithMessage("Attachment ID must be a valid non-empty GUID");
    }
}
