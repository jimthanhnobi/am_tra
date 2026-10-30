namespace TeaPremium.Application.Security;

public static class Permissions
{
    public const string CatalogRead = "Catalog.Read";
    public const string CatalogWrite = "Catalog.Write";
    public const string CatalogPublish = "Catalog.Publish";
    public const string MediaWrite = "Media.Write";
    public const string ContentRead = "Content.Read";
    public const string ContentWrite = "Content.Write";
    public const string ContentPublish = "Content.Publish";
    public const string LeadsRead = "Leads.Read";
    public const string LeadsManage = "Leads.Manage";
    public const string SeoManage = "SEO.Manage";
    public const string SettingsManage = "Settings.Manage";
    public const string UsersManage = "Users.Manage";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        CatalogRead,
        CatalogWrite,
        CatalogPublish,
        ContentRead,
        ContentWrite,
        ContentPublish,
        LeadsRead,
        LeadsManage,
        SeoManage,
        SettingsManage,
        UsersManage,
        MediaWrite,
    ]);
}
