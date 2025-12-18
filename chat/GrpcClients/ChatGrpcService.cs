using Chat.Grpc;
using ChatService.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Dotnet.Grpc;
using AuthUserRequest = Dotnet.Grpc.UserRequest;

namespace ChatService.Grpc;

public class ChatGrpcService : ChatGrpc.ChatGrpcBase
{
    private readonly ChatDbContext _db;
    private readonly AuthGrpc.AuthGrpcClient _authClient;

    public ChatGrpcService(ChatDbContext db, AuthGrpc.AuthGrpcClient authClient)
    {
        _db = db;
        _authClient = authClient;
    }

    public override async Task<ThreadListReply> ListThreads(Chat.Grpc.UserRequest request, ServerCallContext context)
    {
        if (request.UserId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required."));
        }

        var userId = request.UserId;
        var messages = await _db.Messages
            .AsNoTracking()
            .Where(m => m.sender_id == userId || m.receiver_id == userId)
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
        {
            return new ThreadListReply();
        }

        var grouped = messages
            .GroupBy(m => m.sender_id == userId ? m.receiver_id : m.sender_id)
            .ToList();

        var contactIds = grouped.Select(g => g.Key).Distinct().ToList();
        var contacts = await FetchContactsAsync(contactIds, context.CancellationToken);

        var summaries = grouped
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(m => m.sent_at)
                    .ToList();
                var last = ordered.Last();
                var contactId = group.Key;
                var lastTimestamp = last.sent_at;

                var contactInfo = contacts.TryGetValue(contactId, out var contact)
                    ? contact
                    : ContactInfo.CreateFallback(contactId);

                var unreadCount = ordered.Count(m =>
                    m.sender_id == contactId &&
                    m.receiver_id == userId &&
                    !m.is_read);

                var summary = new ThreadSummary
                {
                    ContactId = contactId,
                    Name = contactInfo.DisplayName,
                    AvatarInitials = contactInfo.Initials,
                    LastSenderId = last.sender_id,
                    LastMessage = last.content ?? string.Empty,
                    LastTimestamp = lastTimestamp.ToString("O"),
                    UnreadCount = unreadCount,
                    ContactEmail = contactInfo.Email
                };

                return (Summary: summary, LastTimestamp: lastTimestamp);
            })
            .OrderByDescending(item => item.LastTimestamp)
            .Select(item => item.Summary)
            .ToList();

        var reply = new ThreadListReply();
        reply.Threads.AddRange(summaries);
        return reply;
    }

    public override async Task<MessageListReply> ListMessages(ThreadRequest request, ServerCallContext context)
    {
        if (request.UserId <= 0 || request.ContactId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId and ContactId are required."));
        }

        var userId = request.UserId;
        var contactId = request.ContactId;

        var conversation = await _db.Messages
            .AsNoTracking()
            .Where(m =>
                (m.sender_id == userId && m.receiver_id == contactId) ||
                (m.sender_id == contactId && m.receiver_id == userId))
            .OrderBy(m => m.sent_at)
            .ToListAsync(context.CancellationToken);

        var reply = new MessageListReply();
        reply.Messages.AddRange(conversation.Select(m => new ChatMessage
        {
            Id = m.id,
            SenderId = m.sender_id,
            ReceiverId = m.receiver_id,
            Content = m.content ?? string.Empty,
            Timestamp = m.sent_at.ToString("O"),
            IsRead = m.is_read
        }));

        return reply;
    }

    public override async Task<MarkReadReply> MarkThreadRead(ThreadRequest request, ServerCallContext context)
    {
        if (request.UserId <= 0 || request.ContactId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId and ContactId are required."));
        }

        var userId = request.UserId;
        var contactId = request.ContactId;

        var affected = await _db.Messages
            .Where(m =>
                m.sender_id == contactId &&
                m.receiver_id == userId &&
                !m.is_read)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(m => m.is_read, _ => true),
                context.CancellationToken);

        return new MarkReadReply { Updated = affected };
    }

    private async Task<Dictionary<int, ContactInfo>> FetchContactsAsync(
        IEnumerable<int> contactIds,
        CancellationToken cancellationToken)
    {
        var tasks = contactIds.Select(async id =>
        {
            try
            {
                var user = await _authClient.GetUserByIdAsync(
                    new AuthUserRequest { Id = id },
                    cancellationToken: cancellationToken);
                var info = ContactInfo.FromUser(id, user);
                return (Id: id, Info: info);
            }
            catch
            {
                return (Id: id, Info: ContactInfo.CreateFallback(id));
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(k => k.Id, v => v.Info);
    }

    private record ContactInfo(string DisplayName, string Initials, string Email)
    {
        public static ContactInfo FromUser(int id, UserReply user)
        {
            var first = user.Firstname?.Trim();
            var last = user.Lastname?.Trim();
            var parts = new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var rawName = parts.Length > 0 ? string.Join(" ", parts) : string.Empty;
            var email = user.Email ?? string.Empty;
            var displayName = string.IsNullOrWhiteSpace(rawName)
                ? (!string.IsNullOrWhiteSpace(email) ? email : $"User #{id}")
                : rawName;
            var initials = BuildInitials(displayName);
            return new ContactInfo(displayName, initials, email);
        }

        public static ContactInfo CreateFallback(int id)
        {
            var display = $"User #{id}";
            return new ContactInfo(display, BuildInitials(display), string.Empty);
        }

        private static string BuildInitials(string name)
        {
            var parts = name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return "?";
            }

            if (parts.Length == 1)
            {
                var segment = parts[0];
                if (segment.Contains('@'))
                {
                    var cleaned = segment.Replace("@", string.Empty);
                    return cleaned.Length >= 2
                        ? cleaned[..2].ToUpperInvariant()
                        : cleaned[..1].ToUpperInvariant();
                }

                return segment.Length >= 2
                    ? segment[..2].ToUpperInvariant()
                    : segment[..1].ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
