using FluentAssertions;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.SharedKernel.Tests;

public class AggregateRootTests
{
    private sealed record TestEvent : DomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate(Guid id) => Id = id;

        public void DoSomething() => RaiseDomainEvent(new TestEvent());
    }

    [Fact]
    public void Raising_domain_event_adds_to_collection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DoSomething();

        aggregate.DomainEvents.Should().HaveCount(1);
        aggregate.DomainEvents[0].Should().BeOfType<TestEvent>();
    }

    [Fact]
    public void Clear_domain_events_empties_collection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
