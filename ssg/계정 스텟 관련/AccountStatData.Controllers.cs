using SingleSquadBattle;

namespace Latecia.Shared
{
    public partial class AccountStatData
    {
        public AccountStatType GetAccountStatType()
        {
            var splits = Stat.ToString().Split('_');
            var statType = System.Enum.Parse<AccountStatType>(splits[0]);
            return statType;
        }

        public bool TryGetAccountBattleStat(out BattleStat battleStat)
        {
            battleStat = default(BattleStat);
            var splits = Stat.ToString().Split('_');
            if (!System.Enum.TryParse<BattleStat>(splits[1], out BattleStat stat)) return false;
            battleStat = stat;
            return true;
        }

        public AccountStatModifierType GetAccountStatModifierType()
        {
            var splits = Stat.ToString().Split('_');
            var modifierType = System.Enum.Parse<AccountStatModifierType>(splits[2]);
            return modifierType;
        }

        public void ApplyAccountStat(float statValue)
        {
            var modifierType = GetAccountStatModifierType();
            switch (modifierType)
            {
                case AccountStatModifierType.BaseStat: ApplyAccountBaseStat(statValue); break;
                case AccountStatModifierType.SimpleMultiplier: ApplyAccountSimpleMultiplierStat(statValue); break;
                case AccountStatModifierType.CompoundMultiplier: ApplyAccountCompoundMultiplierStat(statValue); break;
                case AccountStatModifierType.AdditionalStat: ApplyAccountAdditionalStat(statValue); break;
            }
        }

        void ApplyAccountBaseStat(float statValue) => AccountStatValue += statValue;

        void ApplyAccountSimpleMultiplierStat(float statValue) => AccountStatValue += statValue;

        void ApplyAccountCompoundMultiplierStat(float statValue) => AccountStatValue *= statValue;

        void ApplyAccountAdditionalStat(float statValue) => AccountStatValue += statValue;

        public void RemoveAccountStat(float statValue)
        {
            var modifierType = GetAccountStatModifierType();
            switch (modifierType)
            {
                case AccountStatModifierType.BaseStat: RemoveAccountBaseStat(statValue); break;
                case AccountStatModifierType.SimpleMultiplier: RemoveAccountSimpleMultiplierStat(statValue); break;
                case AccountStatModifierType.CompoundMultiplier: RemoveAccountCompoundMultiplierStat(statValue); break;
                case AccountStatModifierType.AdditionalStat: RemoveAccountAdditionalStat(statValue); break;
            }
        }

        void RemoveAccountBaseStat(float statValue) => AccountStatValue -= statValue;

        void RemoveAccountSimpleMultiplierStat(float statValue) => AccountStatValue -= statValue;

        void RemoveAccountCompoundMultiplierStat(float statValue) => AccountStatValue /= statValue;

        void RemoveAccountAdditionalStat(float statValue) => AccountStatValue -= statValue;
    }
}