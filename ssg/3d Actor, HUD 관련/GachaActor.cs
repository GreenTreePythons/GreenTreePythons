using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using ViewSystem;

public class GachaViewActor : MonoBehaviour,
        IViewCamera,
        // IServiceLocatorComponent
        IServiceLocatorSetupComponent
{
    [SerializeField] Transform m_RootTransform;
    [SerializeField] DirectorAssetReference m_GachaStartDirector;
    [SerializeField] DirectorAssetReference m_LegendaryStoryDirector;
    [SerializeField] AudioClip m_GachaBgm;
    // [Inject] 
    DailyDataPref m_DailyDataPref;

    int m_Depth;
    bool m_SkipRequested;
    Action<bool> m_ShowSkipButton;

    public void SetupServiceLocator(IServiceLocator service)
    {
        m_DailyDataPref = service.Resolve<DailyDataPref>();
    }

    public void SkipCurrent()
    {
        m_SkipRequested = true;
    }

    void Update()
    {
        //force skip command
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SkipCurrent();
        }
    }

    public void ShowSkipButton(bool show) => m_ShowSkipButton?.Invoke(show);

    private IEnumerator ShowDirector(object key, List<(CharacterProto proto, bool isNew, bool isSoulStoneMax)> characters, int index)
    {
        var go = BundleUtility.InstantiateAsync(key, m_RootTransform).Wait();
        var director = go.GetComponent<PlayableDirector>();
        if (director == null) yield break;

        foreach (var cam in go.GetComponentsInChildren<Camera>())
        {
            cam.depth = m_Depth;
        }

        foreach (var timelineHandler in go.GetComponentsInChildren<IGachaTimelineHandler>(true))
        {
            timelineHandler.SetGacha(characters, index);
        }

        m_ShowSkipButton.Invoke(false);
        m_SkipRequested = false;
        while (director.time < director.duration && !m_SkipRequested) yield return null;
        Destroy(director.gameObject);
    }

    public IEnumerator ShowGachaProcess(List<(CharacterProto proto, bool isNew, bool isSoulStoneMax)> characters, Action<bool> showSkipButton)
    {
        var prevClip = SoundManager.Instance.LastAmbientClip;
        SoundManager.Instance.PlayAmbientSound(m_GachaBgm);

        m_ShowSkipButton = showSkipButton;

        if (!m_DailyDataPref.CharacterGachaProcessSkip) yield return ShowDirector(m_GachaStartDirector.RuntimeKey, characters, -1);

        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];

            //non-legendary character skips when it's not new
            if (character.proto.Rarity != CharacterRarity.Legendary && !character.isNew) continue;

            if (character.proto.Rarity == CharacterRarity.Legendary)
            {
                if (m_DailyDataPref.CharacterGachaProcessSkip) continue;
                yield return ShowDirector(m_LegendaryStoryDirector.RuntimeKey, characters, i);
            }

            Debug.Log(character.proto.GachaTimeline);
            yield return ShowDirector(character.proto.GachaTimeline, characters, i);
        }

        SoundManager.Instance.PlayAmbientSound(prevClip);
    }
    public void SetViewCameraDepth(int depth)
    {
        m_Depth = depth;
    }

    public void SetVisibleState(bool visible)
    {
        m_RootTransform.gameObject.SetActive(visible);
    }
}

[Serializable]
public class SpecificCharacterPosition
{
    [SerializeField] string m_CharacterProtoSid;
    [SerializeField] Vector3 m_Pos;

    public string GetCharacterProtoSid() => m_CharacterProtoSid;
    public Vector3 GetCharacterPos() => m_Pos;
}

public interface IGachaTimelineHandler
{
    public void SetGacha(List<(CharacterProto proto, bool isNew, bool isSoulStoneMax)> characters, int index);
}
