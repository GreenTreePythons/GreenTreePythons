#if UNITY_EDITOR
    public void GenerateBranchs()
    {
        foreach (var node in m_Nodes)
        {
            node.SetBranchs();
        }
    }
    public void ResetBracnhs()
    {
        foreach (var node in m_Nodes)
        {
            node.ResetBranchs();
        }
    }
    public void RepositionNodes()
    {
        foreach (var node in m_Nodes)
        {
            var nextNodes = node.GetNextNodeInfos();
            if (nextNodes.Length == 0) continue;
            for(int i = 0; i < nextNodes.Length; i++)
            {
                var nextNodeProtoId = nextNodes[i].GetNextNodeProtoId();
                var branch = nextNodes[i].GetBranch();
                if(branch == null) continue;
                var nextNode = m_Nodes.Single(n => n.GetBlessingNodeProtoId() == nextNodeProtoId);
                var branchPos = node.GetGeneratedBranchPos(i);
                var branchInfo = branch.GetBranchInfo();
                var nextNodePos = default(Vector2);
                var nodeRect = node.GetComponent<RectTransform>().rect;
                switch (branchInfo.direction)
                {
                    case BlessingOfStarNodeBranchDirection.Straight:
                        nextNodePos = new Vector2(branchPos.x, 
                                                  branchPos.y + (branchInfo.rect.height * 0.25f));
                        break;
                    case BlessingOfStarNodeBranchDirection.Left:
                        nextNodePos = new Vector2(branchPos.x - (branchInfo.rect.width * 0.2f), 
                                                  branchPos.y + (nodeRect.height * 0.5f));
                        break;
                    case BlessingOfStarNodeBranchDirection.Right:
                        nextNodePos = new Vector2(branchPos.x + (branchInfo.rect.width * 0.2f), 
                                                  branchPos.y + (nodeRect.height * 0.5f));
                        break;
                }
                nextNode.gameObject.transform.position = nextNodePos;
            }
        }
    }
#endif

    public List<BlessingOfStarNode> GetNodes() => m_Nodes;
    public BlessingOfStarNode GetSpecificNode(int protoId) => m_Nodes.Single(n => n.GetBlessingNodeProtoId() == protoId);
    public bool TryGetSpecificNode(int protoId, out BlessingOfStarNode blessingOfStarNode)
    {
        blessingOfStarNode = m_Nodes.SingleOrDefault(n => n.GetBlessingNodeProtoId() == protoId);
        return blessingOfStarNode != null;
    }

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(BlessingOfStarNodeGroup), true)]
public class BranchGenerator : UnityEditor.Editor
{
    BlessingOfStarNodeGroup m_NodeGroup;
    void OnEnable() => m_NodeGroup = (BlessingOfStarNodeGroup)target;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Generate branchs")) m_NodeGroup.GenerateBranchs();
        if(GUILayout.Button("Reset branchs")) m_NodeGroup.ResetBracnhs();
        if(GUILayout.Button("Reposition nodes")) m_NodeGroup.RepositionNodes();
    }
}
#endif

#if UNITY_EDITOR
    public void ResetBranchs()
    {
        foreach (Transform child in m_BranchsRoot)
        {
            DestroyImmediate(child.gameObject);
        }
    }
    public void SetBranchs()
    {
        foreach (var nextNode in m_NextNodes)
        {
            var branch = Instantiate(nextNode.GetBranch(), m_BranchsRoot);
            var branchInfo = branch.GetComponent<BlessingOfStarNodeBranch>().GetBranchInfo();
            var branchPos = default(Vector2);
            var nodeRect = this.GetComponent<RectTransform>().rect;
            if (branchInfo.direction == BlessingOfStarNodeBranchDirection.Straight)
            {
                branchPos = new Vector2(this.transform.position.x,
                this.transform.position.y + branchInfo.rect.height * 0.25f);
            }
            else if (branchInfo.direction == BlessingOfStarNodeBranchDirection.Left)
            {
                branchPos = new Vector2(this.transform.position.x - branchInfo.rect.width * 0.25f,
                this.transform.position.y + nodeRect.height * 0.5f);
            }
            else if (branchInfo.direction == BlessingOfStarNodeBranchDirection.Right)
            {
                branchPos = new Vector2(this.transform.position.x + branchInfo.rect.width * 0.25f,
                this.transform.position.y + nodeRect.height * 0.5f);
            }
            branch.transform.position = branchPos;
        }
    }
    public Vector2 GetGeneratedBranchPos(int index, bool isLocalPos = false)
    {
        var pos = new Vector2();
        var branch = m_BranchsRoot.GetChild(index);
        var branchLocalPos = branch.transform.localPosition;
        pos = isLocalPos == false ? transform.TransformPoint(branchLocalPos) : branchLocalPos;
        return pos;
    }
#endif