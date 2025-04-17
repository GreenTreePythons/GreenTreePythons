using System.Collections.Generic;
using System.Threading.Tasks;
using ContentDot;
using Latecia.Shared;
using UnityEngine;

public partial class GameServiceRequests
{
    public async Task<bool> CharacterEvolution(int characterDataId)
    {
        var req = await ServerManager.GameService.CharacterEvolution(characterDataId);
        m_UserData.ApplyInventoryResult(req);
        m_UserData.UserAction.CharacterEvolutionCount += 1;
        m_ContentDotManager.UpdatedCharacterAwakening(m_UserData.CharacterDatas.GetDataById(characterDataId));

        return true;
    }

    public async Task<bool> CharacterAwakening(int characterDataId)
    {
        var req = await ServerManager.GameService.CharacterAwakening(characterDataId);

        m_UserData.ApplyInventoryResult(req);
        var characterData = m_UserData.CharacterDatas.GetDataById(characterDataId);
        characterData.AwakeningGrade++;
        m_UserData.UserAction.CharacterAwakeningCount++;
        m_ContentDotManager.UpdatedCharacterAwakening(characterData);

        return true;
    }

    //Specificity
    public async Task<bool> CharacterSpecificityLevelUp(int characterDataId, SpecificityCharacterGrowthProto specificityProto, bool isSpecificity)
    {
        var cData = m_UserData.CharacterDatas.GetDataById(characterDataId);
        cData.GetSpecificityLevelUpCost(specificityProto.Id, out var costs, out var goldCost);
        if (!m_UserData.ItemDatas.IsEnoughItems(costs) || !m_UserData.ItemDatas.IsEnoughItem(ProtoDataConst.GOLD, goldCost)) return false;
        
        var req = await ServerManager.GameService.SpecificityLevelUp(characterDataId, specificityProto.Id);
        
        m_UserData.ApplyInventoryResult(req);
        
        cData.SpecificityLevelUp(specificityProto.Id);

        m_UserData.UserAction.CharacterSpecificityCount += 1;
        m_ContentDotManager.UpdatedCharacterSpecificity(cData);
        return true;
    }
}