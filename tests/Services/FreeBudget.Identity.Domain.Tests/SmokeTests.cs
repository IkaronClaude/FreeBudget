using FluentAssertions;

namespace FreeBudget.Identity.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void SharedKernel_domain_primitives_are_accessible()
    {
        var entityType = typeof(SharedKernel.Domain.Entity<>);

        entityType.Should().NotBeNull();
        entityType.Assembly.GetName().Name.Should().Be("FreeBudget.SharedKernel");
    }
}
