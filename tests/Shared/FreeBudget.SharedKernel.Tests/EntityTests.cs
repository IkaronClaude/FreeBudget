using FluentAssertions;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.SharedKernel.Tests;

public class EntityTests
{
    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) => Id = id;
    }

    [Fact]
    public void Entities_with_same_id_are_equal()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }
}
