using System.Collections.Generic;
using System.Threading.Tasks;
using Latecia.Shared;

public partial class GameServiceRequests
{
    public async Task SaveProfileImage(int iconProtoId, int outLineProtoId)
    {
        var req = await ServerManager.GameService.SaveProfileImage(iconProtoId, outLineProtoId);
        
        var iconProto = ProtoData.Current.AccountProfiles.Get(iconProtoId);
        m_UserData.PlayerData.UpdateProfileImage(iconProto.ProfileImageType, iconProto.Id);
        var outLineProto = ProtoData.Current.AccountProfiles.Get(outLineProtoId);
        m_UserData.PlayerData.UpdateProfileImage(outLineProto.ProfileImageType, outLineProto.Id);
    }

    public async Task SaveIllustImage(int illustProtoId)
    {
        var req = await ServerManager.GameService.SaveIllustImage(illustProtoId);

        var outLineProto = ProtoData.Current.AccountProfiles.Get(illustProtoId);
        m_UserData.PlayerData.UpdateProfileImage(outLineProto.ProfileImageType, outLineProto.Id);
    }

    public async Task SaveProfileText(AccountInfoEditType editType, string text)
    {
        var req = await ServerManager.GameService.SaveProfileText(editType, text);
        m_UserData.PlayerData.UpdateProfileText(editType, text);
    }

    public async Task<(InventoryResult inven, bool isLevelUp)> CompleteExploreMission(int missionProtoId)
    {
        var req = await ServerManager.GameService.CompleteExploreMission(missionProtoId);
        m_UserData.PlayerData.UpdateClearExploreMission(missionProtoId);
        m_UserData.ApplyInventoryResult(req.inven);
        if(req.isLevelUp) m_UserData.PlayerData.ExploreLevelUp();
        return (req.inven, req.isLevelUp);
    }

    public async Task<(InventoryResult inven, bool isLevelUp)> CompleteAllExploreMission(List<int> missionProtoIds)
    {
        var req = await ServerManager.GameService.CompleteAllExploreMission(missionProtoIds);
        foreach(var clearedMissionProtoId in missionProtoIds)
        {
            m_UserData.PlayerData.UpdateClearExploreMission(clearedMissionProtoId);
        }
        m_UserData.ApplyInventoryResult(req.inven);
        if(req.isLevelUp) m_UserData.PlayerData.ExploreLevelUp();
        return (req.inven, req.isLevelUp);
    }
}