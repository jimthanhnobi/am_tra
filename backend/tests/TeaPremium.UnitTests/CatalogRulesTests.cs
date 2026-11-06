using System.Text.Json;
using TeaPremium.Application.Catalog;
using TeaPremium.Domain.Catalog;

namespace TeaPremium.UnitTests;

public sealed class CatalogRulesTests
{
    [Theory]
    [InlineData("am-tu-sa", true)]
    [InlineData("am-tu-sa-123", true)]
    [InlineData("Am-Tu-Sa", false)]
    [InlineData("am_tu_sa", false)]
    [InlineData("-am-tu-sa", false)]
    [InlineData("am--tu-sa", false)]
    [InlineData("", false)]
    public void SlugValidationAcceptsOnlyLowercaseAsciiKebabCase(
        string slug,
        bool expected) =>
        Assert.Equal(expected, CatalogRules.IsValidSlug(slug));

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 48, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 49, false)]
    public void PageValidationEnforcesMaximumPageSize(
        int page,
        int pageSize,
        bool expected) =>
        Assert.Equal(expected, CatalogRules.IsValidPage(page, pageSize));

    [Fact]
    public void JsonValidationRejectsArrays()
    {
        using var objectDocument = JsonDocument.Parse("{}");
        using var arrayDocument = JsonDocument.Parse("[]");

        Assert.True(CatalogRules.IsJsonObject(objectDocument.RootElement));
        Assert.False(CatalogRules.IsJsonObject(arrayDocument.RootElement));
    }

    [Theory]
    [InlineData("Draft", PublicationStatus.Draft)]
    [InlineData("inreview", PublicationStatus.InReview)]
    [InlineData("Published", PublicationStatus.Published)]
    [InlineData("Archived", PublicationStatus.Archived)]
    public void StatusParsingIsCaseInsensitive(
        string value,
        PublicationStatus expected)
    {
        Assert.True(CatalogRules.TryParseStatus(value, out var actual));
        Assert.Equal(expected, actual);
    }
}
