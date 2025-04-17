using System.Linq;
using UnityEngine;

public enum NodeUIType
{
    Icon,
    BackGround,
    Stroke
}

[CreateAssetMenu(fileName = "NodeColorInformation", menuName = "ScriptableObject/NodeColorInformation")]
public class NodeColorInformation : ScriptableObject
{
    [SerializeField] NodeColorByState m_NodeColorByState;
    [SerializeField] NodeColorByCharacterElement m_NodeColorByCharacterElement;
    [SerializeField] NodeColorByIslandRarity m_NodeColorByIslandRarity;

    public Color GetNodeColor(NodeUIType uiType, NodeUIState uiState)
        => GetColor(m_NodeColorByState.GetNodeUIInfo(uiState), uiType);

    public Color GetCharacterNodeColor(NodeUIType uiType, NodeUIState uiState, CharacterElement characterElement)
        => GetColor(m_NodeColorByCharacterElement.GetNodeUIInfo(uiState, characterElement), uiType);

    public Color GetCharacterRarityColor(NodeUIType uiType, CharacterRarity rarity)
        => GetColor(m_NodeColorByIslandRarity.GetNodeUIInfo(rarity), uiType);

    public Color GetIslandIconColor(NodeUIType uiType, IslandRarity islandRarity)
        => GetColor(m_NodeColorByIslandRarity.GetNodeUIInfo(islandRarity), uiType);

    private Color GetColor(NodeUIInfo[] nodeUIInfos, NodeUIType uiType)
        => nodeUIInfos.Single(i => i.GetNodeUIType() == uiType).GetNodeUIColor();
}

[System.Serializable]
public class NodeColorByState
{
    [SerializeField] NodeUIInfo[] m_ImpossibleStateInfo;
    [SerializeField] NodeUIInfo[] m_PossibleStateInfo;
    [SerializeField] NodeUIInfo[] m_CompleteStateInfo;

    public NodeUIInfo[] GetNodeUIInfo(NodeUIState uiState) => uiState switch
    {
        NodeUIState.Impossible => m_ImpossibleStateInfo,
        NodeUIState.Possible => m_PossibleStateInfo,
        NodeUIState.Complete => m_CompleteStateInfo,
        _ => null
    };
}

[System.Serializable]
public class NodeColorByCharacterElement
{
    [SerializeField] NodeUIInfo[] m_ImpossibleStateInfo;
    [SerializeField] NodeUIInfo[] m_PossibleStateInfo;
    [SerializeField] NodeUIInfo[] FireCompleteStateInfo;
    [SerializeField] NodeUIInfo[] WaterCompleteStateInfo;
    [SerializeField] NodeUIInfo[] TreeCompleteStateInfo;
    [SerializeField] NodeUIInfo[] LightCompleteStateInfo;
    [SerializeField] NodeUIInfo[] DarkCompleteStateInfo;

    public NodeUIInfo[] GetNodeUIInfo(NodeUIState uiState, CharacterElement characterElement)
    {
        if (uiState == NodeUIState.Impossible) return m_ImpossibleStateInfo;
        else if (uiState == NodeUIState.Possible) return m_PossibleStateInfo;
        else if (uiState == NodeUIState.Complete)
        {
            switch (characterElement)
            {
                case CharacterElement.Fire: return FireCompleteStateInfo;
                case CharacterElement.Water: return WaterCompleteStateInfo;
                case CharacterElement.Tree: return TreeCompleteStateInfo;
                case CharacterElement.Light: return LightCompleteStateInfo;
                case CharacterElement.Dark: return DarkCompleteStateInfo;
            }
        }
        return null;
    }
}

[System.Serializable]
public class NodeColorByIslandRarity
{
    public NodeUIInfo[] m_CommonIslandIconInfo;
    public NodeUIInfo[] m_RareIslandIconInfo;
    public NodeUIInfo[] m_UncommonIslandIconInfo;
    public NodeUIInfo[] m_EpicIslandIconInfo;
    public NodeUIInfo[] m_LegendaryIslandIconInfo;
    public NodeUIInfo[] m_EventIslandIconInfo;

    public NodeUIInfo[] GetNodeUIInfo(CharacterRarity rarity) => rarity switch
    {
        CharacterRarity.Rare => GetNodeUIInfo(IslandRarity.Rare),
        CharacterRarity.Uncommon => GetNodeUIInfo(IslandRarity.Uncommon),
        CharacterRarity.Epic => GetNodeUIInfo(IslandRarity.Epic),
        CharacterRarity.Legendary => GetNodeUIInfo(IslandRarity.Legendary),
        _ => null
    };

    public NodeUIInfo[] GetNodeUIInfo(IslandRarity islandRarity) => islandRarity switch
    {
        IslandRarity.Common => m_CommonIslandIconInfo,
        IslandRarity.Rare => m_RareIslandIconInfo,
        IslandRarity.Uncommon => m_UncommonIslandIconInfo,
        IslandRarity.Epic => m_EpicIslandIconInfo,
        IslandRarity.Legendary => m_LegendaryIslandIconInfo,
        IslandRarity.Event => m_EventIslandIconInfo,
        _ => null
    };
}

[System.Serializable]
public class NodeUIInfo
{
    [SerializeField] NodeUIType m_NodeUIType;
    [SerializeField] Color m_ColorValue;

    public NodeUIType GetNodeUIType() => m_NodeUIType;
    public Color GetNodeUIColor() => m_ColorValue;
}