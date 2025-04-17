using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Latecia.Shared;

public partial class GameServiceRequests
{
    public async Task<List<(GachaRewardType rewardType, int protoId, bool isNew, bool isSoulStoneMax)>> RequestBuyGachaItem(int gachaProtoId, int gachaAmount)
    {
        var gachaShopProto = ProtoData.Current.GachaShopProtos.GetOrDefault(gachaProtoId);
        var (rewardInfoWithDataGroup, mileageAmount, result) = await ServerManager.GameService.BuyGachaItem(gachaProtoId, gachaAmount);
        var resultList = new List<(GachaRewardType rewardType, int protoId, bool isNew, bool isSoulStoneMax)>();

        // updaete mileageData amount
        if (ProtoData.Current.GachaShopMileages.TryGet(gachaShopProto.MileageSid, out var mileageProto))
        {
            var mileageData = m_UserData.ItemDatas.GetDataByProtoSid(mileageProto.MileageItemSid) as GachaShopMileageItemData;
            mileageData.SetCount(mileageAmount);
        }

        m_UserData.ApplyInventoryResult(result);

        // update rewards
        foreach (var reward in rewardInfoWithDataGroup)
        {
            bool isNew = false;
            if (reward.rewardType == GachaRewardType.Character)
            {
                var characterProto = ProtoData.Current.Characters.Get(reward.protoId);
                var characterData = m_UserData.CharacterDatas.GetList().FirstOrDefault(c => c.GetProto().EvolutionBase == characterProto.EvolutionBase);

                if (characterData == null)
                {
                    var newCharacterData = CharacterData.Create(characterProto.Id);
                    newCharacterData.Id = reward.dataId;
                    m_UserData.CharacterDatas.AddData(newCharacterData);
                    isNew = true;
                }
            }
            else if (reward.rewardType == GachaRewardType.Item)
            {
                // shop item reward is not yet
            }
            resultList.Add((reward.rewardType, reward.protoId, isNew, reward.isSoulStoneMax));
        }

        return resultList;
    }

    public async Task<InventoryResult> RequestGetMileageRewards(int gachaProtoId)
    {
        var result = await ServerManager.GameService.GetMileageRewards(gachaProtoId);

        // update mileage grade
        var proto = ProtoData.Current.GachaShopProtos.Get(gachaProtoId);
        if (ProtoData.Current.GachaShopMileages.TryGet(proto.MileageSid, out var mileageProto))
        {
            var mileageData = m_UserData.ItemDatas.GetDataByProtoSid(mileageProto.MileageItemSid) as GachaShopMileageItemData;
            var mileageRewardRoopCount = mileageData.Count / mileageProto.MileageInformations[mileageData.GetMaxMileageGrade(mileageProto) - 1].Point;
            var roopCount = mileageRewardRoopCount < 1 ? 1 : mileageRewardRoopCount;
            for (int i = 0; i < roopCount; i++)
            {
                foreach (var info in mileageProto.MileageInformations)
                {
                    if (!mileageData.IsAbleGetMileageReward(mileageProto, info.Grade)) continue;

                    mileageData.UpdateMileageGrade();
                }
                var maxMileageGrade = mileageData.GetMaxMileageGrade(mileageProto);
                if (mileageData.MileageStepGrade >= maxMileageGrade)
                {
                    mileageData.ResetMileage(mileageProto);
                }
            }
        }

        m_UserData.ApplyInventoryResult(result);

        return result;
    }
}
