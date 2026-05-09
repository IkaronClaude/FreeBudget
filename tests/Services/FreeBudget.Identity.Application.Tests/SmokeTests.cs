using FluentAssertions;
using FreeBudget.Identity.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Identity.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void Application_DI_registration_does_not_throw()
    {
        var services = new ServiceCollection();

        var act = () => services.AddIdentityApplication();

        act.Should().NotThrow();
    }
}
