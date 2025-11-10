using System.ComponentModel.DataAnnotations.Schema;

namespace chat.Models
{
  [Table("message")]
  public class Message
  {
    [Column("id")]
    public int id { get; set; }

    [Column("sender_id")]
    public int sender_id { get; set; }

    [Column("receiver_id")]
    public int receiver_id { get; set; }

    [Column("content")]
    public string content { get; set; } = null!;

    [Column("sent_at")]
    public DateTimeOffset sent_at { get; set; }

    [Column("is_read")]
    public bool is_read { get; set; }
  }
}
