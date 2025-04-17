using System.Collections.Generic;
using System.Linq;
using SingleSquadBattle;

namespace Latecia.Shared
{
    public class CharacterSpecificityContainer
    {
        // string key is characterEvoSid
        readonly Dictionary<string, List<SpecificityCharacterGrowthProto>> m_SpecificityProtos = new();
        readonly Dictionary<string, List<SpecificityItemProto>> m_SpecificityItemProtos = new();
        readonly Dictionary<string, List<SpecificityStat>> m_SpecificityStats = new();

        public CharacterSpecificityContainer(List<SpecificityCharacterGrowthProto> growthProtos,
            ProtoData.ProtoDictionary<CharacterProto> characterProtos,
            ProtoData.ProtoDictionary<BaseItemProto> itemProtos,
            ProtoTestContext protoTest)
        {
            var specificityItemsByClass = itemProtos.Where(i => i.Type == ItemType.Specificity).Cast<SpecificityItemProto>()
                                                    .GroupBy(i => i.Class)
                                                    .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var growthProto in growthProtos)
            {
                // add growth proto to specificity groups
                AddToDictionaryList(m_SpecificityProtos, growthProto.CharacterGroupSID, growthProto);

                // add specificity stats
                if (growthProto.SpecificityStatValue != 0)
                {
                    if (!m_SpecificityStats.TryGetValue(growthProto.CharacterGroupSID, out var list))
                    {
                        list = new List<SpecificityStat>();
                        m_SpecificityStats[growthProto.CharacterGroupSID] = list;
                    }
                    var stat = new SpecificityStat(growthProto.Id, growthProto.SpecificityStat, growthProto.SpecificityStatValue);
                    list.Add(stat);
                }

                // add specificity items related to this growth proto
                if (!m_SpecificityItemProtos.ContainsKey(growthProto.CharacterGroupSID))
                {
                    var characterClass = characterProtos.Get(growthProto.CharacterGroupSID).Class;

                    if (specificityItemsByClass.TryGetValue(characterClass, out var specificityItems))
                    {
                        m_SpecificityItemProtos[growthProto.CharacterGroupSID] = new List<SpecificityItemProto>(specificityItems);
                    }
                }
            }

            // TestContext
            if (protoTest != null)
            {
                foreach (var specificityProto in m_SpecificityProtos)
                {
                    if (!characterProtos.TryGet(specificityProto.Key, out var characterProto))
                    {
                        protoTest.Fail($"{specificityProto.Key} not in characterProto");
                    }

                    foreach (var specificity in specificityProto.Value)
                    {
                        if (!specificity.OpenCondition.IsValid) continue;
                        if (specificityProto.Value.SingleOrDefault(p => p.Id == specificity.OpenCondition.NodeId) == null)
                        {
                            protoTest.Fail($"{specificity.Id} node openCodition nodeId({specificity.OpenCondition.NodeId}) is not contained in CharacterGrowthProto_Specificity");
                        }
                    }
                }
            }
        }

        private static void AddToDictionaryList<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                dict[key] = list;
            }
            list.Add(value);
        }

        public List<SpecificityStat> GetSpecificityStats(string characterEvoSid)
            => m_SpecificityStats[characterEvoSid] == null ? null : m_SpecificityStats[characterEvoSid];

        public (AccountStat stat, float statValue) GetSpecificityStat(int nodeId, string characterEvoSid, int speicificityLevel)
        {
            if (m_SpecificityStats[characterEvoSid] == null) return (AccountStat.None, 0.0f);
            var stat = m_SpecificityStats[characterEvoSid].SingleOrDefault(s => s.NodeId == nodeId);
            if (stat == null) return (AccountStat.None, 0.0f);
            return (stat.BattleStat, stat.StatValue * speicificityLevel);
        }

        public List<SpecificityCharacterGrowthProto> GetSpecificityProtos(string characterEvoSid)
            => m_SpecificityProtos[characterEvoSid];

        public SpecificityCharacterGrowthProto GetSpecificityProto(string characterEvoSid, int nodeId)
            => GetSpecificityProtos(characterEvoSid).Single(p => p.Id == nodeId);

        public SpecificityCharacterGrowthProto GetSpecificityProto(string characterEvoSid, SkillType skillType)
            => GetSpecificityProtos(characterEvoSid).SingleOrDefault(p => p.NodeSkillType == skillType);

        public SpecificityItemProto GetSpecificityItemProto(string characterEvoSid, int grade)
            => m_SpecificityItemProtos.GetValueOrDefault(characterEvoSid).Single(i => i.Grade == grade);

        public class SpecificityStat
        {
            public int NodeId;
            public AccountStat BattleStat;
            public float StatValue;
            public SpecificityStat(int nodeId, AccountStat battleStat, float statValue)
            {
                NodeId = nodeId;
                BattleStat = battleStat;
                StatValue = statValue;
            }
        }
    }
}