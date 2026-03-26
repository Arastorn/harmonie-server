using FluentValidation;

namespace Harmonie.Application.Features.Guilds.TransferOwnership;

public sealed class TransferOwnershipValidator : AbstractValidator<TransferOwnershipRequest>
{
    public TransferOwnershipValidator()
    {
        RuleFor(x => x.NewOwnerId)
            .NotEqual(Guid.Empty)
            .WithMessage("New owner ID must be a valid non-empty GUID");
    }
}
