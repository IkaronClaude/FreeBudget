using FluentAssertions;
using FreeBudget.Transactions.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FreeBudget.Transactions.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void Application_DI_registration_does_not_throw()
    {
        var services = new ServiceCollection();

        var act = () => services.AddTransactionsApplication();

        act.Should().NotThrow();
    }
}
