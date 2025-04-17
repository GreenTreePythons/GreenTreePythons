using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

namespace Latecia.Shared
{
    [MessagePackObject]
    public partial class ShipData
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public int ProtoId { get; set; }
        [Key(2), DBJson(true)]
        public List<ShipCommand> Commands { get; set; } = new();
        [Key(3), DBJson(true)]
        public Vector2 LastPosition { get; set; }
        [Key(4)]
        public DateTime LastUpdateTimeUTC { get; set; }
        [Key(5), DBJson(true)]
        public ShipPartsInformation BridgePart { get; set; } = new();
        [Key(6), DBJson(true)]
        public ShipPartsInformation BodyPart { get; set; } = new();
        [Key(7), DBJson(true)]
        public ShipPartsInformation EnginePart { get; set; } = new();
        [Key(8), DBJson(true)]
        public ShipPartsInformation HeadPart { get; set; } = new();
        [Key(9), DBJson(true)]
        public ShipPartsInformation WingPart { get; set; } = new();
    }

    [MessagePackObject]
    public class ShipPartsInformation
    {
        [Key(0)]
        public int PartsItemProtoId { get; set; }
        [Key(1)]
        public ShipPartsColor PartsColor { get; set; }

        public void SetShipPartsInformation(int itemProtoId, ShipPartsColor partsColor)
        {
            PartsItemProtoId = itemProtoId;
            PartsColor = partsColor;
        }
    }

    [MessagePackObject]
    public class PositionSnapshot
    {
        [Key(0)]
        public Vector2 LastPosition { get; set; }
        [Key(1)]
        public DateTime LastPositionTimeUTC { get; set; }

        public PositionSnapshot(Vector2 lastPosition, DateTime lastPositionTimeUtc)
        {
            LastPosition = lastPosition;
            LastPositionTimeUTC = lastPositionTimeUtc;
        }
    }

    [MessagePackObject]
    public class ShipCommand
    {
        [Key(0)]
        public List<Vector2> Path { get; set; }
        [Key(1)]
        public TimeSpan Duration { get; set; }
        [Key(2)]
        public TimeSpan DockDuration { get; set; }
        [Key(3)]
        public bool IsTeleport { get; set; }
        [Key(4)]
        public int TargetIsland { get; set; }
    }
}
