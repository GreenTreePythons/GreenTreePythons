using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "SpaceTimelineInformations", menuName = "ScriptableObject/SpaceTimelineInformations")]
public class SpaceTimelineInformations : ScriptableObject
{
    [SerializeField] string m_BundlePath;
    [SerializeField] SpaceTimelineInfo[] m_SpaceTimelineInfos;
    
    public SpaceTimelineInfo GetSpaceTimelineInfo(SpaceTimelineType timelineType) 
        => m_SpaceTimelineInfos.SingleOrDefault(t => t.GetSpaceTimelineType() == timelineType);
}

[System.Serializable]
public class SpaceTimelineInfo
{
    public SpaceTimelineType m_TimelineType;
    public AssetReferenceGameObject m_AssetReferenceGameObject;

    public object GetCutSceneAddress() => m_AssetReferenceGameObject.RuntimeKey;
    public SpaceTimelineType GetSpaceTimelineType() => m_TimelineType;
}