using HYDRON.Models;

namespace HYDRON.Core
{
    public static class SystemConstants
    {
        public static readonly Atomos TxReward =
            Atomos.FromDenomination(1, Denominations.Hya);

        public static readonly Atomos TransactionBlockReward =
            Atomos.FromDenomination(1, Denominations.Hyb);

        public static readonly Atomos StateBlockReward =
            Atomos.FromDenomination(1, Denominations.Hyg);

        public static readonly Atomos MinimumFee =
            Atomos.FromDenomination(1, Denominations.Hyd);

        public const int TransactionsPerBlock = 100;

        public const int BlocksPerStateBlock = 100;

        public const int ImmutabilityDepth = 100;

        public const double SupermajorityThreshold = 2.0 / 3.0;

        public const decimal HydrogenIonizationEnergyEv = 13.6m;

        public static bool IsSupermajority(int approvals, int total) =>
            total > 0 && approvals >= Math.Ceiling(total * SupermajorityThreshold);
    }
}