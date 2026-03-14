using Harmonie.Application.Common;
using Harmonie.Application.Interfaces;
using Harmonie.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Harmonie.Application.Features.Users.DeleteMyAvatar;

public sealed class DeleteMyAvatarHandler
{
    private readonly IUserRepository _userRepository;
    private readonly UploadedFileCleanupService _uploadedFileCleanupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteMyAvatarHandler> _logger;

    public DeleteMyAvatarHandler(
        IUserRepository userRepository,
        UploadedFileCleanupService uploadedFileCleanupService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteMyAvatarHandler> logger)
    {
        _userRepository = userRepository;
        _uploadedFileCleanupService = uploadedFileCleanupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationResponse<bool>> HandleAsync(
        UserId currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DeleteMyAvatar started. UserId={UserId}",
            currentUserId);

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "DeleteMyAvatar failed because user was not found. UserId={UserId}",
                currentUserId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.User.NotFound,
                "User was not found");
        }

        var previousAvatarFileId = user.AvatarFileId;
        if (previousAvatarFileId is null)
        {
            _logger.LogWarning(
                "DeleteMyAvatar failed because no avatar is set. UserId={UserId}",
                currentUserId);

            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Upload.NotFound,
                "User avatar was not found");
        }

        var avatarUpdateResult = user.UpdateAvatarFile(null);
        if (avatarUpdateResult.IsFailure)
        {
            return ApplicationResponse<bool>.Fail(
                ApplicationErrorCodes.Common.DomainRuleViolation,
                avatarUpdateResult.Error ?? "Avatar file is invalid");
        }

        await using (var transaction = await _unitOfWork.BeginAsync(cancellationToken))
        {
            await _userRepository.UpdateProfileAsync(
                new ProfileUpdateParameters(
                    UserId: user.Id,
                    DisplayNameIsSet: false, DisplayName: null,
                    BioIsSet: false, Bio: null,
                    AvatarFileIdIsSet: true, AvatarFileId: null,
                    AvatarColorIsSet: false, AvatarColor: null,
                    AvatarIconIsSet: false, AvatarIcon: null,
                    AvatarBgIsSet: false, AvatarBg: null,
                    ThemeIsSet: false, Theme: null,
                    LanguageIsSet: false, Language: null,
                    UpdatedAtUtc: user.UpdatedAtUtc),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await _uploadedFileCleanupService.DeleteIfExistsAsync(previousAvatarFileId, cancellationToken);

        _logger.LogInformation(
            "DeleteMyAvatar succeeded. UserId={UserId}, DeletedAvatarFileId={AvatarFileId}",
            currentUserId,
            previousAvatarFileId);

        return ApplicationResponse<bool>.Ok(true);
    }
}
