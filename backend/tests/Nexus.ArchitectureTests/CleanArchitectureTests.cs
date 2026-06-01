using NetArchTest.Rules;
using Xunit;
using FluentAssertions;

namespace Nexus.ArchitectureTests;

public class CleanArchitectureTests
{
    private const string ApplicationNamespace = "Nexus.Application";
    private const string InfrastructureNamespace = "Nexus.Infrastructure";
    private const string ApiNamespace = "Nexus.Api";

    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherProjects()
    {
        var result = Types.InAssembly(typeof(Nexus.Domain.IAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAll(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(Nexus.Application.IAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAll(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOnApi()
    {
        var result = Types.InAssembly(typeof(Nexus.Infrastructure.IAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
