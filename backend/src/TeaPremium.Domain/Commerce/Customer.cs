namespace TeaPremium.Domain.Commerce;

public sealed class Customer
{
    private Customer() { }

    public Customer(string name, string phone, string normalizedPhone, string origin,
        string acquisitionSource, bool hadPriorPurchase, string[] tags)
    {
        Id = Guid.NewGuid();
        Name = name;
        Phone = phone;
        NormalizedPhone = normalizedPhone;
        Origin = origin;
        AcquisitionSource = acquisitionSource;
        HadPriorPurchase = hadPriorPurchase;
        Tags = tags;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string NormalizedPhone { get; private set; } = "";
    public string Origin { get; private set; } = "new";
    public string AcquisitionSource { get; private set; } = "unknown";
    public bool HadPriorPurchase { get; private set; }
    public string[] Tags { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Update(string name, string[] tags, bool hadPriorPurchase)
    {
        Name = name;
        Tags = tags;
        // Explicit true is irreversible: never erase a known historical purchase accidentally.
        HadPriorPurchase |= hadPriorPurchase;
    }
}

public sealed class CustomerInteraction
{
    private CustomerInteraction() { }

    public CustomerInteraction(Guid customerId, string channel, string kind, string notes)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Channel = channel;
        Kind = kind;
        Notes = notes;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Channel { get; private set; } = "";
    public string Kind { get; private set; } = "";
    public string Notes { get; private set; } = "";
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
