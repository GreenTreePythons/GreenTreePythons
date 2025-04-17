using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using SingleSquadBattle;

public static class StatCalculatorUtility
{
    public static float CalculateBattleStatWithAdditionalStats(CharacterData characterData, BattleStat battleStat, List<AccountStatData> accountStatDatas)
    {
        TotalStatValueByModifiers statModifiers = new();
        var awakeningGrowthProto = ProtoData.Current.CharacterGrowthAwakeningInformations.Get(characterData.GetProto().EvolutionBase);

        ApplyAwakeningStats(awakeningGrowthProto, characterData.AwakeningGrade, battleStat, ref statModifiers);
        ApplySpecificityStats(characterData, ref statModifiers);
        ApplyAccountStats(accountStatDatas, battleStat, ref statModifiers);

        return CalculateTotalBattleStat(characterData, battleStat, statModifiers);
    }

    public static float CalculateBattleStatWithAdditionalStats(CharacterDataSnapshot characterDataSnapshot, BattleStat battleStat, List<AccountStatData> accountStatDatas)
    {
        TotalStatValueByModifiers statModifiers = new();
        var characterProto = ProtoData.Current.Characters.Get(characterDataSnapshot.ProtoId);
        var awakeningGrowthProto = ProtoData.Current.CharacterGrowthAwakeningInformations.Get(characterProto.EvolutionBase);

        var prevValues = characterDataSnapshot.AdditionalStats.GetValueOrDefault(battleStat);
        if (prevValues != null)
        {
            foreach (var prevValue in prevValues)
            {
                ApplyStat(prevValue.Key, prevValue.Value, ref statModifiers);
            }
        }

        ApplyAwakeningStats(awakeningGrowthProto, characterDataSnapshot.AwakenGrade, battleStat, ref statModifiers);

        ApplyAccountStats(accountStatDatas, battleStat, ref statModifiers);

        return CalculateTotalBattleStat(characterDataSnapshot, battleStat, statModifiers);
    }

    private static Dictionary<AccountStatModifierType, float> InitializeStatInfos() => new()
    {
        { AccountStatModifierType.BaseStat, 0.0f },
        { AccountStatModifierType.SimpleMultiplier, 1.0f },
        { AccountStatModifierType.CompoundMultiplier, 1.0f },
        { AccountStatModifierType.AdditionalStat, 0.0f }
    };

    public static void AddOrUpdateStats(Dictionary<BattleStat, Dictionary<AccountStatModifierType, float>> additionalStats,
                                     BattleStat battleStat, AccountStatData accountStatData)
    {
        if (!additionalStats.TryGetValue(battleStat, out var statInfos))
        {
            statInfos = InitializeStatInfos();
            additionalStats.Add(battleStat, statInfos);
        }
        ModifyStatInfos(statInfos, accountStatData.GetAccountStatModifierType(), accountStatData.AccountStatValue);
    }

    public static void AddOrUpdateStats(Dictionary<BattleStat, Dictionary<AccountStatModifierType, float>> additionalStats,
                                         BattleStat battleStat, AccountStatModifierType modifierType, float statValue)
    {
        if (!additionalStats.TryGetValue(battleStat, out var statInfos))
        {
            statInfos = InitializeStatInfos();
            additionalStats.Add(battleStat, statInfos);
        }
        ModifyStatInfos(statInfos, modifierType, statValue);
    }

    private static void ModifyStatInfos(Dictionary<AccountStatModifierType, float> statInfos, AccountStatModifierType modifierType, float statValue)
    {
        switch (modifierType)
        {
            case AccountStatModifierType.BaseStat:
            case AccountStatModifierType.AdditionalStat:
                statInfos[modifierType] += statValue;
                break;

            case AccountStatModifierType.SimpleMultiplier:
                statInfos[AccountStatModifierType.SimpleMultiplier] += statValue;
                break;

            case AccountStatModifierType.CompoundMultiplier:
                statInfos[AccountStatModifierType.CompoundMultiplier] *= statValue;
                break;
        }
    }

