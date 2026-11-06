namespace TeaPremium.Domain.Leads;

public enum LeadStatus { New, Contacted, Closed }

public sealed class Lead
{
    private Lead() { }
    public Lead(string name, string? phone, string? email, string message, Guid? productId,
        string sourcePage, string channel, string utmJson, DateTimeOffset consentAt)
    {
        Id = Guid.NewGuid();
        Name = name;
        Phone = phone;
        Email = email;
        Message = message;
        ProductId = productId;
        SourcePage = sourcePage;
        Channel = channel;
        UtmJson = utmJson;
        ConsentAt = consentAt.ToUniversalTime();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Status = LeadStatus.New;
    }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public Guid? ProductId { get; private set; }
    public string SourcePage { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public LeadStatus Status { get; private set; }
    public string UtmJson { get; private set; } = "{}";
    public DateTimeOffset ConsentAt { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public void SetStatus(LeadStatus status) => Status = status;
}
