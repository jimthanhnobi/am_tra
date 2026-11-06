namespace TeaPremium.IntegrationTests;

public sealed class PostgreSqlFixtureSafetyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("teapremium", false)]
    [InlineData("postgres", false)]
    [InlineData("teapremium_tests", true)]
    [InlineData("teapremium_test_p71", true)]
    public void IntegrationTestsOnlyAcceptDedicatedDatabaseNames(string? databaseName, bool expected) =>
        Assert.Equal(expected, PostgreSqlFixture.IsDedicatedTestDatabase(databaseName));
}
