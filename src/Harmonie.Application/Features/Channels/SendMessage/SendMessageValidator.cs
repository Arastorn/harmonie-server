using FluentValidation;
using Harmonie.Domain.ValueObjects;

namespace Harmonie.Application.Features.Channels.SendMessage;

public sealed class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Message content is required")
            .MaximumLength(MessageContent.MaxLength)
            .WithMessage($"Message content cannot exceed {MessageContent.MaxLength} characters");

        RuleForEach(x => x.AttachmentFileIds)
            .Must(id => Guid.TryParse(id, out var parsedId) && parsedId != Guid.Empty)
            .WithMessage("Attachment file IDs must be valid non-empty GUIDs")
            .When(x => x.AttachmentFileIds is not null);

        RuleFor(x => x.AttachmentFileIds)
            .Must(ids => ids is null || ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Count)
            .WithMessage("Attachment file IDs must be unique")
            .When(x => x.AttachmentFileIds is not null);
    }
}
