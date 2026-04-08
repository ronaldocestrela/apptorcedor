namespace SocioTorcedor.BuildingBlocks.Domain.ValueObjects;

public sealed record TenantId
{
    private TenantId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(value));

        return new TenantId(value);
    }

    public static TenantId New() => new(Guid.NewGuid());
}
