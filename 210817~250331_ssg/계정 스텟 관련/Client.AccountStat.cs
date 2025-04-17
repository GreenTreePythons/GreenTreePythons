using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using ViewSystem;

public partial class ClientUserData
{
    public List<AccountStatData> AccountStatDatas { get; private set; }

    void SetAccountStatDatas(UserData userData) => AccountStatDatas ??= userData.AccountStatDatas;

    public bool TryRemoveAccountStat(IAccountStatDataComponent accountStatDataComponent)
    {
        if (accountStatDataComponent.TryGetAccountStat(out AccountStat stat, out float statValue))
        {
            var accountStatData = GetAccountStatDataOrDefault(stat);
            if (accountStatData != null)
            {
                accountStatData.RemoveAccountStat(statValue);
                return true;
            }
        }
        return false;
    }

    public bool TryAddAccountStat(IAccountStatDataComponent accountStatDataComponent)
    {
        if (accountStatDataComponent.TryGetAccountStat(out AccountStat stat, out float statValue))
        {
            AddAccountStat(stat, statValue);
            return true;
        }
        return false;
    }

    public bool TryAddAccountStat(IAccountStatProtoComponent accountStatProtoComponent, int level)
    {
        if (accountStatProtoComponent.TryGetStatValue(level, out float statValue))
        {
            var stat = accountStatProtoComponent.Stat;
            AddAccountStat(stat, statValue);
            return true;
        }
        return false;
    }

    public void AddAccountStat(AccountStat stat, float statValue)
    {
        var accountStatData = GetAccountStatDataOrDefault(stat);
        if (accountStatData == null)
        {
            accountStatData = new AccountStatData()
            {
                Stat = stat,
                AccountStatValue = ProtoData.Current.DefaultAccountStatValues.GetValueOrDefault(stat),
                AccountStatSid = stat.ToString()
            };
            AccountStatDatas.Add(accountStatData);
        }
        else
        {
            accountStatData.ApplyAccountStat(statValue);
        }

        // refresh due to stats affecting top bar information(ex, ticket)
        var topbarReferencedStat = stat == AccountStat.BattleStage_BattleTicketChargeCycle_BaseStat ||
                                   stat == AccountStat.SpaceExplore_SpaceTicketChargeCycle_BaseStat;
        if (topbarReferencedStat)
        {
            ViewManager.GetOverlay<TopbarOverlay>().RefreshCurrencyPanel();
        }
    }

    public void AddAccountStat(IAccountStatProtoComponent accountStatProtoComponent, int level)
    {
        if (accountStatProtoComponent.TryGetStatValue(level, out var value))
        {
            AddAccountStat(accountStatProtoComponent.Stat, value);
        }
    }

    public void AddAccountStats(IEnumerable<IAccountStatProtoComponent> accountStatProtoComponents, int level)
    {
        foreach (var component in accountStatProtoComponents)
        {
            AddAccountStat(component, level);
        }
    }

    public AccountStatData GetAccountStatDataOrDefault(AccountStat stat) => AccountStatDatas.SingleOrDefault(a => a.Stat == stat);

    public float GetAccountStatValueOrDefault(AccountStat stat)
    {
        var statData = GetAccountStatDataOrDefault(stat);
        if (statData != null) return statData.AccountStatValue;
        return ProtoData.Current.DefaultAccountStatValues.GetValueOrDefault(stat);
    }

    public bool TryGetAccountTotalStatValue(AccountStat stat, out float statValue)
    {
        statValue = 0;
        var accountStatData = GetAccountStatDataOrDefault(stat);
        if (accountStatData == null) return false;
        statValue = accountStatData.AccountStatValue;
        return true;
    }

    public Dictionary<AccountStatType, Dictionary<AccountStat, float>> GetAllAccountStatInformation()
    {
        var result = new Dictionary<AccountStatType, Dictionary<AccountStat, float>>();
        foreach (var data in AccountStatDatas)
        {
            var statType = data.GetAccountStatType();
            var valueDict = new Dictionary<AccountStat, float>();
            var valueKey = data.Stat;
            valueDict.Add(valueKey, data.AccountStatValue);

            if (!result.TryAdd(statType, valueDict))
            {
                if (result[statType].TryGetValue(valueKey, out var existStatValue))
                {
                    result[statType][valueKey] = existStatValue + data.AccountStatValue;
                }
                else result[statType].Add(valueKey, data.AccountStatValue);
            }

            if (data.AccountStatValue != 0) SharedDebug.Log($"statType:{statType} / stat:{valueKey} / value:{data.AccountStatValue}");
        }
        return result;
    }

    public Dictionary<AccountStatType, Dictionary<AccountStat, float>> GetAccountStatInformationFromData(IEnumerable<IAccountStatDataComponent> datas)
    {
        var result = new Dictionary<AccountStatType, Dictionary<AccountStat, float>>();
        foreach (var data in datas)
        {
            if (!data.TryGetAccountStat(out var stat, out var value)) continue;
            if (!data.TryGetAccountStatInformation(out var statType, out AccountStatModifierType modifierType)) continue;
            var valueDict = new Dictionary<AccountStat, float>();
            var valueKey = stat;
            valueDict.Add(valueKey, value);

            if (!result.TryAdd(statType, valueDict))
            {
                if (result[statType].TryGetValue(valueKey, out var existStatValue))
                {
                    result[statType][valueKey] = existStatValue + value;
                }
                else result[statType].Add(valueKey, value);
            }

            if (value != 0) SharedDebug.Log($"statType:{statType} / stat:{stat} / value:{value}");
        }
        return result;
    }

    public Dictionary<AccountStatType, Dictionary<AccountStat, float>> GetAchievementAccountStat(AchievementWrapperData achievementWrapperData)
    {
        var result = new Dictionary<AccountStatType, Dictionary<AccountStat, float>>();

        foreach (var proto in ProtoData.Current.AchievementLevels.Where(p => p.Level >= 1).OrderBy(p => p.Level))
        {
            if (achievementWrapperData.AchievementLevel == 1) return result;
            if (!proto.IsExistStatRward) continue;
            if (proto.Level <= achievementWrapperData.AchievementLevel) // get account stat
            {
                var valueDict = new Dictionary<AccountStat, float>
                {
                    [proto.Stat] = proto.StatValue
                };

                var statType = proto.StatType;
                if (!result.TryAdd(statType, valueDict))
                {
                    if (result[statType].TryGetValue(proto.Stat, out var existStatValue))
                    {
                        result[statType][proto.Stat] = existStatValue + proto.StatValue;
                    }
                    else result[statType].Add(proto.Stat, proto.StatValue);
                }
            }
        }

        return result;
    }

    public Dictionary<AccountStat, float> GetAccountStatsFromData(IEnumerable<IAccountStatDataComponent> datas)
    {
        var result = new Dictionary<AccountStat, float>();
        foreach (var data in datas)
        {
            if (!data.TryGetAccountStat(out var stat, out var value)) continue;
            if (result.TryGetValue(stat, out var prevValue))
            {
                result[stat] = prevValue + value;
            }
            else
            {
                result.Add(stat, value);
            }
        }
        return result;
    }
}