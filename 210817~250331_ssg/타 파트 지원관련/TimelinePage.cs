
using System.Collections;
using System.Collections.Generic;
using Latecia.Shared;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ViewSystem;

[RequiredReference]
[ViewLoad("Common/MainTimelinePage")]
public class MainTimelinePage : PageView,
    IViewResult<bool>,
    IViewBackButtonListener
{
    [SerializeField] SpaceTimelineInformations m_SpaceTimelineInformations;
    [SerializeField] Image m_Background;
    [SerializeField] UIButton m_SkipButton;

    bool m_CameraEnabled;
    bool m_Cancelled = false;

    GameObject m_Root;
    GameObject m_Ship;

    Animator m_ShipAnimator;
    Vector3 m_PrevShipAnimatorLocalScale;

    SpaceTimelineInformation m_SpaceTimelineInformation;

    void Awake()
    {
        m_SkipButton.AddButtonEvent(OnSkipButton);
    }

    public void Open(SpaceTimelineType timelineType, List<ItemAmountInfo> rewardItems, GameObject island = null, bool showSkipButton = true)
    {
        Open(timelineType, island, showSkipButton);
        if (m_SpaceTimelineInformation == null) return;
        if(m_SpaceTimelineInformation.UIType == SpaceTimelineUIType.Rewards)
        {
            m_SpaceTimelineInformation.Open(rewardItems);
        }
    }

    public void Open(SpaceTimelineType timelineType, GameObject island = null, bool showSkipButton = true)
    {
        m_Root = new GameObject("Timeline_Root");
        m_SkipButton.SetActive(false);
        m_Cancelled = false;
        m_CameraEnabled = MainCameraActor.Instance.enabled;
        m_Background.color = Color.black;

        StartCoroutine(_PlayTimelineAndExit());
        if(showSkipButton) StartCoroutine(_ShowSkipButton(1f));

        IEnumerator _ShowSkipButton(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            m_SkipButton.SetActive(true);
        }

        IEnumerator _PlayTimelineAndExit()
        {
            yield return _PlayTimeline();
            StartCoroutine(WaitForEndOrFrameAndEnd(false));
        }

        IEnumerator _PlayTimeline()
        {
            if (MainCameraActor.Instance == null) yield break;

            SetIslandHUDCullingMask(true);

            m_Ship = SpaceSceneManager.Instance.ShipActor.ShipModel;
            m_ShipAnimator = m_Ship.GetComponentInChildren<Animator>();
            m_PrevShipAnimatorLocalScale = m_ShipAnimator.transform.localScale;

            var cutSceneAddress = m_SpaceTimelineInformations.GetSpaceTimelineInfo(timelineType)?.GetCutSceneAddress();
            var inst = BundleUtility.InstantiateAsync(cutSceneAddress, m_Root.transform).Wait();
            if (inst == null) yield break;
            
            //call handlers
            if (island != null && island.TryGetComponent<IslandActor>(out var actor))
            {
                foreach (var handler in inst.GetComponentsInChildren<ISpaceTimelineHandler>())
                {
                    handler.SetIsland(actor);
                }
            }

            m_SpaceTimelineInformation = inst.GetComponent<SpaceTimelineInformation>();
            if (m_SpaceTimelineInformation == null) 
            {
                SharedDebug.Log($"timelineType need SpaceTimelineInformation");
                yield break;
            }

            m_Background.CrossFadeAlpha(0, m_SpaceTimelineInformation.StartDimDuration, true);

            if (!m_SpaceTimelineInformation.UseTimeLinePos)
            {
                inst.transform.position = m_Root.transform.position;
                inst.transform.rotation = m_Root.transform.rotation;
            }

            var instPlayableDirector = inst.GetComponent<PlayableDirector>();
            if (instPlayableDirector == null) yield break;
            instPlayableDirector.extrapolationMode = DirectorWrapMode.Hold;

            if (island == null)
            {
                m_Root.transform.position = m_Ship.transform.position;
                m_Root.transform.rotation = m_Ship.transform.rotation;
            }
            else
            {
                SpaceIslandActor.ApplyTimelineConfiguration(m_Root, island, m_SpaceTimelineInformation.UseTimeLinePos);
            }

            m_Ship.transform.parent = m_Root.transform;
            m_Ship.transform.localPosition = Vector3.zero;
            m_Ship.transform.localRotation = Quaternion.identity;

            foreach (var vCam in instPlayableDirector.GetComponentsInChildren<CinemachineVirtualCamera>(true))
            {
                vCam.m_Lens.NearClipPlane = MainCameraActor.Instance.nearClipPlane;
                vCam.m_Lens.FarClipPlane = MainCameraActor.Instance.farClipPlane;
            }

            var timeline = (TimelineAsset)instPlayableDirector.playableAsset;
            instPlayableDirector.SetVolumesOfAudioTrack(SoundManager.VolumeGroup.Effect);

            bool shouldSheapAppear = false;

            foreach (var track in timeline.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "Ship":
                        instPlayableDirector.SetGenericBinding(track, m_Ship.GetComponentInChildren<Animator>());
                        shouldSheapAppear = true;
                        break;
                    case "Ship_Move":
                        instPlayableDirector.SetGenericBinding(track, m_Ship.GetComponentInChildren<Animator>());
                        shouldSheapAppear = true;
                        break;
                    case "Island":
                        instPlayableDirector.SetGenericBinding(track, island.GetComponentInChildren<Animator>());
                        break;
                    case "Island_Move":
                        instPlayableDirector.SetGenericBinding(track, island.GetComponentInChildren<Animator>());
                        break;
                }
            }

            MainCameraActor.Instance.enabled = true;
            m_Ship.gameObject.SetActive(m_SpaceTimelineInformation.ForceShowShip || shouldSheapAppear);

            instPlayableDirector.Play();
            var dimEndCalled = false;
            instPlayableDirector.extrapolationMode = DirectorWrapMode.Hold;
            while (instPlayableDirector.time < instPlayableDirector.duration && !m_Cancelled)
            {
                yield return null;
                if (!dimEndCalled && (instPlayableDirector.duration - instPlayableDirector.time < m_SpaceTimelineInformation.EndDimDuration))
                {
                    m_Background.CrossFadeAlpha(1, m_SpaceTimelineInformation.EndDimDuration, true);
                    dimEndCalled = true;
                }

                if (Application.isEditor && Input.GetKeyDown(KeyCode.Space)) break;
            }
        }
    }

    IEnumerator WaitForEndOrFrameAndEnd(bool success)
    {
        yield return new WaitForEndOfFrame();

        if (m_Ship != null)
        {
            m_Ship.gameObject.SetActive(true);
            m_Ship.transform.parent = SpaceSceneManager.Instance.ShipActor.transform;
            m_Ship.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_Ship.transform.localScale = Vector3.one;

            m_ShipAnimator.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_ShipAnimator.transform.localScale = m_PrevShipAnimatorLocalScale;

            m_Ship = null;
            m_ShipAnimator = null;
        }
        if (m_Root != null)
        {
            GameObject.Destroy(m_Root);
            m_Root = null;
        }
        
        SetIslandHUDCullingMask(false);
        MainCameraActor.Instance.enabled = m_CameraEnabled;

        this.Success(success);
    }

    //no back button support
    public void OnViewBackButton() { }

    public void OnSkipButton() => StartCoroutine(WaitForEndOrFrameAndEnd(true));

    private void SetIslandHUDCullingMask(bool cullingIslandHUD)
    {
        var cullMask = MainCameraActor.Instance.cullingMask;
        if (cullingIslandHUD) 
        {
            cullMask &= ~LayerMask.GetMask("IslandHUD");
        }
        else
        {
            cullMask |= LayerMask.GetMask("IslandHUD");
        }
        MainCameraActor.Instance.cullingMask = cullMask;
    }
}


public interface ISpaceTimelineHandler
{
    void SetIsland(IslandActor island);
}