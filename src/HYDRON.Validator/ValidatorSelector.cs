using HYDRON.Models;

namespace HYDRON.Validator
{
    public static class ValidatorSelector
    {
        public static IReadOnlyList<ValidatorRank> Select(
            IReadOnlyList<ValidatorRank> ranked,
            int count)
        {
            ArgumentNullException.ThrowIfNull(ranked);
            if (count <= 0)
                throw new ArgumentException(
                    "Count must be greater than zero.", nameof(count));
            if (ranked.Count == 0)
                throw new ArgumentException(
                    "Ranked validator list cannot be empty.", nameof(ranked));
            if (count > ranked.Count)
                throw new ArgumentException(
                    $"Requested count ({count}) exceeds available validators ({ranked.Count}).",
                    nameof(count));

            return ranked
                .OrderBy(r => r.Tier)           // Core = 0, Edge = 1
                .ThenByDescending(r => r.FinalRank)
                .Take(count)
                .ToList()
                .AsReadOnly();
        }
    }
}