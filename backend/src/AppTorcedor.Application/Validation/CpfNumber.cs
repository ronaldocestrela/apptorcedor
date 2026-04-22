using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AppTorcedor.Application.Validation;

/// <summary>CPF (Cadastro de Pessoas Físicas) — validação módulo 11 e normalização (somente 11 dígitos).</summary>
public static class CpfNumber
{
    /// <summary>Extrai dígitos; exige exatamente 11 dígitos, sequência inválida (todos iguais) rejeitada, dígitos verificadores conferidos.</summary>
    public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var sb = new StringBuilder(11);
        foreach (var c in input.AsSpan().Trim())
        {
            if (c is >= '0' and <= '9')
                sb.Append(c);
        }

        if (sb.Length != 11)
            return false;

        var digits = sb.ToString();
        if (IsAllSameDigit(digits))
            return false;

        if (ComputeCheckDigit(digits, 0, 9) != digits[9] - '0')
            return false;
        if (ComputeCheckDigit(digits, 0, 10) != digits[10] - '0')
            return false;

        normalized = digits;
        return true;
    }

    private static bool IsAllSameDigit(string digits)
    {
        var d0 = digits[0];
        for (var i = 1; i < digits.Length; i++)
        {
            if (digits[i] != d0)
                return false;
        }

        return true;
    }

    /// <summary>Calcula o dígito verificador (0–9) para os primeiros <paramref name="count"/> caracteres (9 ou 10).</summary>
    private static int ComputeCheckDigit(string fullEleven, int start, int count)
    {
        var sum = 0;
        var weight = count + 1;
        for (var i = 0; i < count; i++)
        {
            sum += (fullEleven[start + i] - '0') * weight;
            weight--;
        }

        var mod = sum % 11;
        return mod < 2 ? 0 : 11 - mod;
    }
}
