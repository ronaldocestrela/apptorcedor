namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public enum StripeThinWebhookDispatch
{
    SaaS,
    Connect
}

/// <summary>
/// Corpo sintético no formato snapshot (<c>type</c> + <c>data.object</c>) para reutilizar os applicators.
/// </summary>
public sealed record StripeThinSyntheticWebhook(string IdempotencyKey, string EventType, string SnapshotShapedJson);
