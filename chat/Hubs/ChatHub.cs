using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using ChatService.Configurations;
using ChatService.Data;
using Dotnet.Grpc;
using chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatRealtime;

public class ChatHub : Hub
{
  private readonly ChatDbContext _db;
  private readonly AuthGrpc.AuthGrpcClient _authClient;
  private readonly ChatSettings _settings;
  private static readonly ConcurrentDictionary<int, byte> ConnectedAdmins = new();

  public ChatHub(
    ChatDbContext db,
    AuthGrpc.AuthGrpcClient authClient,
    IOptions<ChatSettings> chatOptions)
  {
    _db = db;
    _authClient = authClient;
    _settings = chatOptions.Value ?? throw new ArgumentNullException(nameof(chatOptions));
  }

  public override async Task OnConnectedAsync()
  {
    if (TryGetUserId(out var userId) && IsAdminConnection())
    {
      ConnectedAdmins.TryAdd(userId, 0);
    }

    await base.OnConnectedAsync();
  }

  public override async Task OnDisconnectedAsync(Exception? exception)
  {
    if (TryGetUserId(out var userId) && IsAdminConnection())
    {
      ConnectedAdmins.TryRemove(userId, out _);
    }

    await base.OnDisconnectedAsync(exception);
  }

  public async Task SendMessageByAdmin(int receiverId, string content)
  {
    var senderId = GetUserId();
    var timestamp = DateTimeOffset.UtcNow;

    var message = new Message
    {
      sender_id = senderId,
      receiver_id = receiverId,
      content = content,
      sent_at = timestamp,
      is_read = false
    };

    _db.Messages.Add(message);
    await _db.SaveChangesAsync();
    await Clients.Users(receiverId.ToString())
        .SendAsync("clientMessage", new
        {
          senderId,
          receiverId,
          content,
          timestamp = timestamp.ToString("O"),
          isRead = false
        });
  }
  public async Task SendMessageByClient(string content)
  {
    var senderId = GetUserId();
    var timestamp = DateTimeOffset.UtcNow;
    var receiverId = await ResolveAdminReceiverIdAsync(senderId);

    var message = new Message
    {
      sender_id = senderId,
      receiver_id = receiverId,
      content = content,
      sent_at = timestamp,
      is_read = false
    };

    _db.Messages.Add(message);
    await _db.SaveChangesAsync();
    await Clients.Users(receiverId.ToString())
        .SendAsync("adminMessage", new
        {
          senderId,
          receiverId,
          content,
          timestamp = timestamp.ToString("O"),
          isRead = false
        });
  }

  private int GetUserId()
  {
    if (TryGetUserId(out var id))
      return id;

    throw new HubException("Unauthorized");
  }

  private bool TryGetUserId(out int userId)
  {
    var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out var parsed))
    {
      userId = parsed;
      return true;
    }

    userId = default;
    return false;
  }

  private async Task<int> ResolveAdminReceiverIdAsync(int clientId)
  {
    var lastMessage = await _db.Messages
        .AsNoTracking()
        .Where(m => m.sender_id == clientId || m.receiver_id == clientId)
        .OrderByDescending(m => m.sent_at)
        .FirstOrDefaultAsync();

    if (lastMessage != null)
      return lastMessage.sender_id == clientId
        ? lastMessage.receiver_id
        : lastMessage.sender_id;

    if (!ConnectedAdmins.IsEmpty)
    {
      var onlineAdminId = ConnectedAdmins.Keys.First();
      return onlineAdminId;
    }

    if (_settings.DefaultAdminId <= 0)
      throw new HubException("Default admin is not configured.");

    return _settings.DefaultAdminId;
  }

  private bool IsAdminConnection()
  {
    var roleClaim = Context.User?.FindFirstValue(ClaimTypes.Role);
    return IsAdminRole(roleClaim);
  }

  private static bool IsAdminRole(string? roleValue)
  {
    if (string.IsNullOrWhiteSpace(roleValue))
      return false;

    if (!int.TryParse(roleValue, out var role))
      return false;

    return role >= 0 && role <= 2;
  }
}
