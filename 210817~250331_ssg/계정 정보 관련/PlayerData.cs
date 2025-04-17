[MessagePackObject]
public partial class PlayerData
{
    [Key(0)]
    public int Id { get; set; }
    [Key(1)]
    public string NickName { get; set; }
    [Key(2)]
    public int OriginOfStarGrade { get; set; }
    [Key(3)]
    public int LastSpaceDataId { get; set; }
    [Key(4), DBJson(true)]
    public ProfileInfo ProfileInfo { get; set; } = new();
    [Key(5)]
    public int ExploreLevel { get; set; } = 1;
    [Key(6), DBJson(true)]
    public List<int> ClearedExploreMissionProtoIds { get; set; } = new();
}

[MessagePackObject]
public class ProfileInfo
{
    [Key(0)]
    public int IconProtoId { get; set; }
    [Key(1)]
    public int OutLineProtoId { get; set; }
    [Key(2)]
    public int IllustProtoId { get; set; }
    [Key(3)]
    public string IntroductionMessage { get; set; } = string.Empty;
    [Key(4)]
    public DateTime NickNameUpdateTime { get; set; }
}