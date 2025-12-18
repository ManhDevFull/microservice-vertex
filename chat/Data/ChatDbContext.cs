using chat.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Data
{
  public class ChatDbContext : DbContext
  {
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options) { }

    public DbSet<Message> Messages { get; set; } = null!;
  }
}
