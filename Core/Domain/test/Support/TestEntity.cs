using RaccoonLand.Core.Domain.Abstractions;

namespace RaccoonLand.Core.Domain.Tests.Support;

internal sealed class TestEntity : Entity<int>
{
    public TestEntity()
    {
    }

    public TestEntity(int id)
        : base(id)
    {
    }
}

internal sealed class OtherTestEntity : Entity<int>
{
    public OtherTestEntity(int id)
        : base(id)
    {
    }
}
