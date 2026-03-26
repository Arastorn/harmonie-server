using FluentValidation;

namespace Harmonie.Application.Features.Channels.AcknowledgeRead;

public sealed class AcknowledgeReadValidator : AbstractValidator<AcknowledgeReadRequest>
{
    public AcknowledgeReadValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEqual(Guid.Empty)
            .When(x => x.MessageId.HasValue)
            .WithMessage("Message ID must be a valid non-empty GUID when provided");
    }
}
