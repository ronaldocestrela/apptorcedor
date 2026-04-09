namespace SocioTorcedor.Modules.Membership.Domain.ValueObjects;

public sealed class Cpf : IEquatable<Cpf>
{
    private Cpf(string digits)
    {
        Digits = digits;
    }

    /// <summary>11-digit CPF (digits only).</summary>
    public string Digits { get; }

    public static Cpf Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("CPF is required.", nameof(raw));

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            throw new ArgumentException("CPF must have 11 digits.", nameof(raw));

        if (IsInvalidSequence(digits))
            throw new ArgumentException("CPF is invalid.", nameof(raw));

        if (!ValidateCheckDigits(digits))
            throw new ArgumentException("CPF check digits are invalid.", nameof(raw));

        return new Cpf(digits);
    }

    public string ToFormattedString() =>
        $"{Digits[..3]}.{Digits.Substring(3, 3)}.{Digits.Substring(6, 3)}-{Digits.Substring(9, 2)}";

    public bool Equals(Cpf? other) => other is not null && Digits == other.Digits;

    public override bool Equals(object? obj) => obj is Cpf c && Equals(c);

    public override int GetHashCode() => Digits.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Digits;

    private static bool IsInvalidSequence(string digits)
    {
        if (digits.Distinct().Count() == 1)
            return true;

        return false;
    }

    private static bool ValidateCheckDigits(string digits)
    {
        var first = ComputeCheckDigit(digits.AsSpan(0, 9));
        if (first != digits[9] - '0')
            return false;

        var second = ComputeCheckDigit(digits.AsSpan(0, 10));
        return second == digits[10] - '0';
    }

    private static int ComputeCheckDigit(ReadOnlySpan<char> baseDigits)
    {
        var length = baseDigits.Length;
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (baseDigits[i] - '0') * (length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
