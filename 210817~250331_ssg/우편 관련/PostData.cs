using MessagePack;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Latecia.Shared
{
    [MessagePackObject]
    [Index(nameof(Type))]
    public partial class PostData : IBaseServerData
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public PostType Type { get; set; }
        [Key(2)]
        public bool IsGetReward { get; set; }
        [Key(3)]
        public string TitleText { get; set; }
        [Key(4)]
        public string DescriptionText { get; set; }
        [Key(5), DBJson(true)]
        public List<(int, long)> RewardPairs { get; set; } = new();
        [Key(7)]
        public DateTime DeleteDateTime { get; set; }
        [Key(8)]
        public DateTime ReceivedRewardDateTime { get; set; }
    }
}