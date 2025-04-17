public async UnaryResult<Nil> SaveProfileImage(int iconProtoId, int outLineProtoId)
{
    var user = await UserQuery.Include(u => u.PlayerData).SingleAsync();

    await ValidateProfileImage(iconProtoId);
    await ValidateProfileImage(outLineProtoId);
    user.PlayerData.UpdateProfileImage(AccountProfileImageType.Icon, iconProtoId);
    user.PlayerData.UpdateProfileImage(AccountProfileImageType.OutLine, outLineProtoId);

    await DbContext.SaveChangesAsync();
    return Nil.Default;
}

public async UnaryResult<Nil> SaveIllustImage(int illustProtoId)
{
    var user = await UserQuery.Include(u => u.PlayerData).SingleAsync();

    await ValidateProfileImage(illustProtoId);
    user.PlayerData.UpdateProfileImage(AccountProfileImageType.Illust, illustProtoId);

    await DbContext.SaveChangesAsync();
    return Nil.Default;
}

public async UnaryResult<Nil> SaveProfileText(AccountInfoEditType editType, string text)
{
    var user = await UserQuery.Include(u => u.PlayerData).SingleAsync();

    ValidateProfileText(user.PlayerData, editType, text);
    user.PlayerData.UpdateProfileText(editType, text);

    await DbContext.SaveChangesAsync();
    return Nil.Default;
}

public async UnaryResult<(InventoryResult inven, bool isLevelUp)> CompleteExploreMission(int missionProtoId)
{
    var user = await UserQuery.Include(u => u.PlayerData)
                              .Include(u => u.UserActionData).SingleAsync();

    CompleteExploreMisisonInternal(user, missionProtoId);

    var isLevelup = user.PlayerData.IsPossibleExploreLevelUp();
    if (isLevelup) user.PlayerData.ExploreLevelUp();

    await Inventory.GrantItemsAsync();
    await DbContext.SaveChangesAsync();

    return (Inventory.Result, isLevelup);
}

public async UnaryResult<(InventoryResult inven, bool isLevelUp)> CompleteAllExploreMission(List<int> missionProtoIds)
{
    var user = await UserQuery.Include(u => u.PlayerData)
                              .Include(u => u.UserActionData).SingleAsync();

    foreach (var missionProtoId in missionProtoIds)
    {
        CompleteExploreMisisonInternal(user, missionProtoId);
    }

    var isLevelup = user.PlayerData.IsPossibleExploreLevelUp();
    if (isLevelup) user.PlayerData.ExploreLevelUp();

    await Inventory.GrantItemsAsync();
    await DbContext.SaveChangesAsync();

    return (Inventory.Result, isLevelup);
}

void CompleteExploreMisisonInternal(UserData user, int missionProtoId)
{
    ValidateExploreMission(user, missionProtoId);

    user.PlayerData.UpdateClearExploreMission(missionProtoId);
    var accountLevelProto = ProtoData.Current.AccountLevels.Get(missionProtoId);
    if (accountLevelProto.HasReward)
    {
        Inventory.GrantRequest.AddItem(Proto.Items.Get(accountLevelProto.RewardSid).CreateData(accountLevelProto.RewardAmount));
    }
}

private async void ValidateExploreMission(UserData userData, int missionProtoId)
{
    var exploreMissionProto = ProtoData.Current.AccountLevels.SubDictionary[userData.PlayerData.ExploreLevel].Get(missionProtoId);
    var result = await CanClearAsync(exploreMissionProto.QuestCondition);
    if (result == false)
        throw new InvalidOperationException($"cant clear mission {missionProtoId}");
}

private void ValidateProfileText(PlayerData playerData, AccountInfoEditType editType, string text)
{
    if (editType != AccountInfoEditType.NickName && editType != AccountInfoEditType.Message)
    {
        throw new InvalidOperationException($"cant save profile data : {editType} {text}");
    }
    if (editType == AccountInfoEditType.NickName)
    {
        if (!playerData.IsPossibleUpdateNickName())
        {
            throw new InvalidOperationException($"cant update profile data : {editType} {text}");
        }
    }
}

private async UnaryResult ValidateProfileImage(int protoId)
{
    var profileProto = ProtoData.Current.AccountProfiles.Get(protoId);
    if (profileProto.ProfileImageGetType == AccountProfileImageGetType.CharacterGet ||
        profileProto.ProfileImageGetType == AccountProfileImageGetType.CharacterEvo)
    {
        await ValidateCharacterDataAsync(profileProto.ProfileImageGetType, profileProto.TypeSid);
    }
    else if (profileProto.ProfileImageGetType == AccountProfileImageGetType.ItemCollection)
    {

    }
}

private async UnaryResult ValidateCharacterDataAsync(AccountProfileImageGetType getType, string targetCharacterSid)
{
    var characterProto = ProtoData.Current.Characters.Get(targetCharacterSid);
    var characterData = await UserQuery.SelectMany(u => u.CharacterDatas)
                                        .Where(u => u.OriginProtoId == characterProto.Id)
                                        .SingleOrDefaultAsync();

    if (getType == AccountProfileImageGetType.CharacterGet)
    {
        if (characterData == null)
        {
            throw new InvalidOperationException($"Character data not found for ProtoId: {characterProto.Id}");
        }
    }
    else if (getType == AccountProfileImageGetType.CharacterEvo)
    {
        if (!characterData.IsEvolutionMax())
        {
            throw new InvalidOperationException($"not valuable: {characterProto.Id}");
        }
    }
}