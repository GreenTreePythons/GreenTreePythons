using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using MagicOnion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public partial class GameService
{
    public readonly UserAccountStat AccountStats;
}

public class UserAccountStat
{
    private readonly EntityEntry<UserData> m_UserEntry;
    private readonly ProtoData m_Proto;

    public UserAccountStat(EntityEntry<UserData> userEntry, ProtoData proto)
    {
        m_UserEntry = userEntry;
        m_Proto = proto;
    }

    public async UnaryResult Load(IEnumerable<IAccountStatProtoComponent> components)
        => await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => components.Select(c => c.Stat).Contains(a.Stat)).LoadAsync();

    public async UnaryResult Load(IEnumerable<AccountStat> stats)
        => await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => stats.Contains(a.Stat)).LoadAsync();

    public async UnaryResult Load(params AccountStat[] stats)
        => await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => stats.Contains(a.Stat)).LoadAsync();

    public async UnaryResult<bool> RemoveValue(AccountStat stat, float statValue)
    {
        var accountStatData = await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => a.Stat == stat).SingleOrDefaultAsync();
        if (accountStatData == null) return false;
        accountStatData.RemoveAccountStat(statValue);
        return true;
    }

    public async UnaryResult<bool> RemoveValue(IAccountStatDataComponent accountStatDataComponent)
    {
        if (accountStatDataComponent.TryGetAccountStat(out AccountStat stat, out float statValue))
        {
            var accountStatData = await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => a.Stat == stat).SingleAsync();
            accountStatData.RemoveAccountStat(statValue);
            return true;
        }
        return false;
    }

    public async UnaryResult<bool> TryApplyValue(IAccountStatDataComponent accountStatDataComponent)
    {
        if (accountStatDataComponent.TryGetAccountStat(out AccountStat stat, out float statValue))
        {
            var accountStatData = await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => a.Stat == stat).SingleOrDefaultAsync();
            if (accountStatData != null)
            {
                accountStatData.ApplyAccountStat(statValue);
            }
            else
            {
                m_UserEntry.Entity.SetAccountStatDataComponent(accountStatDataComponent);
            }
            return true;
        }
        return false;
    }

    public async UnaryResult ApplyValues(IEnumerable<IAccountStatProtoComponent> components)
    {
        await Load(components);
        foreach (var component in components)
        {
            var accountStatData = m_UserEntry.Entity.AccountStatDatas.SingleOrDefault(s => s.Stat == component.Stat);
            if (accountStatData != null)
            {
                if (!component.TryGetStatValue(1, out float statValue)) continue;
                accountStatData.ApplyAccountStat(statValue);
            }
            else
            {
                if (!component.TryGetStatValue(1, out float statValue)) continue;
                accountStatData = new AccountStatData()
                {
                    Stat = component.Stat,
                    AccountStatValue = statValue,
                    AccountStatSid = component.Stat.ToString()
                };
                m_UserEntry.Entity.AccountStatDatas.Add(accountStatData);
            }
        }
    }

    public async UnaryResult<float> GetValueOrDefault(AccountStat stat)
    {
        var accountStat = await m_UserEntry.Collection(u => u.AccountStatDatas).Query().Where(a => a.Stat == stat).SingleOrDefaultAsync();
        if (accountStat != null) return accountStat.AccountStatValue;
        return m_Proto.DefaultAccountStatValues.GetValueOrDefault(stat);
    }
}