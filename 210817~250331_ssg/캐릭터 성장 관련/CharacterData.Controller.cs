
using System.Collections.Generic;
using System.Linq;
using SingleSquadBattle;

namespace Latecia.Shared
{
    public partial class CharacterData
    {
        public float GetBattleStat(BattleStat stat, float defaultValue = 0f) => GetBattleStat(stat, Level, defaultValue);

        public float GetBattleStat(BattleStat stat, int level, float defaultValue = 0f)
        {
            var proto = this.GetProto();
            return proto.Stat.GetBattleStat(proto.Rarity, stat, level, TranscendenceGrade, defaultValue);
        }

        public int GetSkillLevelOrDefault(SkillType skillType)
        {
            var specificityProto = ProtoData.Current.CharacterGrowthSpecificities.GetSpecificityProto(GetProto().EvolutionBase, skillType);
            if (specificityProto == null) return 1;
            var specificityInfo = GetActiveSpecificityInfo(specificityProto.Id);
            return specificityInfo == null ? 1 : specificityInfo.Level;
        }

        public CharacterDataSnapshot CreateBattleSnapshot(List<AccountStatData> accountStatDatas)
        {
            var result = new CharacterDataSnapshot()
            {
                Level = Level,
                AwakenGrade = AwakeningGrade,
                TranscendenceGrade = TranscendenceGrade,
                ProtoId = ProtoId,
                SkillLevels = GetSkillLevels(),
                AdditionalStats = new(),
                AdditionalPassives = GetAdditionalPassives()
            };

            foreach (var accountStatData in accountStatDatas)
            {
                if (accountStatData.GetAccountStatType() != AccountStatType.CharacterStat) continue;
                if (!accountStatData.TryGetAccountBattleStat(out BattleStat battleStat)) continue;
                if (accountStatData.AccountStatValue == 0) continue;
                StatCalculatorUtility.AddOrUpdateStats(result.AdditionalStats, battleStat, accountStatData);
            }

            foreach (var activeSpecificity in ActiveSpecificities)
            {
                var specificityStat = ProtoData.Current.CharacterGrowthSpecificities.GetSpecificityStat(activeSpecificity.NodeId, GetProto().EvolutionBase, activeSpecificity.Level);
                if (specificityStat.stat == AccountStat.None) continue;
                if (specificityStat.statValue == 0) continue;
                if (specificityStat.stat.TryGetBattleStat(out BattleStat battleStat, out var _)) continue;
                StatCalculatorUtility.AddOrUpdateStats(result.AdditionalStats, battleStat, specificityStat.stat.SplitAccountStat().modifierType, specificityStat.statValue);
            }

            return result;
        }

        public CharacterSkillLevel GetSkillLevels()
        {
            var skillLevels = new CharacterSkillLevel
            {
                Default = GetSkillLevelOrDefault(SkillType.Default),
                Active = GetSkillLevelOrDefault(SkillType.ActiveSkill),
                Leader = GetSkillLevelOrDefault(SkillType.LeaderSkill),
                Drive = GetSkillLevelOrDefault(SkillType.DriveSkill),
                Passive = GetSkillLevelOrDefault(SkillType.PassiveSkill)
            };
            return skillLevels;
        }

        public int GetSkillMaxLevel() => ProtoData.Current.Setting.CharacterSkillLevelMax;

        public int GetBattlePower(List<AccountStatData> accountStats)
        {
            float attackValue = accountStats == null ? GetBattleStat(BattleStat.Attack)
                                : StatCalculatorUtility.CalculateBattleStatWithAdditionalStats(this, BattleStat.Attack, accountStats);
            float defenceValue = accountStats == null ? GetBattleStat(BattleStat.Defence)
                                : StatCalculatorUtility.CalculateBattleStatWithAdditionalStats(this, BattleStat.Defence, accountStats);
            float healthValue = accountStats == null ? GetBattleStat(BattleStat.Health)
                                : StatCalculatorUtility.CalculateBattleStatWithAdditionalStats(this, BattleStat.Health, accountStats);
            float elementValue = accountStats == null ? GetBattleStat(BattleStat.ElementalPower)
                                : StatCalculatorUtility.CalculateBattleStatWithAdditionalStats(this, BattleStat.ElementalPower, accountStats);
            float constDefenceValue = ProtoData.Current.CharacterSetting.DefenceConstantValue;
            float constElementValue = ProtoData.Current.CharacterSetting.ElementPowerConstantValue;
            float battlePowerMultiplier = ProtoData.Current.CharacterSetting.BattlePowerMultiplier;

            float attackContribution = attackValue * (1 - (defenceValue / (defenceValue + constDefenceValue)));
            float defenceContribution = defenceValue * (1 - (attackValue / (attackValue + constDefenceValue)));

            float firstValue = (attackContribution + defenceContribution) * healthValue / 28;
            float secondValue = ((1f * 0.3f) + (0.7f * (1f + (1f - (100f / (constElementValue + elementValue)))))) * battlePowerMultiplier;
            float result = firstValue * secondValue;
            return (int)result;
        }
    }
}