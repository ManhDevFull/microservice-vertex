namespace dotnet.Model;

public class EmailVerification
{
  public int id { get; set; }
  public string email { get; set; } = string.Empty;
  public string codehash { get; set; } = string.Empty;
  public string passwordhash { get; set; } = string.Empty;
  public string? firstname { get; set; }
  public string? lastname { get; set; }
  public DateTime expiresat { get; set; }
  public DateTime createdat { get; set; } = DateTime.UtcNow;
  public DateTime? updatedat { get; set; }
  public int attemptcount { get; set; }
  public DateTime? lastsentat { get; set; }
}
