using System;
using System.Collections;
using Coffee.UIExtensions;
using Latecia.Shared;
using TMPro;
using UnityEngine;
using ViewSystem;

[RequiredReference]
public class IslandHUD : MonoBehaviour,
    IServiceLocatorComponent,
    IServiceLocatorSetupComponent
{
    [SerializeField] IslandColorInformation m_IslandColorInformation;
    [SerializeField] UIAnimation m_Animation;

    [Header("RenderInfo")]
    [SerializeField] float m_MinScale = 0.1f;
    [SerializeField] float m_MaxScale = 0.5f;
    [SerializeField] AnimationCurve m_ScaleCurve;
    [SerializeField] float m_ScaleMinDistance = 2.0f;
    [SerializeField] float m_ScaleMaxDistance = 30.0f;
    [SerializeField] float m_RenderDistance = 60.0f;
    [SerializeField] float m_RenderTextDistance = 30.0f;
    
    [SerializeField] CanvasGroup m_DockingUIGroup;
    [SerializeField] UIParticle m_DockingEffect;
    [SerializeField] UIParticle m_DockingCompleteEffect;
    [SerializeField] TextMeshProUGUI m_DistanceText;
    [SerializeField] TextMeshProUGUI m_NameText;
    [SerializeField] IslandIconWithGauge m_IslandIcon;
    [SerializeField] UIButton m_FoldButton;

    Camera m_MainCamera;
    CanvasGroup m_CanvasGroup;
    IslandActor m_IslandActor;
    float m_KmDistance;

    float m_EnteranceEventDistance;
    bool m_AlreadyShowSubDialog;
    string m_EnteranceSubDialogGroupSid;

    float m_DockableDistance;
    bool m_EnteredDockableArea;
    bool m_EnteredEventArea;
    bool m_IsDockableIslandHUD;

    bool m_IsDockingComplete;
    Coroutine m_UpdateIslandCoroutine;
    
    IServiceLocator m_Service;

    public bool IsFold { get; private set; }
    public bool IsEnteredDockableArea => m_EnteredDockableArea;
    public bool IsEnteredEventArea => m_EnteredEventArea;
    public float KmDistance => m_KmDistance;

    [Inject] ClientUserData m_UserData;
    [Inject] MessageBroker m_MessageBroker;
    [Inject] GameServiceRequests m_GameServiceRequests;
    
    void Awake()
    {
        IsFold = true;
        m_MainCamera = MainCameraActor.Instance;
        m_FoldButton.AddButtonEvent(OnInteraction);
        this.GetComponent<Canvas>().worldCamera = m_MainCamera;
        m_CanvasGroup = GetComponent<CanvasGroup>();
        m_RenderDistance = m_RenderDistance < 0 ? 0 : m_RenderDistance;
    }

    public void Initialize(IslandActor islandActor)
    {
        m_EnteredDockableArea = false;
        m_AlreadyShowSubDialog = false;
        m_DockingCompleteEffect.gameObject.SetActive(false);

        var islandProto = islandActor.Data.GetProto();
        m_IsDockableIslandHUD = islandProto is IDockableIsland || islandProto is IProximityIsland;
        m_IslandActor = islandActor;
        m_EnteranceSubDialogGroupSid = m_IslandActor.Data.GetProto().EnteranceSubDialogGroupSid;

        m_NameText.text = StringTable.Get(islandProto.NameSid);
        m_IslandIcon.SetData(m_IslandActor.Data, m_IslandColorInformation);

        m_DockableDistance = m_IslandActor.Data.GetDockableRadius();
        m_EnteranceEventDistance = m_IslandActor.Data.GetEnteranceEventRadius();

        m_DockingUIGroup.gameObject.SetActive(m_IsDockableIslandHUD);

        SetIslandHudUI();
    }

    void LateUpdate()
    {
        if (m_IslandActor == null) return;

        if(m_IslandActor.Data.State == IslandState.Cleared)
        {
            this.gameObject.SetActive(false);
            return;
        }

        var isVisible = MainCameraActor.Actor.IsTargetVisible(m_IslandActor.transform.position);
        if (!isVisible && !m_IsDockableIslandHUD) return;

        var islandActorPos = m_IslandActor.Data.GetServerPosition().ToClientPos();
        m_KmDistance = GetKmDistanceShipToIsland(islandActorPos);

        var isRender = m_KmDistance <= m_RenderDistance && IsFold;
        if (!isRender) return;

        this.transform.LookAt(transform.position + m_MainCamera.transform.rotation * Vector3.forward,
                            m_MainCamera.transform.rotation * Vector3.up);

        UpdateState(m_KmDistance);
        UpdatePosition(m_KmDistance);
        UpdateGauge();
        UpdateHUDScale(m_KmDistance);
    }

    public void SetIslandHudUI(bool isFoldState = true)
    {
        if (m_IslandActor.Data.GetIslandState() == IslandState.Inactive)
        {
            IsFold = true;
            SetHUDAlpha(0.0f);
        }
        else
        {
            IsFold = isFoldState;
            SetHUDAlpha(IsFold ? 1.0f : 0.0f);
            m_IslandIcon.SetData(m_IslandActor.Data, m_IslandColorInformation);
        }
    }

    void UpdateGauge()
    {
        if (m_IslandActor == null) return;
        if (m_UserData == null) return;
        float islandProgress = m_UserData.GetIslandProgress(m_IslandActor.Data, out _);
        m_IslandIcon.UpdateGauge(islandProgress);

        m_IsDockingComplete = false;
        if(m_IslandActor.Data.GetProto() is IDockableIsland)
        {
            if(m_IslandActor.Data.Progress != null)
            {
                var remainTime = m_IslandActor.Data.Progress.GetRemainTime();
                m_IsDockingComplete = remainTime <= TimeSpan.Zero;
            }
        }
    }

    void UpdateState(float kmDistanceFromShip)
    {
        if (m_IslandActor.Data.GetIslandState() != IslandState.Inactive) return;

        var isEnterEnteranceEventArea = kmDistanceFromShip <= m_EnteranceEventDistance;
        if (!isEnterEnteranceEventArea) return;

        if (m_UpdateIslandCoroutine != null) return;
        m_UpdateIslandCoroutine = StartCoroutine(CoRequestsFindIsland());
        IEnumerator CoRequestsFindIsland()
        {
            var req = m_GameServiceRequests.ActivateIsland(m_IslandActor.Data);
            yield return req;
            if (!req.IsCompletedSuccessfully) yield break;
            m_UpdateIslandCoroutine = null;
        }
    }

    void UpdatePosition(float kmDistanceFromShip)
    {
        m_RenderDistance = m_RenderDistance < 0 ? 0 : m_RenderDistance;
        m_RenderTextDistance = m_RenderTextDistance < 0 ? 0 : m_RenderTextDistance;

        var isEnterDockableArea = kmDistanceFromShip <= m_DockableDistance;
        var isEnterEnteranceEventArea = kmDistanceFromShip <= m_EnteranceEventDistance;

        // name text alpha control by distance
        var alphaValueByDistance = kmDistanceFromShip <= m_RenderTextDistance ? 255.0f : 0.0f;
        m_NameText.color = new Color(m_NameText.color.r, m_NameText.color.g, m_NameText.color.b, alphaValueByDistance);
        m_NameText.gameObject.SetActive(alphaValueByDistance != 0);

        // set hud position y when enter dockable area
        if (isEnterDockableArea)
        {
            float newY = m_DockableDistance - kmDistanceFromShip;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
        else
        {
            transform.localPosition = Vector3.zero;
        }

        // docking ui alpha control by distance
        if (m_IsDockableIslandHUD)
        {
            m_DockingEffect.gameObject.SetActive(isEnterDockableArea && !m_IsDockingComplete);

            // Set alpha to 1.0 if either isEnterDockableArea or m_IsDockingComplete is true otherwise, set to 0.3
            m_DockingUIGroup.alpha = (isEnterDockableArea || m_IsDockingComplete) ? 1.0f : 0.3f;
            m_DockingCompleteEffect.gameObject.SetActive(m_IsDockingComplete);
        }

        // distance text update by km distance
        m_DistanceText.text = $"{kmDistanceFromShip:0.0}km";

        // when dockable state changed, publish message to dockable island
        if (isEnterDockableArea != m_EnteredDockableArea)
        {
            if (m_IsDockableIslandHUD)
            {
                m_MessageBroker.Publish(new PublishEnterDockableArea()
                {
                    IslandDataId = m_IslandActor.Data.Id,
                    IsEntered = isEnterDockableArea
                });
            }
        }

        // enterance
        if (!m_AlreadyShowSubDialog)
        {
            if (m_EnteranceSubDialogGroupSid != null && isEnterEnteranceEventArea)
            {
                ViewManager.Get<MainRootPage>().ShowSubDialog(m_EnteranceSubDialogGroupSid);
                m_AlreadyShowSubDialog = true;
            }
        }

        m_EnteredDockableArea = isEnterDockableArea;
        m_EnteredEventArea = isEnterEnteranceEventArea;
    }

    void UpdateHUDScale(float kmDistanceFromShip)
    {
        float scale = 1.0f;

        // Calculate base scale based on distance from island to ensure gradual scaling
        if (kmDistanceFromShip >= m_ScaleMaxDistance)
        {
            scale = m_MaxScale;
        }
        else if (kmDistanceFromShip <= m_ScaleMinDistance)
        {
            scale = m_MinScale;
        }
        else
        {
            float normalizedDistance = Mathf.InverseLerp(m_ScaleMinDistance, m_ScaleMaxDistance, kmDistanceFromShip);
            scale = Mathf.Lerp(m_MinScale, m_MaxScale, m_ScaleCurve.Evaluate(normalizedDistance));
        }

        // Apply additional scaling factor based on distance to camera
        float distanceToCamera = Vector3.Distance(m_MainCamera.transform.position, this.transform.position);
        float distanceScaleFactor = Mathf.Lerp(m_MinScale, m_MaxScale, Mathf.Clamp01(distanceToCamera / m_ScaleMaxDistance));

        // Final scale that blends both the base scale and the distance factor
        this.transform.localScale = Vector3.one * (scale * distanceScaleFactor);
    }

    void OnInteraction()
    {        
        IsFold = false;
        if (m_IsDockableIslandHUD)
        {
            if (m_IslandActor.Data.State == IslandState.Cleared)
            {
                ViewManager.GetOverlay<ToastOverlay>().ShowToast(Strings.ISLAND_COMPLETE, ToastOverlay.Style.Info);
            }
            else
            {
                // render dockable island interaction modal
                RequestInteractionModal();
            }
        }
        else
        {
            // render indicator
            ViewManager.Get<MainRootPage>().OpenIslandIndicator(m_IslandActor);
        }
    }

    void RequestInteractionModal()
    {
        switch (m_IslandActor.Data.GetProto().Type)
        {
            case IslandType.Battle:
                StartCoroutine(_CoBattleIslandInteraction());
                IEnumerator _CoBattleIslandInteraction()
                {
                    var req = ViewRequest.Open<BattleIslandModal>(view => view.Open(m_IslandActor.Data));
                    yield return req;
                    SetIslandHudUI();
                }
                break;
            case IslandType.Dialog:
                StartCoroutine(CoIslandInteraction<DialogIslandInfoModal>(m_IslandActor.Data));
                break;
            case IslandType.CaseCollect:
                ViewManager.Get<MainRootPage>().OpenIslandIndicator(m_IslandActor);
                break;
            case IslandType.Finding:
                StartCoroutine(CoIslandInteraction<FindingIslandInfoModal>(m_IslandActor.Data));
                break;
            case IslandType.ObservationPost:
                StartCoroutine(CoIslandInteraction<ObservationPostIslandInfoModal>(m_IslandActor.Data));
                break;
        }   
    }

    IEnumerator CoIslandInteraction<T>(IslandData islandData) where T : BaseDockableIslandInfoModal
    {
        var req = ViewRequest.Open<T>(v => v.Open(islandData));
        yield return req;
        SetIslandHudUI();
    }

    float GetKmDistanceShipToIsland(Vector3 islandActorPos)
    {
        var shipActorPos = SpaceSceneManager.Instance.ShipActor.transform.position;
        // 1:20 in space scale
        return Vector3.Distance(islandActorPos, shipActorPos) * 0.02f;
    }

    void SetHUDAlpha(float alphaValue) => m_CanvasGroup.alpha = alphaValue;

    public void SetupServiceLocator(IServiceLocator service) 
    {
        m_Service = service;
    }
}