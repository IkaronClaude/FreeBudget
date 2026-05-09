using FluentAssertions;
using FreeBudget.Ledger.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Ledger.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void Application_DI_registration_does_not_throw()
    {
        var services = new ServiceCollection();

        var act = () => services.AddLedgerApplication();

        act.Should().NotThrow();
    }
}
