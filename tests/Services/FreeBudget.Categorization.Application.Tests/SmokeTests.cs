using FluentAssertions;
using FreeBudget.Categorization.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Categorization.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void Application_DI_registration_does_not_throw()
    {
        var services = new ServiceCollection();

        var act = () => services.AddCategorizationApplication();

        act.Should().NotThrow();
    }
}
