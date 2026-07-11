using System.Numerics;

namespace HYDRON.Models
{
    public readonly struct Atomos :
        IComparable<Atomos>,
        IEquatable<Atomos>,
        IFormattable
    {
        private readonly BigInteger _atomos;

        private static readonly BigInteger HyaFactor = new(100);
        private static readonly BigInteger HybFactor = HyaFactor * HyaFactor;
        private static readonly BigInteger HygFactor = HybFactor * HybFactor;
        private static readonly BigInteger HydFactor = HygFactor * HygFactor;
        private static readonly BigInteger HyeFactor = HydFactor * HydFactor;
        private static readonly BigInteger HyzFactor = HyeFactor * HyeFactor;

        public static readonly Atomos Zero = new(BigInteger.Zero);
        public static readonly Atomos One = new(BigInteger.One);

        public Atomos(BigInteger atomos)
        {
            if (atomos < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(atomos), "Atomos amount cannot be negative.");

            _atomos = atomos;
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

        public static Atomos operator +(Atomos left, Atomos right)
            => new(left._atomos + right._atomos);

        public static Atomos operator -(Atomos left, Atomos right) => left._atomos < right._atomos
            ? throw new InvalidOperationException("Subtraction would result in a negative Atomos amount.")
            : new Atomos(left._atomos - right._atomos);

        public static Atomos operator *(Atomos left, BigInteger multiplier) => multiplier < BigInteger.Zero
            ? throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier cannot be negative.")
            : new Atomos(left._atomos * multiplier);

        public static Atomos operator *(Atomos left, int multiplier)
            => left * new BigInteger(multiplier);

        public static Atomos operator /(Atomos left, BigInteger divisor) => divisor <= BigInteger.Zero
            ? throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be greater than zero.")
            : new Atomos(left._atomos / divisor);

        public static Atomos operator /(Atomos left, int divisor)
            => left / new BigInteger(divisor);

        public static Atomos operator %(Atomos left, Atomos right) => right._atomos <= BigInteger.Zero
            ? throw new ArgumentOutOfRangeException(nameof(right), "Divisor must be greater than zero.")
            : new Atomos(left._atomos % right._atomos);

        public static Atomos operator ++(Atomos atomos) => new(atomos._atomos + BigInteger.One);

        public static Atomos operator --(Atomos atomos) => atomos._atomos <= BigInteger.Zero
            ? throw new InvalidOperationException("Decrement would result in a negative Atomos amount.")
            : new Atomos(atomos._atomos - BigInteger.One);

        public static explicit operator double(Atomos atomos) => (double)atomos._atomos;

        public static explicit operator BigInteger(Atomos atomos) => atomos._atomos;

        public Atomos Scale(BigInteger numerator, BigInteger denominator)
        {
            if (numerator < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(numerator), "Numerator cannot be negative.");
            if (denominator <= BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(denominator), "Denominator must be greater than zero.");

            return new Atomos((_atomos * numerator) / denominator);
        }

        public Atomos Scale(int numerator, int denominator)
            => Scale(new BigInteger(numerator), new BigInteger(denominator));

        public static Atomos FromDenomination(double value, Denominations denomination) => value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value), "Denomination value cannot be negative.")
            : new Atomos((BigInteger)(value * (double)GetFactor(denomination)));

        public double ToDenomination(Denominations denomination)
            => Math.Round((double)_atomos / (double)GetFactor(denomination), 2, MidpointRounding.ToZero);

        public Atomos RemainderAfterDenomination(Denominations denomination)
            => new(_atomos % GetFactor(denomination));

        private static BigInteger GetFactor(Denominations denomination) => denomination switch
        {
            Denominations.Hya => HyaFactor,
            Denominations.Hyb => HybFactor,
            Denominations.Hyg => HygFactor,
            Denominations.Hyd => HydFactor,
            Denominations.Hye => HyeFactor,
            Denominations.Hyz => HyzFactor,
            _ => throw new ArgumentOutOfRangeException(nameof(denomination), "Invalid denomination.")
        };

        public string ToString(string? format, IFormatProvider? formatProvider)
            => _atomos.ToString(format, formatProvider);

        public override string ToString() => _atomos.ToString();
    }
}