    private static void ApplySpecificityStats(CharacterData characterData, ref TotalStatValueByModifiers statModifiers)
    {
        foreach (var activeSpecificity in characterData.ActiveSpecificities)
        {
            var specificityStat = ProtoData.Current.CharacterGrowthSpecificities.GetSpecificityStat(activeSpecificity.NodeId, characterData.GetProto().EvolutionBase, activeSpecificity.Level);
            if (specificityStat.stat == AccountStat.None) continue;
            if (specificityStat.statValue == 0) continue;
            var statInfo = specificityStat.stat.SplitAccountStat();
            ApplyStat(statInfo.modifierType, specificityStat.statValue, ref statModifiers);
        }
    }

    private static void ApplyAwakeningStats(AwakeningInformationCharacterGrowthProto awakeningGrowthProto, int awakenGrade, BattleStat battleStat, ref TotalStatValueByModifiers statModifiers)
    {
        foreach (var awakeningGrowthInfo in awakeningGrowthProto.AwakeningInformations)
        {
            if (awakeningGrowthInfo.Grade > awakenGrade) continue;
            if (awakeningGrowthInfo.AccountStatInfo == null) continue;
            if (awakeningGrowthInfo.AccountStatInfo.BattleStat != battleStat) continue;
            ApplyStat(awakeningGrowthInfo.AccountStatInfo.BattleStatModifierType, awakeningGrowthInfo.AccountStatInfo.BattleStatValue, ref statModifiers);
        }
    }

    private static void ApplyAccountStats(List<AccountStatData> accountStatDatas, BattleStat battleStat, ref TotalStatValueByModifiers statModifiers)
    {
        foreach (var accountStatData in accountStatDatas.Where(a => a.GetAccountStatType() == AccountStatType.CharacterStat))
        {
            if (accountStatData.Stat == AccountStat.None) continue;
            if (!accountStatData.TryGetAccountBattleStat(out BattleStat accountBattleStat) || accountBattleStat != battleStat) continue;
            ApplyStat(accountStatData.GetAccountStatModifierType(), accountStatData.AccountStatValue, ref statModifiers);
        }
    }

    private static void ApplyStat(AccountStatModifierType accountStatModifierType, float accountStatValue, ref TotalStatValueByModifiers statModifiers)
    {
        switch (accountStatModifierType)
        {
            case AccountStatModifierType.BaseStat: statModifiers.BaseStat += accountStatValue; break;
            case AccountStatModifierType.SimpleMultiplier: statModifiers.SimpleMultiplier += accountStatValue; break;
            case AccountStatModifierType.CompoundMultiplier: statModifiers.CompoundMultiplier *= accountStatValue; break;
            case AccountStatModifierType.AdditionalStat: statModifiers.AdditionalStat += accountStatValue; break;
        }
    }

    private static float CalculateTotalBattleStat(CharacterData characterData, BattleStat battleStat, TotalStatValueByModifiers statModifiers)
    {
        float characterBaseStat = characterData.GetBattleStat(battleStat);
        return CalculateTotalValue(characterBaseStat, statModifiers);
    }

    private static float CalculateTotalBattleStat(CharacterDataSnapshot characterDataSnapshot, BattleStat battleStat, TotalStatValueByModifiers statModifiers)
    {
        var characterProto = ProtoData.Current.Characters.Get(characterDataSnapshot.ProtoId);
        float characterBaseStat = characterProto.Stat.GetBattleStat(characterProto.Rarity, battleStat, characterDataSnapshot.Level, characterDataSnapshot.TranscendenceGrade);
        return CalculateTotalValue(characterBaseStat, statModifiers);
    }

    private static float CalculateTotalValue(float baseStat, TotalStatValueByModifiers statModifiers)
    {
        float totalValue = baseStat;
        totalValue += statModifiers.BaseStat;
        totalValue *= statModifiers.SimpleMultiplier;
        totalValue *= statModifiers.CompoundMultiplier;
        totalValue += statModifiers.AdditionalStat;
        return totalValue;
    }

    private class TotalStatValueByModifiers
    {
        public float BaseStat { get; set; } = 0.0f;
        public float SimpleMultiplier { get; set; } = 1.0f;
        public float CompoundMultiplier { get; set; } = 1.0f;
        public float AdditionalStat { get; set; } = 0.0f;
    }
}