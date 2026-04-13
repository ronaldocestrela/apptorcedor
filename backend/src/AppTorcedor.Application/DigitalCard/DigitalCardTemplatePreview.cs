namespace AppTorcedor.Application.DigitalCard;

/// <summary>Layout fixo de linhas de preview da carteirinha (paridade B.7 / C.3).</summary>
public static class DigitalCardTemplatePreview
{
    public static IReadOnlyList<string> Build(
        string holderName,
        int version,
        string membershipStatus,
        string? planName,
        string? documentMasked,
        string cardStatus)
    {
        var plan = string.IsNullOrWhiteSpace(planName) ? "Sem plano" : planName;
        var doc = documentMasked ?? "(documento não informado)";
        return
        [
            "Carteirinha digital — layout fixo (B.7)",
            $"Titular: {holderName}",
            $"Versão: {version}",
            $"Status da associação: {membershipStatus}",
            $"Plano: {plan}",
            $"Documento (mascarado): {doc}",
            $"Status da emissão: {cardStatus}",
        ];
    }

    public static string? MaskDocument(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return null;
        var d = document.Trim();
        return d.Length <= 4 ? "****" : $"***{d[^4..]}";
    }
}
