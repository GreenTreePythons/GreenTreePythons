using MessagePack;
using Microsoft.EntityFrameworkCore;
using SingleSquadBattle;
using System.Collections.Generic;
using System.Linq;

namespace Latecia.Shared
{
    [MessagePackObject]
    [Index(nameof(ProtoId))]
    [Index(nameof(OriginProtoId))]
    public partial class CharacterData : IServerData<CharacterProto>
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public int ProtoId { get; set; }
        [Key(2)]
        public int OriginProtoId { get; set; }
        [Key(3)]
        public int Level { get; set; } = 1;
        [Key(4)]
        public ulong Exp { get; set; }
        [Key(5)]
        public int TranscendenceGrade { get; set; }
        [Key(6)]
        public int AwakeningGrade { get; set; }
        [Key(7)]
        public bool Bookmark { get; set; }
        [Key(8), DBJson(true)]
        public List<SpecificityInfo> ActiveSpecificities { get; set; } = new();

        public static CharacterData Create(int protoId)
        {
            CharacterData newCharacter = new();
            newCharacter.ProtoId = protoId;
            newCharacter.OriginProtoId = protoId;
            newCharacter.Level = 1;

            var specificityInfos = ProtoData.Current.CharacterGrowthSpecificities
                                                    .GetSpecificityProtos(newCharacter.GetProto().EvolutionBase)
                                                    .Where(sp => sp.NodeSkillType != SkillType.PassiveAdditional && sp.NodeSkillType != SkillType.None)
                                                    .Select(sp => SpecificityInfo.Create(sp.Id));
            newCharacter.ActiveSpecificities.AddRange(specificityInfos);

            return newCharacter;

        }
    }

    [MessagePackObject]
    public class SpecificityInfo
    {
        [Key(0)]
        public int NodeId { get; set; }
        [Key(1)]
        public int Level { get; set; }

        public static SpecificityInfo Create(int nodeId) => new SpecificityInfo
        {
            NodeId = nodeId,
            Level = 1
        };
    }
}