using System.Numerics;

namespace Hydron.Models
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

        public Atomos(BigInteger atomos)
        {
            if (atomos < 0)
                throw new InvalidOperationException("Atomos amount cannot be negative.");

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

        public string ToString(string? format, IFormatProvider? formatProvider) => _atomos.ToString(format, formatProvider);

        public override string ToString() => _atomos.ToString();
    }
}
