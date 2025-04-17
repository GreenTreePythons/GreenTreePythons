using System;
using System.Linq;
using System.Collections.Generic;
using Latecia.Shared;
using MagicOnion;
using Microsoft.EntityFrameworkCore;

public partial class GameService
{
    public async UnaryResult<(List<(GachaRewardType rewardType, int protoId, int dataId)> rewardInfoWithDataGroup, int shopCoinAmount, long mileageAmount, InventoryResult result)> BuyGachaItem(int gachaProtoId, bool isTicket, int gachaAmount = 1)
    {
        var user = await UserQuery.Include(u => u.ContentLockData).SingleAsync();
        if (!user.ContentLockData.IsContentUnLock(ContentLockGroupType.Research1Group1, 1)) throw new InvalidOperationException($"Content is Locked");

        var rewardInfoGroup = new List<(GachaRewardType rewardType, int protoId)>();
        var rewardInfoWithDataGroup = new List<(GachaRewardType rewardType, int protoId, int dataId)>();
        var totalShopCoinAmount = 0;
        long mileageCount = 0;
        long soulStoneAmount = ProtoData.Current.GachaShopSettings.CharacterShopSoulStoneAmount;
        var mileageData = default(GachaShopMileageItemData);

        if (!ProtoData.Current.GachaShopProtos.TryGet(gachaProtoId, out var gachaShopProto))
        {
            return (rewardInfoWithDataGroup, totalShopCoinAmount, mileageCount, InventoryResult.Empty);
        }

        user = await UserQuery.Include(u => u.ItemDatas)
                                    .Include(u => u.CharacterDatas)
                                    .Include(u => u.UserActionData)
                                    .Include(u => u.TutorialData)
                                    .SingleAsync();

        user.UserActionData.GachaCount += gachaAmount;

        if (gachaShopProto.MileageSid != null)
        {
            var mileageProto = ProtoData.Current.GachaShopMileages.Get(gachaShopProto.MileageSid);
            var mileageItemProto = Proto.Items.Get(mileageProto.MileageItemSid);
            mileageData = (GachaShopMileageItemData)await UserEntry.Collection(u => u.ItemDatas).Query().SingleAsync(i => i.ProtoId == mileageItemProto.Id);
        }

        if (isTicket)
        {
            await Inventory.ConsumeItemAsync(Proto.Items.GetId(gachaShopProto.TicketItemSid), gachaShopProto.TicketCost * gachaAmount);
        }
        else
        {
            await Inventory.ConsumeItemAsync(ProtoDataConst.GEM, gachaShopProto.CashCost * gachaAmount);
        }

        // get item by probability
        var boxGroupPicker = new WeightedRandomPicker();
        var box = ProtoData.Current.GachaShopGachaBoxs.SubDictionary[gachaShopProto.GachaBoxSid];

        for (int i = 0; i < gachaAmount; i++)
        {
            var itemGroupPicker = new WeightedRandomPicker();

            // pick box
            foreach (var boxGroup in box)
            {
                var weightedGroupItem = new WeightedItem() { Sid = boxGroup.RewardGroup, Weight = boxGroup.Probability };
                boxGroupPicker.WeightedItemList.Add(weightedGroupItem);
            }
            WeightedItem pickedBox = boxGroupPicker.GetRandomPick();

            // pick reward in box
            var pickedItemProto = box.First(b => b.RewardGroup == pickedBox.Sid);
            var gachaRewardType = pickedItemProto.RewardType;
            switch (pickedItemProto.RewardType)
            {
                case GachaRewardType.Character:
                    {
                        var characterGroup = ProtoData.Current.GachaShopCharacterGroups.SubDictionary.GetValueOrDefault(pickedItemProto.RewardGroup);
                        foreach (var group in characterGroup)
                        {
                            var characterProtoId = ProtoData.Current.Characters.Get(group.CharacterSid).Id;
                            _SetWeightedItemList(itemGroupPicker, characterProtoId, group.Probability);
                        }
                    }
                    break;
                case GachaRewardType.Item:
                    {
                        var itemGroup = ProtoData.Current.GachaShopItemGroups.SubDictionary.GetValueOrDefault(pickedItemProto.RewardGroup);
                        foreach (var group in itemGroup)
                        {
                            var itemProtoId = ProtoData.Current.Characters.Get(group.ItemSid).Id;
                            _SetWeightedItemList(itemGroupPicker, itemProtoId, group.Probability);
                        }
                    }
                    break;
            }
            WeightedItem pickedItem = itemGroupPicker.GetRandomPick();

            // update reward, shop coin
            switch (pickedItemProto.RewardType)
            {
                case GachaRewardType.Character:

                    if (user.TutorialData.CompleteTutorialGroupId == ProtoDataConst.GACHA_TUTORIAL_START)
                    {
                        // when tutorial gacha, last character result is ilchae1evo(1270)
                        if (i == 9) pickedItem.ProtoId = ProtoDataConst.GACHA_TUTORIAL_LEGEND_REWARD;
                    }

                    var characterProto = ProtoData.Current.Characters.Get(pickedItem.ProtoId);
                    var characterData = user.CharacterDatas.FirstOrDefault(c => c.GetProto().EvolutionBase == characterProto.EvolutionBase);

                    // if new character, add it
                    if (characterData == null)
                    {
                        var newCharacterData = CharacterData.Create(characterProto.Id);
                        user.CharacterDatas.Add(newCharacterData);
                    }
                    else
                    {
                        var soulStoneItemProto = ProtoData.Current.Items.Get(characterData.GetProto().GetSoulStoneItemSid());
                        Inventory.GrantRequest.AddItem(soulStoneItemProto.CreateData(soulStoneAmount));
                    }

                    var coinAmount = ProtoData.Current.GachaShopSettings.GetShopCoinAmountByCharacterRarity(characterProto.Rarity, characterData == null);
                    var shopCoinProto = ProtoData.Current.Items.Get(ProtoDataConst.GACHASHOP_CHARACTER_COIN);
                    Inventory.GrantRequest.AddItem(shopCoinProto.CreateData(coinAmount));
                    totalShopCoinAmount += coinAmount;
                    break;

                case GachaRewardType.Item:
                    break;
            }

            // updaete mileageData amount by gacha time
            if (mileageData != null)
            {
                mileageData.SetCount(mileageData.Count + 1);
                mileageCount = mileageData.Count;
            }

            rewardInfoGroup.Add((gachaRewardType, pickedItem.ProtoId));
        }

        await Inventory.GrantItemsAsync();
        await DbContext.SaveChangesAsync();

        // add data id to rewardInfoGroup
        foreach (var reward in rewardInfoGroup)
        {
            GachaRewardType rewardType = default(GachaRewardType);
            var protoId = 0;
            var dataId = 0;

            if (reward.rewardType == GachaRewardType.Character)
            {
                var proto = ProtoData.Current.Characters.Get(reward.protoId);
                dataId = user.CharacterDatas.First(c => c.GetProto().EvolutionBase == proto.EvolutionBase).Id;
            }
            else if (reward.rewardType == GachaRewardType.Item)
            {
                dataId = user.ItemDatas.First(i => i.ProtoId == reward.protoId).Id;
            }
            protoId = reward.protoId;
            rewardType = reward.rewardType;

            rewardInfoWithDataGroup.Add((rewardType, protoId, dataId));
        }

        void _SetWeightedItemList(WeightedRandomPicker itemGroupPicker, int protoId, double probability)
        {
            var weightedGroupItem = new WeightedItem() { ProtoId = protoId, Weight = probability };
            itemGroupPicker.WeightedItemList.Add(weightedGroupItem);
        }

        return (rewardInfoWithDataGroup, totalShopCoinAmount, mileageCount, Inventory.Result);
    }

