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

        // ── Comparison ────────────────────────────────────────────────────────────────────

        public int CompareTo(Atomos other) => _atomos.CompareTo(other._atomos);

        public int CompareTo(object? obj) => obj is not Atomos atomos
            ? throw new ArgumentException("Object is not an Atomos.", nameof(obj))
            : CompareTo(atomos);

        public bool Equals(Atomos other) => _atomos.Equals(other._atomos);

        public override bool Equals(object? obj) => obj is Atomos atomos && Equals(atomos);

        public override int GetHashCode() => _atomos.GetHashCode();

        // ── Operators ───────────────────────────────────────────────────────────────────

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

        // ── Ratio scaling ──────────────────────────────────────────────────────────────────

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

        // ── Denomination conversions (display only) ───────────────────────────────────────

        /// <summary>
        /// Converts a denomination value to its atomos equivalent.
        /// Uses <see cref="decimal"/> arithmetic (28 significant digits) to avoid the floating-point
        /// precision loss that occurs with <see cref="double"/> for HYD (10^16) and larger factors.
        /// The result is rounded to the nearest integer (banker's rounding).
        /// </summary>
        public static Atomos FromDenomination(decimal value, Denominations denomination)
        {
            if (value < 0m)
                throw new ArgumentOutOfRangeException(nameof(value), "Denomination value cannot be negative.");

            BigInteger factor = GetFactor(denomination);

            // For HYE (10^32) and HYZ (10^64) the factor exceeds decimal's range (~7.9 * 10^28).
            // Fall back to double for those two denominations; precision loss is negligible at
            // that scale since realistic user inputs are tiny (e.g. 0.000001 HYE).
            if (denomination >= Denominations.Hye)
                return new Atomos((BigInteger)Math.Round((double)value * (double)factor, MidpointRounding.ToEven));

            // decimal can represent the factor exactly for HYA..HYD (max 10^16 < 7.9e28).
            decimal factorDecimal = (decimal)factor;
            decimal atomosDecimal = Math.Round(value * factorDecimal, MidpointRounding.ToEven);
            return new Atomos((BigInteger)atomosDecimal);
        }

        /// <summary>
        /// Returns the value in the given denomination as a <see cref="decimal"/>, rounded to
        /// 6 decimal places (truncating, so you never display more than you own).
        /// Use for display purposes only.
        /// </summary>
        public decimal ToDenomination(Denominations denomination)
        {
            BigInteger factor = GetFactor(denomination);

            // For HYE/HYZ the factor exceeds decimal range — use double.
            if (denomination >= Denominations.Hye)
                return (decimal)Math.Round((double)_atomos / (double)factor, 6, MidpointRounding.ToZero);

            decimal factorDecimal = (decimal)factor;
            return Math.Round((decimal)_atomos / factorDecimal, 6, MidpointRounding.ToZero);
        }

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

        // ── Formatting ────────────────────────────────────────────────────────────────────────

        public string ToString(string? format, IFormatProvider? formatProvider)
            => _atomos.ToString(format, formatProvider);

        public override string ToString() => _atomos.ToString();
    }
}
