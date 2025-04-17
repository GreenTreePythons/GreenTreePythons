using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class BaseTabPanel : SubView, 
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] protected BaseTabCell[] m_TabCellArray;
    [SerializeField] float m_TabStartX = -300.0f;
    [SerializeField] float m_TabTopPadding = 100.0f;
    [SerializeField] float m_VerticalTabSpacing = 4.0f;
    [SerializeField] float m_CellAnimationWaitingTime = 0.1f;

    protected int m_CurrentTabIndex;
    int m_PreviousTabIndex;
    ScrollRect m_ScrollRect;

    public int GetCurrentIndex() => m_CurrentTabIndex;
    public int GetPreviousIndex() => m_PreviousTabIndex;
    public BaseTabCell[] GetTabCells() => m_TabCellArray;

    public virtual void Initialize(int startIndex) => InternerInitialize(startIndex);
    public virtual void Initialize() => InternerInitialize(0);

    protected override void Awake()
    {
        base.Awake();
        m_ScrollRect = this.GetComponentInChildren<ScrollRect>();
    }

    public void Initialize(int startIndex, List<BaseTabCell> tabCells)
    {
        if (tabCells.Count == 0) return;

        m_TabCellArray = new BaseTabCell[tabCells.Count];
        for (int i = 0; i < tabCells.Count; i++)
        {
            m_TabCellArray[i] = tabCells[i];
            m_TabCellArray[i].Initialize(i);
        }

        InternerInitialize(startIndex);
    }

    public virtual void ChangeTab(int index)
    {
        if (m_CurrentTabIndex == index) return;
        m_PreviousTabIndex = m_CurrentTabIndex;
        m_CurrentTabIndex = index;
        DisableAllTabs();
        GetTabCell(index).AbleTabAnimation();
    }

    public BaseTabCell GetTabCell(int index)
    {
        if (index >= 0 && index < m_TabCellArray.Length) return m_TabCellArray[index];
        return null;
    }

    public void ShowChangeTabAnimationOnly() => GetTabCell(m_CurrentTabIndex).AbleTabAnimation();

    public ScrollRect GetScrollRect() => m_ScrollRect;

    private void InternerInitialize(int startIndex)
    {
        m_PreviousTabIndex = startIndex;
        m_CurrentTabIndex = startIndex;
        InitializeScrollRect();
        InitializeCells(m_CurrentTabIndex);
    }

    private void InitializeScrollRect()
    {
        m_ScrollRect.verticalNormalizedPosition = 1.0f;
        // Applies InitializeScrollRect() when the UI is turned on this component when working
        m_ScrollRect.movementType = ScrollRect.MovementType.Elastic;
        if (m_ScrollRect.content.TryGetComponent<ContentSizeFitter>(out var contentSizeFitter))
        {
            contentSizeFitter.enabled = false;
        }
        if (m_ScrollRect.content.TryGetComponent<VerticalLayoutGroup>(out var verticalLayoutGroup))
        {
            verticalLayoutGroup.enabled = false;
        }
        SetTabLayOut();
    }

    private void InitializeCells(int startIndex)
    {
        for (int i = 0; i < m_TabCellArray.Length; i++)
        {
            m_TabCellArray[i].Initialize(i);
            m_TabCellArray[i].transform.localPosition = new Vector3(m_TabStartX, m_TabCellArray[i].transform.localPosition.y, 0);
        }
        StartCoroutine(CoPlayAnimations(startIndex));
    }

    private IEnumerator CoPlayAnimations(int startIndex = 0)
    {
        foreach (var cell in m_TabCellArray)
        {
            if (!cell.gameObject.activeSelf) continue;
            cell.PlayAnimation(true, startIndex);
            yield return new WaitForSecondsRealtime(m_CellAnimationWaitingTime);
        }
    }

    private void SetTabLayOut()
    {
        var totalHeight = m_TabTopPadding;
        for (int i = 0; i < m_TabCellArray.Length; i++)
        {
            if (m_TabCellArray[i] == null) continue;
            if (!m_TabCellArray[i].gameObject.activeInHierarchy) continue;
            var tabCellHeight = m_TabCellArray[i].GetComponent<RectTransform>().rect.height;
            var xPoint = m_TabStartX;
            var yPoint = totalHeight + (tabCellHeight * 0.5f);
            m_TabCellArray[i].transform.localPosition = new Vector2(xPoint, -yPoint);
            totalHeight += tabCellHeight + m_VerticalTabSpacing;
        }
        var contentRect = m_ScrollRect.content.GetComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(contentRect.rect.width, totalHeight);
    }

    private void DisableAllTabs()
    {
        foreach (var cell in m_TabCellArray)
        {
            cell.DisableTabAnimation();
        }
    }

    public void DestroyAllTabs()
    {
        for (int i = 0; i < m_ScrollRect.content.childCount; i++)
        {
            Destroy(m_ScrollRect.content.GetChild(i).gameObject);
        }
    }


    void Update()
    {
        if (Application.isPlaying) return;
        if (m_TabCellArray == null) return;
        if (m_TabCellArray.Length == 0) return;
        if (m_TabCellArray[0] == null) return;
        SetTabLayOut();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_ScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        m_ScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_ScrollRect.OnEndDrag(eventData);
    }

}

public interface IViewAttachTab
{
    public void OnSelectTabInTab(int index, int innerIndex);
}