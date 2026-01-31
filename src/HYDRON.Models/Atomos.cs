using System.Numerics;

namespace Hydron.Models
{
    public readonly struct Atomos :
        IComparable<Atomos>,
        IEquatable<Atomos>,
        IFormattable
    {
        private readonly BigInteger _atomos;

        private const double HyaFactor = 100;
        private const double HybFactor = HyaFactor * HyaFactor;
        private const double HygFactor = HybFactor * HybFactor;
        private const double HydFactor = HygFactor * HygFactor;
        private const double HyeFactor = HydFactor * HydFactor;
        private const double HyzFactor = HyeFactor * HyeFactor;

        public enum Denominations
        {
            Hya = 0,
            Hyb = 1,
            Hyg = 2,
            Hyd = 3,
            Hye = 4,
            Hyz = 5
        }

        public Atomos(BigInteger atomos)
        {
            if (atomos < 0)
                throw new InvalidOperationException("Atomos amount cannot be negative.");

            _atomos = atomos;
        }

        public Atomos(double atomos)
        {
            if (atomos < 0)
                throw new InvalidOperationException("Atomos amount cannot be negative.");

            _atomos = (BigInteger)Math.Ceiling(atomos);
        }

        public int CompareTo(Atomos other) => _atomos.CompareTo(other._atomos);

        public int CompareTo(object? obj) => obj is not Atomos atomos
            ? throw new ArgumentException("Object is not an Atomos.", nameof(obj))
            : CompareTo(atomos);

        public bool Equals(Atomos other) => _atomos.Equals(other._atomos);

        public override bool Equals(object? obj) => obj is Atomos atomos && Equals(atomos);

        public override int GetHashCode() => _atomos.GetHashCode();

        public static bool operator ==(Atomos left, Atomos right) => left.Equals(right);

        public static bool operator !=(Atomos left, Atomos right) => !(left == right);

        public static bool operator <(Atomos left, Atomos right) => left.CompareTo(right) < 0;

        public static bool operator <=(Atomos left, Atomos right) => left.CompareTo(right) <= 0;

        public static bool operator >(Atomos left, Atomos right) => left.CompareTo(right) > 0;

        public static bool operator >=(Atomos left, Atomos right) => left.CompareTo(right) >= 0;

        public static Atomos operator +(Atomos originalAmount, Atomos additionAmount) => new Atomos(originalAmount._atomos + additionAmount._atomos);

        public static Atomos operator -(Atomos originalAmount, Atomos subtractionAmount) => originalAmount._atomos < subtractionAmount._atomos
            ? throw new InvalidOperationException("Atomos originalAmount cannot be negative.")
            : new Atomos(originalAmount._atomos - subtractionAmount._atomos);

        public static Atomos operator *(Atomos originalAmount, double multiplier) => originalAmount._atomos > (BigInteger)double.MaxValue
            ? throw new InvalidOperationException("Multiplication exceeds processable MaxValue limitations.")
            : new Atomos((double)originalAmount._atomos * multiplier);

        public static Atomos operator *(Atomos originalAmount, Atomos multiplier) => new(originalAmount._atomos * multiplier._atomos);

        public static Atomos operator /(Atomos originalAmount, double divisor) => originalAmount._atomos > (BigInteger)double.MaxValue
            ? throw new InvalidOperationException("Division exceeds processable MaxValue limitations.")
            : originalAmount._atomos <= 0 || divisor <= 0
                ? throw new InvalidOperationException("Division by/of zero or negative value is not processable.")
                : new Atomos((double)originalAmount._atomos / divisor);

        public static Atomos operator /(Atomos originalAmount, Atomos divisor) => originalAmount._atomos <= 0 || divisor._atomos <= 0
            ? throw new InvalidOperationException("Division by/of zero or negative value is not processable.")
            : new Atomos(originalAmount._atomos / divisor._atomos);

        public static Atomos operator %(Atomos originalAmount, Atomos divisor) => originalAmount._atomos <= 0 || divisor._atomos <= 0
            ? throw new InvalidOperationException("Division by/of zero or negative value is not processable.")
            : new Atomos(originalAmount._atomos % divisor._atomos);

        public static Atomos operator %(Atomos originalAmount, double divisor) => originalAmount._atomos > (BigInteger)double.MaxValue
            ? throw new InvalidOperationException("Division exceeds processable MaxValue limitations.")
            : originalAmount._atomos <= 0 || divisor <= 0
                ? throw new InvalidOperationException("Division by/of zero or negative value is not processable.")
                : new Atomos((double)originalAmount._atomos % divisor);

        public static Atomos operator ++(Atomos originalAmount) => new (originalAmount._atomos + 1);

        public static Atomos operator --(Atomos originalAmount) => originalAmount._atomos <= 0
            ? throw new InvalidOperationException("Atomos amount cannot be negative.")
            : new Atomos(originalAmount._atomos - 1);

        public string ToString(string? format, IFormatProvider? formatProvider) => _atomos.ToString(format, formatProvider);

        public override string ToString() => _atomos.ToString();

        private static Atomos ConvertFromDenomination(double value, double factor) => new(new BigInteger(Math.Ceiling(value * factor)));

        private static double ConvertToDenomination(Atomos atomos, double factor) => (double)(atomos._atomos / (BigInteger)factor);

        public static Atomos ConvertDenomination(double value, Denominations denomination) => denomination switch
        {
            Denominations.Hya => ConvertFromDenomination(value, HyaFactor),
            Denominations.Hyb => ConvertFromDenomination(value, HybFactor),
            Denominations.Hyg => ConvertFromDenomination(value, HygFactor),
            Denominations.Hyd => ConvertFromDenomination(value, HydFactor),
            Denominations.Hye => ConvertFromDenomination(value, HyeFactor),
            Denominations.Hyz => ConvertFromDenomination(value, HyzFactor),
            _ => throw new ArgumentException("Invalid denomination.", nameof(denomination))
        };

        public static double ConvertAtomos(Atomos value, Denominations denomination) => denomination switch
        {
            Denominations.Hya => ConvertToDenomination(value, HyaFactor),
            Denominations.Hyb => ConvertToDenomination(value, HybFactor),
            Denominations.Hyg => ConvertToDenomination(value, HygFactor),
            Denominations.Hyd => ConvertToDenomination(value, HydFactor),
            Denominations.Hye => ConvertToDenomination(value, HyeFactor),
            Denominations.Hyz => ConvertToDenomination(value, HyzFactor),
            _ => throw new ArgumentException("Invalid denomination.", nameof(denomination))
        };
    }
}