    public async UnaryResult<InventoryResult> GetMileageRewards(int gachaProtoId)
    {
        var user = await UserQuery.Include(u => u.ContentLockData).SingleAsync();
        if (!user.ContentLockData.IsContentUnLock(ContentLockGroupType.Research1Group1, 1)) throw new InvalidOperationException($"Content is Locked");

        var proto = ProtoData.Current.GachaShopProtos.Get(gachaProtoId);
        if (proto.MileageSid == null) return InventoryResult.Empty;

        var mileageProto = Proto.GachaShopMileages.Get(proto.MileageSid);
        var mileageItemProto = Proto.Items.Get(mileageProto.MileageItemSid);

        var mileageItemData = (GachaShopMileageItemData)await UserEntry.Collection(u => u.ItemDatas).Query().SingleAsync(i => i.ProtoId == mileageItemProto.Id);

        var mileageRewardRoopCount = mileageItemData.Count / mileageProto.MileageInformations[mileageItemData.GetMaxMileageGrade(mileageProto) - 1].Point;
        var roopCount = mileageRewardRoopCount < 1 ? 1 : mileageRewardRoopCount;

        for (int i = 0; i < roopCount; i++)
        {
            foreach (var info in mileageProto.MileageInformations)
            {
                // already got reward or impossible to get reward check
                if (!mileageItemData.IsAbleGetMileageReward(mileageProto, info.Grade)) continue;

                // update mileage
                mileageItemData.UpdateMileageGrade();

                var item = ProtoData.Current.Items.Get(info.RewardItemSid).CreateData(info.RewardItemAmount);
                // set rewards
                Inventory.GrantRequest.AddItem(item);
            }
            var maxMileageGrade = mileageItemData.GetMaxMileageGrade(mileageProto);
            if (mileageItemData.MileageStepGrade >= maxMileageGrade)
            {
                mileageItemData.ResetMileage(mileageProto);
            }
        }

        await Inventory.GrantItemsAsync();

        return Inventory.Result;
    }
}