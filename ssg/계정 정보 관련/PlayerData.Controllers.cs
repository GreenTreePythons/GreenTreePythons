public void UpdateProfileImage(AccountProfileImageType imageType, int protoId)
{
    switch (imageType)
    {
        case AccountProfileImageType.Icon:
            ProfileInfo.IconProtoId = protoId;
            break;
        case AccountProfileImageType.OutLine:
            ProfileInfo.OutLineProtoId = protoId;
            break;
        case AccountProfileImageType.Illust:
            ProfileInfo.IllustProtoId = protoId;
            break;
    }
}

public bool IsPossibleUpdateNickName() => (ServerTime.GetUTC() - ProfileInfo.NickNameUpdateTime).TotalDays >= ProtoDataConst.NICKNAME_DURATION_DAYTIME;

public void UpdateProfileText(AccountInfoEditType editType, string text)
{
    if (editType == AccountInfoEditType.NickName)
    {
        NickName = text;
        ProfileInfo.NickNameUpdateTime = ServerTime.GetUTC();
    }
    else if (editType == AccountInfoEditType.Message)
    {
        ProfileInfo.IntroductionMessage = text;
    }
}

public void UpdateClearExploreMission(int missionProtoId)
{
    if (ClearedExploreMissionProtoIds.Contains(missionProtoId)) return;
    ClearedExploreMissionProtoIds.Add(missionProtoId);
}

public bool IsPossibleExploreLevelUp()
{
    var currentLevelGroupProtos = ProtoData.Current.AccountLevels.SubDictionary[ExploreLevel];
    return currentLevelGroupProtos.All(p => ClearedExploreMissionProtoIds.Contains(p.GetId()));
}

public void ExploreLevelUp() => ExploreLevel++;