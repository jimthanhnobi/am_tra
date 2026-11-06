using System.Net.Mail;
using System.Text.Json;
using TeaPremium.Domain.Leads;

namespace TeaPremium.Application.Leads;

public sealed record CreateLeadInput(string Name, string? Phone, string? Email, string Message,
    Guid? ProductId, string SourcePage, string Channel, IReadOnlyDictionary<string, string>? Utm,
    bool Consent, string? Website);
public sealed record CreateContactEventInput(Guid ClientEventId, string EventType, string Channel,
    Guid? ProductId, string PagePath, bool ConsentState);
public sealed record LeadDto(Guid Id, string Name, string? Phone, string? Email, string Message,
    Guid? ProductId, string SourcePage, string Channel, string Status, string UtmJson,
    DateTimeOffset ConsentAt, DateTimeOffset CreatedAtUtc);
public sealed record LeadPage(IReadOnlyList<LeadDto> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public interface ILeadStore
{
    Task<bool> ProductExistsAsync(Guid id, CancellationToken ct);
    Task SaveLeadAsync(Lead lead, CancellationToken ct);
    Task SaveEventIfNewAsync(ContactEvent contactEvent, CancellationToken ct);
    Task<LeadPage> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<Lead?> FindAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class LeadService(ILeadStore store, TimeProvider clock)
{
    private static readonly HashSet<string> EventChannels = ["zalo", "facebook", "hotline"];
    private static readonly HashSet<string> UtmKeys = ["source", "medium", "campaign", "term", "content"];

    public async Task SubmitAsync(CreateLeadInput input, CancellationToken ct)
    {
        // Honeypot receives the same generic success response as a valid submission.
        if (!string.IsNullOrWhiteSpace(input.Website)) return;
        var name = input.Name?.Trim() ?? "";
        var phone = input.Phone?.Trim();
        var email = input.Email?.Trim();
        var message = input.Message?.Trim() ?? "";
        if (!input.Consent || name.Length is < 2 or > 200 || message.Length is < 5 or > 4000 ||
            (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email)) ||
            phone?.Length > 30 || email?.Length > 254 ||
            (phone is { Length: > 0 } && (!phone.All(c => char.IsDigit(c) || "+() .-".Contains(c)) ||
                phone.Count(char.IsDigit) is < 8 or > 15)) ||
            (email is { Length: > 0 } && !MailAddress.TryCreate(email, out _)) ||
            input.Channel != "web_form" || !ValidPage(input.SourcePage) ||
            input.Utm?.Count > 5 ||
            input.Utm?.Any(pair => !UtmKeys.Contains(pair.Key) || pair.Value is null || pair.Value.Length > 200) == true)
            throw new LeadValidationException("Lead fields or consent are invalid.");
        if (input.ProductId is Guid productId && !await store.ProductExistsAsync(productId, ct))
            throw new LeadValidationException("Product does not exist.");
        var lead = new Lead(name, phone, email, message, input.ProductId, input.SourcePage,
            input.Channel, JsonSerializer.Serialize(input.Utm ?? new Dictionary<string, string>()), clock.GetUtcNow());
        await store.SaveLeadAsync(lead, ct);
    }

    public async Task RecordEventAsync(CreateContactEventInput input, CancellationToken ct)
    {
        if (!input.ConsentState) return;
        if (input.ClientEventId == Guid.Empty || input.EventType != "contact_click" ||
            !EventChannels.Contains(input.Channel) || !ValidPage(input.PagePath))
            throw new LeadValidationException("Contact event is invalid.");
        if (input.ProductId is Guid productId && !await store.ProductExistsAsync(productId, ct))
            throw new LeadValidationException("Product does not exist.");
        await store.SaveEventIfNewAsync(new ContactEvent(input.ClientEventId, input.EventType,
            input.Channel, input.ProductId, input.PagePath, true, clock.GetUtcNow()), ct);
    }

    public Task<LeadPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 50) throw new LeadValidationException("Invalid pagination.");
        return store.ListAsync(page, pageSize, ct);
    }

    public async Task<LeadDto> GetAsync(Guid id, CancellationToken ct) =>
        Map(await store.FindAsync(id, ct) ?? throw new LeadNotFoundException("Lead not found."));

    public async Task<LeadDto> SetStatusAsync(Guid id, string status, CancellationToken ct)
    {
        if (!Enum.TryParse<LeadStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
            throw new LeadValidationException("Invalid lead status.");
        var lead = await store.FindAsync(id, ct) ?? throw new LeadNotFoundException("Lead not found.");
        lead.SetStatus(parsed);
        await store.SaveChangesAsync(ct);
        return Map(lead);
    }

    public static LeadDto Map(Lead x) => new(x.Id, x.Name, x.Phone, x.Email, x.Message,
        x.ProductId, x.SourcePage, x.Channel, x.Status.ToString(), x.UtmJson, x.ConsentAt, x.CreatedAtUtc);

    private static bool ValidPage(string? path) => path is { Length: > 0 and <= 2048 } &&
        path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Contains('\\') && !path.Contains('\n') && !path.Contains('\r');
}

public sealed class LeadValidationException(string message) : Exception(message);
public sealed class LeadNotFoundException(string message) : Exception(message);
