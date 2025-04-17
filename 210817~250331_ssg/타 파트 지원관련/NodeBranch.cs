using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BlessingOfStarNodeBranchDirection
{
    Straight,
    Left,
    Right
}

[RequiredReference]
public class BlessingOfStarNodeBranch : MonoBehaviour
{
    [SerializeField] BlessingOfStarNodeBranchDirection m_Direction;
    
    public (Rect rect, BlessingOfStarNodeBranchDirection direction) GetBranchInfo()
        => (this.GetComponent<RectTransform>().rect, m_Direction);
}