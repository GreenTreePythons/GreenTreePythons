using System.Collections;
using UnityEngine;
using ViewSystem;

[RequiredReference]
[ViewLoad("Account/AccountInfoPage")]
public class AccountInfoPage : PageView,
    IServiceLocatorComponent,
    IServiceLocatorSetupComponent,
    IViewBackButtonListener,
    ITopbarAttach,
    ITopbarBackButtonListener
{
    [SerializeField] BundledImage m_IllustImage;
    [SerializeField] UIButton m_BtnEditIllust;
    [SerializeField] UIButton m_BtnAccountEffectModal;

    [Inject] ClientUserData m_UserData;
    [Inject] AccountInfoProfilePanel m_ProfilePanel;
    [Inject] AccountInfoExploreMissionPanel m_MissionPanel;

    void Awake()
    {
        m_BtnEditIllust.AddButtonEvent(OnOpenIllustEditModal);
        m_BtnAccountEffectModal.AddButtonEvent(() => ViewRequest.Open<TotalEffectListModal>(v => v.Open(AccountStatComponent.All)));
    }

    public void Open()
    {
        m_ProfilePanel.Initialize();
        RefreshProfile(AccountInfoEditType.Illust);
    }

    public void RefreshProfile(AccountInfoEditType editType)
    {
        switch (editType)
        {
            case AccountInfoEditType.NickName:
                m_ProfilePanel.RefreshNickName();
                break;
            case AccountInfoEditType.Message:
                m_ProfilePanel.RefreshIntroductionMessage();
                break;
            case AccountInfoEditType.ProfileIcon:
                m_ProfilePanel.RefreshIcon();
                break;
            case AccountInfoEditType.ProfileOutLine:
                m_ProfilePanel.RefreshOutLine();
                break;
            case AccountInfoEditType.Illust:
                var illustProto = ProtoData.Current.AccountProfiles.Get(m_UserData.PlayerData.ProfileInfo.IllustProtoId);
                StartCoroutine(m_IllustImage.CoLoadAsync(illustProto.Image));
                break;
        }
    }

    void OnOpenIllustEditModal()
    {
        StartCoroutine(_CoOpenIllustEditPage());
        IEnumerator _CoOpenIllustEditPage()
        {
            var req = ViewRequest<bool>.Open<AccountInfoIllustEditPage>(v => v.Open());
            yield return req;
            if (!req.Succeeded) yield break;
            RefreshProfile(AccountInfoEditType.Illust);
        }
    }

    public void OnClose()
    {
        StartCoroutine(_CoClose());
        IEnumerator _CoClose()
        {
            if (m_MissionPanel.IsOpened())
            {
                yield return m_MissionPanel.CoActivatePanel(false);
            }
            this.Complete();
        }
    }

    void OnDisable()
    {
        m_MissionPanel.Initialize();
    }

    public void OnViewBackButton() => OnClose();

    public IEnumerator OnCoTopbarBackButton()
    {
        OnClose();
        yield return null;
    }
}