using System;
using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using MagicOnion;
using Microsoft.EntityFrameworkCore;

public partial class GameService
{
    public async UnaryResult<InventoryResult> CharacterEvolution(int characterDataId)
    {
        var characterData = await UserEntry.Collection(u => u.CharacterDatas).Query().SingleAsync(c => c.Id == characterDataId);
        await UserEntry.Reference(u => u.UserActionData).LoadAsync();

        if (characterData.IsEvolutionMax()) throw new Exception("Already Max Evolution");

        var costProto = characterData.GetEvolutionCost();

        var goldProto = ProtoData.Current.Items.Get(ProtoDataConst.GOLD);
        var soulStoneItemProto = ProtoData.Current.Items.Get(characterData.GetProto().GetSoulStoneItemSid());

        await Inventory.ConsumeItemAsync(soulStoneItemProto.Id, costProto.SoulStoneCost);

        characterData.CharacterEvolution();

        User.UserActionData.CharacterEvolutionCount += 1;
        await DbContext.SaveChangesAsync();

        return Inventory.Result;
    }

    public async UnaryResult<InventoryResult> CharacterAwakening(int characterDataId)
    {
        var user = await UserQuery.Include(u => u.CharacterDatas.Where(c => c.Id == characterDataId))
                                    .Include(u => u.UserActionData)
                                    .Include(u => u.ContentLockData)
                                    .SingleAsync();
        
        if (!user.ContentLockData.IsContentUnLock(ContentLockGroupType.Character, 2)) throw new InvalidOperationException($"Content is Locked");

        var characterData = user.CharacterDatas.Single();
        var costProto = characterData.GetAwakeningCostProto(characterData.AwakeningGrade + 1);

        await Inventory.ConsumeItemAsync(Proto.Items.Get(characterData.GetProto().GetSoulStoneItemSid()).Id, costProto.SoulStoneCost);

        characterData.AwakeningGrade++;
        user.UserActionData.CharacterAwakeningCount++;

        await DbContext.SaveChangesAsync();

        return Inventory.Result;
    }

    public async UnaryResult<InventoryResult> SpecificityLevelUp(int characterDataId, int nodeId)
    {
        var user = await UserQuery.Include(u => u.UserActionData)
                                    .Include(u => u.CharacterDatas.Where(c => c.Id == characterDataId))
                                    .Include(u => u.ContentLockData)
                                    .SingleAsync();

        if (!user.ContentLockData.IsContentUnLock(ContentLockGroupType.Character, 4)) throw new InvalidOperationException($"Content is Locked");

        var characterData = user.CharacterDatas.Single();
        var specificityProto = ProtoData.Current.CharacterGrowthSpecificities.GetSpecificityProto(characterData.GetProto().EvolutionBase, nodeId);

        var isMaxLevel = characterData.IsMaxSpecificityLevel(specificityProto.Id);
        if (isMaxLevel) throw new Exception("specificity level is max");

        if (characterData.GetSpecificityState(nodeId) != NodeUIState.Possible) throw new Exception("specificity not possible state");

        characterData.GetSpecificityLevelUpCost(nodeId, out var costItems, out var goldCost);
        var goldProto = Proto.Items.Get(ProtoDataConst.GOLD);
        costItems.Add((goldProto.Id, goldCost));

        await Inventory.ConsumeItemsAsync(costItems);

        characterData.SpecificityLevelUp(nodeId);

        user.UserActionData.CharacterSpecificityCount += 1;

        await DbContext.SaveChangesAsync();

        return Inventory.Result;
    }
}