using TeaPremium.Application;
using TeaPremium.Domain;

namespace TeaPremium.UnitTests;

public sealed class ArchitectureDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        var references = typeof(DomainAssemblyReference)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("TeaPremium.Application", references);
        Assert.DoesNotContain("TeaPremium.Infrastructure", references);
        Assert.DoesNotContain("TeaPremium.Api", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        var references = typeof(ApplicationAssemblyReference)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("TeaPremium.Infrastructure", references);
        Assert.DoesNotContain("TeaPremium.Api", references);
    }
}

