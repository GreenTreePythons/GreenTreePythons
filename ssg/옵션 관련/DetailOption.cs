using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequiredReference]
public class OptionPageDetailOption : MonoBehaviour,
    IServiceLocatorSetupComponent
{
    [SerializeField] DetailOptionType m_DetailOptionType;
    [SerializeField] List<MonoBehaviour> m_Options;
    [SerializeField] Transform m_DetailOptionRoot;

    public DetailOptionType DetailOptionType => m_DetailOptionType;
    [Inject] OptionPref m_OptionPref;
    [Inject] ClientUserData m_ClientUserData;
    [Inject] OptionPage m_OptionPage;

    const float HIGH_GRAPHIC_QUALITY = 1.0f;
    const float MIDDLE_GRAPHIC_QUALITY = 0.33f;
    const float LOW_GRAPHIC_QUALITY = 0.1f;
    const int LOW_FRAME = 30;
    const int HIGH_FRAME = 60;

    public void Initialize()
    {
        if(m_DetailOptionType == DetailOptionType.GraphicQuality)
        {
            var currentQuality = m_OptionPref.RenderQuality;
            SharedDebug.Log($"currentQuality : {currentQuality}");
            foreach(var option in m_Options)
            {
                option.GetComponent<OptionPageToggleOption>().Initialize();
            }
            var toggleComponent0 = m_Options[0].GetComponent<Toggle>();
            toggleComponent0.isOn = currentQuality == HIGH_GRAPHIC_QUALITY;
            toggleComponent0.onValueChanged.AddListener(t => GraphicQualityControl(HIGH_GRAPHIC_QUALITY));

            var toggleComponent1 = m_Options[1].GetComponent<Toggle>();
            toggleComponent1.isOn = currentQuality == MIDDLE_GRAPHIC_QUALITY;
            toggleComponent1.onValueChanged.AddListener(t => GraphicQualityControl(MIDDLE_GRAPHIC_QUALITY));

            var toggleComponent2 = m_Options[2].GetComponent<Toggle>();
            toggleComponent2.isOn = currentQuality == LOW_GRAPHIC_QUALITY;
            toggleComponent2.onValueChanged.AddListener(t => GraphicQualityControl(LOW_GRAPHIC_QUALITY));
        }
        else if(m_DetailOptionType == DetailOptionType.GraphicFrame)
        {
            var currentFrame = m_OptionPref.GraphicFrameRate;
            var toggleComponent0 = m_Options[0].GetComponent<Toggle>();
            toggleComponent0.isOn = currentFrame == LOW_FRAME;
            toggleComponent0.onValueChanged.AddListener(t => FrameControl(LOW_FRAME));

            var toggleComponent1 = m_Options[1].GetComponent<Toggle>();
            toggleComponent1.isOn = currentFrame == HIGH_FRAME;
            toggleComponent1.onValueChanged.AddListener(t => FrameControl(HIGH_FRAME));
        }
        else if(m_DetailOptionType == DetailOptionType.SoundTotal)
        {
            var totalSound = m_OptionPref.TotalSoundValue;
            var sliderOption = m_Options[0].GetComponentInChildren<OptionPageSliderOption>();
            sliderOption.SetValue(totalSound.isOn, totalSound.soundValue);
            sliderOption.GetSlider().onValueChanged.AddListener(s => TotalSoundControl(sliderOption));
            if(sliderOption.GetMuteButton() != null)
            {
                sliderOption.GetMuteButton().AddButtonEvent(() => TotalSoundMute(sliderOption));
            }
        }
        else if(m_DetailOptionType == DetailOptionType.SoundBackGround)
        {
            var bgSound = m_OptionPref.BGSoundValue;
            var sliderOption = m_Options[0].GetComponentInChildren<OptionPageSliderOption>();
            sliderOption.SetValue(bgSound.isOn, bgSound.soundValue);
            sliderOption.GetSlider().onValueChanged.AddListener(s => BGSoundControl(sliderOption));
            if(sliderOption.GetMuteButton() != null)
            {
                sliderOption.GetMuteButton().AddButtonEvent(() => BGSoundMute(sliderOption));
            }
        }
        else if(m_DetailOptionType == DetailOptionType.SoundEffect)
        {
            var effectSound = m_OptionPref.EffectSoundValue;
            var sliderOption = m_Options[0].GetComponentInChildren<OptionPageSliderOption>();
            sliderOption.SetValue(effectSound.isOn, effectSound.soundValue);
            sliderOption.GetSlider().onValueChanged.AddListener(s => EffectSoundControl(sliderOption));
            if(sliderOption.GetMuteButton() != null)
            {
                sliderOption.GetMuteButton().AddButtonEvent(() => EffectSoundMute(sliderOption));
            }
        }
        else if(m_DetailOptionType == DetailOptionType.LanguageText)
        {
            var languageText = m_OptionPref.Language;
            foreach (var detailOption in m_DetailOptionRoot.GetComponentsInChildren<OptionPageToggleOption>())
            {
                detailOption.Initialize();
                var currentLanguage = m_OptionPref.Language;
                var toggleComponent = detailOption.GetComponent<Toggle>();
                var languageId = (int)detailOption.GetValue();
                toggleComponent.isOn = languageId == (int)currentLanguage;
                if(detailOption.GetValue() == 23 || detailOption.GetValue() == 10)
                {
                    toggleComponent.onValueChanged.AddListener(t => LanguageTextControl(languageId));
                }
                else
                {
                    toggleComponent.interactable = false;
                }
#if UNITY_EDITOR
                if (detailOption.GetValue() == 43)
                {
                    toggleComponent.onValueChanged.AddListener(t => LanguageTextControl(languageId));
                    toggleComponent.interactable = true;
                }
#endif
            }
        }
        else if(m_DetailOptionType == DetailOptionType.GameDirectionContent)
        {
            var currentValue = m_OptionPref.PlayContentTimeline;
            var optionToggle = m_Options[0].GetComponent<OptionPageToggleOption>();
            optionToggle.SetValue(currentValue);
            var toggleComponent = m_Options[0].GetComponent<Toggle>();
            toggleComponent.isOn = currentValue;
            toggleComponent.onValueChanged.AddListener(t => ContentTimelineControl(optionToggle, toggleComponent.isOn));
        }
        else if(m_DetailOptionType == DetailOptionType.GameDirectionDriveSkill)
        {
            var currentValue = m_OptionPref.PlayDriveTimeline;
            var optionToggle = m_Options[0].GetComponent<OptionPageToggleOption>();
            optionToggle.SetValue(currentValue);
            var toggleComponent = m_Options[0].GetComponent<Toggle>();
            toggleComponent.isOn = currentValue;
            toggleComponent.onValueChanged.AddListener(t => DriveSkillTimelineControl(optionToggle, toggleComponent.isOn));
        }
        else if(m_DetailOptionType == DetailOptionType.GameDirectionHUD)
        {
            var currentValue = m_OptionPref.PlayHUD;
            var optionToggle = m_Options[0].GetComponent<OptionPageToggleOption>();
            optionToggle.SetValue(currentValue);
            var toggleComponent = m_Options[0].GetComponent<Toggle>();
            toggleComponent.isOn = currentValue;
            toggleComponent.onValueChanged.AddListener(t => HUDControl(optionToggle, toggleComponent.isOn));
        }
        else if(m_DetailOptionType == DetailOptionType.AccountUID)
        {
            m_Options[0].GetComponent<TextMeshProUGUI>().text = $"UID : {m_ClientUserData.Id}";
        }
        else if(m_DetailOptionType == DetailOptionType.Dialog3DModel)
        {
            var currentValue = m_OptionPref.Is3dModeInDialog;
            var optionToggle = m_Options[0].GetComponent<OptionPageToggleOption>();
            optionToggle.SetValue(currentValue);
            var toggleComponent = m_Options[0].GetComponent<Toggle>();
            toggleComponent.isOn = currentValue;
            toggleComponent.onValueChanged.AddListener(t => Dialog3DModelContorol(optionToggle, toggleComponent.isOn));
        }
    }

#if UNITY_EDITOR
    public void Update()
    {
        if (Application.isPlaying) return;
        
    }
#endif

    public void GraphicQualityControl(float quality)
    {
        if (quality >= HIGH_GRAPHIC_QUALITY) m_OptionPref.RenderQuality = HIGH_GRAPHIC_QUALITY;
        else if (quality >= MIDDLE_GRAPHIC_QUALITY) m_OptionPref.RenderQuality = MIDDLE_GRAPHIC_QUALITY;
        else m_OptionPref.RenderQuality = LOW_GRAPHIC_QUALITY;
        m_OptionPref.Save();
        SharedDebug.Log($"GraphicQualityControl : {m_OptionPref.RenderQuality}");
    }

    public void FrameControl(int frame)
    {
        if(frame > HIGH_FRAME || frame > LOW_FRAME) 
        {
            m_OptionPref.GraphicFrameRate = HIGH_FRAME;
        }
        else
        {
            m_OptionPref.GraphicFrameRate = LOW_FRAME;
        }
        m_OptionPref.Save();
        SharedDebug.Log($"FrameControl : {m_OptionPref.GraphicFrameRate}");
    }

    public void TotalSoundControl(OptionPageSliderOption slider)
    {
        slider.Refresh();
        m_OptionPref.TotalSoundValue = slider.GetValue();
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.TotalSoundValue.isOn ? m_OptionPref.TotalSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Ambient, m_OptionPref.BGSoundValue.soundValue * soundVolumn);
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Effect, m_OptionPref.EffectSoundValue.soundValue * soundVolumn);
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Battle, m_OptionPref.EffectSoundValue.soundValue * soundVolumn);
        SharedDebug.Log($"TotalSoundControl : {m_OptionPref.TotalSoundValue}");
    }

    public void EffectSoundControl(OptionPageSliderOption slider)
    {
        slider.Refresh();
        m_OptionPref.EffectSoundValue = slider.GetValue();
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.EffectSoundValue.isOn ? m_OptionPref.EffectSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Effect, soundVolumn);
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Battle, soundVolumn);
        SharedDebug.Log($"EffectSoundControl : {m_OptionPref.EffectSoundValue}");
    }

    public void BGSoundControl(OptionPageSliderOption slider)
    {   
        slider.Refresh();
        m_OptionPref.BGSoundValue = slider.GetValue();
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.BGSoundValue.isOn ? m_OptionPref.BGSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Ambient, soundVolumn);
        SharedDebug.Log($"BGSoundControl : {m_OptionPref.BGSoundValue}");
    }

    public void TotalSoundMute(OptionPageSliderOption slider)
    {
        slider.OnMute();
        m_OptionPref.TotalSoundValue.isOn = slider.GetValue().isSoundOn;
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.TotalSoundValue.isOn ? m_OptionPref.TotalSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Ambient, m_OptionPref.BGSoundValue.soundValue * soundVolumn);
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Battle, m_OptionPref.EffectSoundValue.soundValue * soundVolumn);
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Effect, m_OptionPref.EffectSoundValue.soundValue * soundVolumn);
        SharedDebug.Log($"TotalSoundMute : {m_OptionPref.TotalSoundValue.isOn}");
    }

    public void EffectSoundMute(OptionPageSliderOption slider)
    {
        slider.OnMute();
        m_OptionPref.EffectSoundValue.isOn = slider.GetValue().isSoundOn;
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.EffectSoundValue.isOn ? m_OptionPref.EffectSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Effect, soundVolumn);
        SharedDebug.Log($"EffectSoundMute : {m_OptionPref.EffectSoundValue.isOn}");
    }

    public void BGSoundMute(OptionPageSliderOption slider)
    {
        slider.OnMute();
        m_OptionPref.BGSoundValue.isOn = slider.GetValue().isSoundOn;
        m_OptionPref.Save();
        var soundVolumn = m_OptionPref.BGSoundValue.isOn ? m_OptionPref.BGSoundValue.soundValue : 0.0f;
        SoundManager.Instance.ChangeVolumn(SoundManager.VolumeGroup.Ambient, soundVolumn);
        SharedDebug.Log($"BGSoundMute : {m_OptionPref.BGSoundValue.isOn}");
    }

    public void LanguageTextControl(int languageId)
    {
        var systemLanguage = (SystemLanguage)languageId;
        StringTable.SetLanguage(systemLanguage);
        m_OptionPref.Language = systemLanguage;
        m_OptionPref.Save();
        SharedDebug.Log($"LanguageTextControl : {m_OptionPref.Language}");
    }

    public void ContentTimelineControl(OptionPageToggleOption toggle, bool isOn)
    {
        toggle.SetValue(isOn);
        m_OptionPref.PlayContentTimeline = isOn;
        m_OptionPref.Save();
        SharedDebug.Log($"ContentTimelineControl : {m_OptionPref.PlayContentTimeline}");
    }

    public void DriveSkillTimelineControl(OptionPageToggleOption toggle, bool isOn)
    {
        toggle.SetValue(isOn);
        m_OptionPref.PlayDriveTimeline = isOn;
        m_OptionPref.Save();
        SharedDebug.Log($"DriveSkillTimelineControl : {m_OptionPref.PlayDriveTimeline}");
    }

    public void HUDControl(OptionPageToggleOption toggle, bool isOn)
    {
        toggle.SetValue(isOn);
        m_OptionPref.PlayHUD = isOn;
        m_OptionPref.Save();
        SharedDebug.Log($"HUDControl : {m_OptionPref.PlayHUD}");
    }

    public void Dialog3DModelContorol(OptionPageToggleOption toggle, bool isOn)
    {
        toggle.SetValue(isOn);
        m_OptionPref.Is3dModeInDialog = isOn;
        m_OptionPref.Save();
        SharedDebug.Log($"Dialog3DModelContorol : {m_OptionPref.Is3dModeInDialog}");
    }
}