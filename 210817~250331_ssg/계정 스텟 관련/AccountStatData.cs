using MessagePack;
using Microsoft.EntityFrameworkCore;

namespace Latecia.Shared
{
    [MessagePackObject]
    [Index(nameof(Stat))]
    public partial class AccountStatData
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public AccountStat Stat { get; set; }
        [Key(2)]
        public float AccountStatValue { get; set; }
    }

    public interface IAccountStatDataComponent
    {
        bool TryGetAccountStat(out AccountStat stat, out float value);
        bool TryGetAccountStatValue(out float value);
        bool TryGetAccountStatInformation(out AccountStatType statType, out AccountStatModifierType modifierType);
    }

    public interface IAccountStatProtoComponent
    {
        public AccountStat Stat { get; }
        public AccountStatType StatType { get; }
        public AccountStatModifierType StatModifierType { get; }
        bool TryGetStatValue(int level, out float statValue);
    }
}