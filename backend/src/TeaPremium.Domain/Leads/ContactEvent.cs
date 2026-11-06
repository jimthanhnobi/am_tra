namespace TeaPremium.Domain.Leads;

public sealed class ContactEvent
{
    private ContactEvent() { }
    public ContactEvent(Guid clientEventId, string eventType, string channel, Guid? productId,
        string pagePath, bool consentState, DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        ClientEventId = clientEventId;
        EventType = eventType;
        Channel = channel;
        ProductId = productId;
        PagePath = pagePath;
        ConsentState = consentState;
        OccurredAt = occurredAt.ToUniversalTime();
    }
    public Guid Id { get; private set; }
    public Guid ClientEventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public Guid? ProductId { get; private set; }
    public string PagePath { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public bool ConsentState { get; private set; }
}
