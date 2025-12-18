using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatService.Data;
using Dotnet.Grpc;
using System.Security.Claims;

namespace chat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ChatDbContext _db;
    private readonly AuthGrpc.AuthGrpcClient _authClient;

    public ChatController(ChatDbContext db, AuthGrpc.AuthGrpcClient authClient)
    {
        _db = db;
        _authClient = authClient;
    }

    private int GetUserId()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idValue) || !int.TryParse(idValue, out var id))
            throw new UnauthorizedAccessException("Invalid user token.");
        return id;
    }

    // ======================================================
    // 🆕 API: Lấy danh sách tin nhắn theo người (thread)
    // ======================================================
    [HttpGet("messages/grouped")]
    public async Task<IActionResult> GetMessagesGroupedByUser(CancellationToken ct)
    {
        var currentUserId = GetUserId();

        var messages = await _db.Messages
            .AsNoTracking()
            .Where(m => m.sender_id == currentUserId || m.receiver_id == currentUserId)
            .OrderByDescending(m => m.sent_at)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return Ok(new List<object>());

        var grouped = messages.GroupBy(m =>
            m.sender_id == currentUserId ? m.receiver_id : m.sender_id);

        var contactIds = grouped.Select(g => g.Key).Distinct().ToList();
        var contacts = await FetchContactsAsync(contactIds, ct);

        var result = grouped.Select(group =>
        {
            var contactId = group.Key;
            var ordered = group.OrderBy(m => m.sent_at).ToList();
            var lastMsg = ordered.Last();

            var contactInfo = contacts.TryGetValue(contactId, out var info)
                ? info
                : ($"User #{contactId}", BuildInitials($"User #{contactId}"));

            return new
            {
                contactId,
                contactName = contactInfo.Item1,
                avatarInitials = contactInfo.Item2,
                lastMessage = lastMsg.content,
                lastTimestamp = lastMsg.sent_at,
                unreadCount = ordered.Count(m =>
                    m.receiver_id == currentUserId && !m.is_read),
                messages = ordered.Select(m => new
                {
                    id = m.id,
                    senderId = m.sender_id,
                    receiverId = m.receiver_id,
                    content = m.content,
                    timestamp = m.sent_at,
                    isRead = m.is_read
                }).ToList()
            };
        })
        .OrderByDescending(t => t.lastTimestamp)
        .ToList();

        return Ok(result);
    }

    // ======================================================
    // 🔧 Helper: Lấy thông tin user từ AuthService qua gRPC
    // ======================================================
    private async Task<Dictionary<int, (string name, string initials)>> FetchContactsAsync(
        IEnumerable<int> ids, CancellationToken ct)
    {
        var result = new Dictionary<int, (string, string)>();

        foreach (var id in ids)
        {
            try
            {
                var user = await _authClient.GetUserByIdAsync(new UserRequest { Id = id }, cancellationToken: ct);
                var name = ComposeDisplayName(user, id);
                result[id] = (name, BuildInitials(name));
            }
            catch
            {
                result[id] = ($"User #{id}", BuildInitials($"User #{id}"));
            }
        }

        return result;
    }

    private static string ComposeDisplayName(UserReply user, int fallbackId)
    {
        var first = user.Firstname?.Trim();
        var last = user.Lastname?.Trim();
        var parts = new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (parts.Length > 0)
            return string.Join(" ", parts);
        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email!;
        return $"User #{fallbackId}";
    }

    private static string BuildInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpper()
        };
    }
}
