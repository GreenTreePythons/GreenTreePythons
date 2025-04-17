using System.Collections;
using ReuseScroller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseTabCell : ReuseCell<int>
{
    [SerializeField] BundledImage m_IconImage;
    [SerializeField] TextMeshProUGUI m_NameText;

    [SerializeField] protected UIAnimation m_Animation;
    [SerializeField] protected GameObject m_SelectedEffect;
    [SerializeField, RequiredReference(false)] protected AnimationClip m_FirstIntroAnimation;
    [SerializeField, RequiredReference(false)] protected AnimationClip m_IntroAnimation;
    [SerializeField, RequiredReference(false)] protected AnimationClip m_ActiveAnimation;
    [SerializeField, RequiredReference(false)] protected ContentDotSet m_ContentDotSet;

    int m_Index;
    BaseTabPanel m_TabPanel;
    Coroutine m_AnimationCoroutine;

    public int GetIndex() => m_Index;

    protected override void Awake()
    {
        base.Awake();
        if (m_Animation != null)
        {
            m_Animation.AutoPlay = false;
            m_Animation.AutoPlayOnlyVisible = false;
            m_Animation.AnimationToPlay = null;
        }
        m_TabPanel = this.GetComponentInParent<BaseTabPanel>();
    }

    public virtual void Initialize(int index)
    {
        m_Index = index;
        if (m_SelectedEffect) m_SelectedEffect.SetActive(false);
    }

    public void SetData(string nameSid, BundlePath bundlePath)
    {
        SetName(nameSid);
        StartCoroutine(m_IconImage.CoLoadAsync(bundlePath));
    }

    public void SetData(string nameSid)
    {
        SetName(nameSid);
    }

    public void SetName(string nameSid) => m_NameText.text = StringTable.Get(nameSid);

    public virtual void DisableTabAnimation()
    {
        InitCoroutine();
        if (m_Animation != null)
        {
            m_Animation.StopAllCoroutines();
            m_Animation.AnimationToPlay = m_ActiveAnimation;
            m_Animation.SampleProgress(0f);
        }
        if (m_SelectedEffect) m_SelectedEffect.SetActive(false);
    }

    public virtual void AbleTabAnimation()
    {
        PlayAnimation(false);
        if (m_SelectedEffect) m_SelectedEffect.SetActive(true);
    }

    public void PlayAnimation(bool isIntro, int startIndex = 0)
    {
        InitCoroutine();
        m_AnimationCoroutine = StartCoroutine(CoPlayAnimation(isIntro, startIndex));
    }

    public virtual void OnClick()
    {
        var panel = GetComponentInParent<BaseTabPanel>();
        if (panel != null) panel.ChangeTab(GetIndex());
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        m_TabPanel.ChangeTab(GetIndex());
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        m_TabPanel.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        m_TabPanel.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        m_TabPanel.OnEndDrag(eventData);
    }

    public float GetTabCurrentAnimLength() => m_Animation.AnimationToPlay.length;

    private IEnumerator CoPlayAnimation(bool isIntro, int startIndex = 0)
    {
        if (m_SelectedEffect) m_SelectedEffect.SetActive(m_Index == startIndex);

        InitCoroutine();
        if (m_Animation != null)
        {
            if (isIntro && m_Index == startIndex)
            {
                m_Animation.AnimationToPlay = m_FirstIntroAnimation;
            }
            else
            {
                m_Animation.AnimationToPlay = isIntro ? m_IntroAnimation : m_ActiveAnimation;
            }
            m_Animation.SampleProgress(0f);
            m_AnimationCoroutine = m_Animation.ShowAnimation();
            yield return m_AnimationCoroutine;
        }
    }

    private void InitCoroutine()
    {
        if (m_AnimationCoroutine != null)
        {
            StopCoroutine(m_AnimationCoroutine);
            m_AnimationCoroutine = null;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}