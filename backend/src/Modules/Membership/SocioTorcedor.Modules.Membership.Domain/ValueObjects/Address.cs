namespace SocioTorcedor.Modules.Membership.Domain.ValueObjects;

public sealed class Address : IEquatable<Address>
{
    // EF Core
    private Address()
    {
        Street = null!;
        Number = null!;
        Neighborhood = null!;
        City = null!;
        State = null!;
        ZipCode = null!;
    }

    private Address(
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state,
        string zipCode)
    {
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string Street { get; private set; } = null!;

    public string Number { get; private set; } = null!;

    public string? Complement { get; private set; }

    public string Neighborhood { get; private set; } = null!;

    public string City { get; private set; } = null!;

    /// <summary>UF, 2 letters.</summary>
    public string State { get; private set; } = null!;

    /// <summary>CEP digits (8).</summary>
    public string ZipCode { get; private set; } = null!;

    public static Address Create(
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state,
        string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required.", nameof(street));
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Number is required.", nameof(number));
        if (string.IsNullOrWhiteSpace(neighborhood))
            throw new ArgumentException("Neighborhood is required.", nameof(neighborhood));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code is required.", nameof(zipCode));

        var uf = state.Trim().ToUpperInvariant();
        if (uf.Length != 2 || !uf.All(char.IsLetter))
            throw new ArgumentException("State must be a 2-letter UF.", nameof(state));

        var zipDigits = new string(zipCode.Where(char.IsDigit).ToArray());
        if (zipDigits.Length != 8)
            throw new ArgumentException("Zip code (CEP) must have 8 digits.", nameof(zipCode));

        return new Address(
            street.Trim(),
            number.Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            neighborhood.Trim(),
            city.Trim(),
            uf,
            zipDigits);
    }

    public bool Equals(Address? other)
    {
        if (other is null)
            return false;

        return Street == other.Street
            && Number == other.Number
            && Complement == other.Complement
            && Neighborhood == other.Neighborhood
            && City == other.City
            && State == other.State
            && ZipCode == other.ZipCode;
    }

    public override bool Equals(object? obj) => obj is Address a && Equals(a);

    public override int GetHashCode() =>
        HashCode.Combine(Street, Number, Complement, Neighborhood, City, State, ZipCode);
}
