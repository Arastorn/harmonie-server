
namespace Harmonie.Application.Interfaces.Messages;

public interface IMessageSearchRepository
{
    Task<SearchGuildMessagesPage> SearchGuildMessagesAsync(
        SearchGuildMessagesQuery query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<SearchConversationMessagesPage> SearchConversationMessagesAsync(
        SearchConversationMessagesQuery query,
        int limit,
        CancellationToken cancellationToken = default);
}
