using UnityEngine;
using ViewSystem;
using System.Linq;
using System.Collections;
using static UIButton;
using Latecia.Shared;

[RequiredReference]
public class MainRootPageControlPanel : MonoBehaviour,
    IServiceLocatorComponent,
    IServiceLocatorSetupComponent
{
    [SerializeField] Transform m_MainButtonsRoot;
    [SerializeField] MainRootPageControlButton m_MainButtonTemplate;
    [SerializeField] Transform m_SubButtonsRoot;
    [SerializeField] MainRootPageControlButton m_SubButtonTemplate;
    [SerializeField] UIAnimation m_Animation;
    [SerializeField] AnimationClip m_UnFoldAnimClip;
    [SerializeField] AnimationClip m_FoldAnimClip;
    [SerializeField] UIButton m_BgButton;
    [SerializeField] UIButton m_QuitButton;
    [SerializeField] UIButton m_ScenarioBattleButton;
    [SerializeField] UIButton m_ContentsBattleButton;

    [Inject] DeckViewActor m_DeckViewActor;
    [Inject] ClientUserData m_UserData;
    [Inject] IServiceLocator m_ServiceLocator;
    [Inject] MainRootPageFreeMovePanel m_FreeMovePanel;

    public void Awake()
    {
        m_BgButton.AddButtonEvent(() => OnClose());
        m_QuitButton.AddButtonEvent(OnQuit);
        m_ScenarioBattleButton.AddButtonEvent(OnScenarioBattle);
        m_ContentsBattleButton.AddButtonEvent(OnContentsBattle);
        CreateButtons(ContentMenuType.Main, m_MainButtonTemplate, m_MainButtonsRoot);
        CreateButtons(ContentMenuType.Sub, m_SubButtonTemplate, m_SubButtonsRoot);
        m_MainButtonTemplate.gameObject.SetActive(false);
        m_SubButtonTemplate.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }

    public void Open(bool playAnim = true) => PlayAnim(true, playAnim);

    public void OnClose(bool withAnim = true) => PlayAnim(false, withAnim);

    private void OnClose() => this.gameObject.SetActive(false);

    void Update()
    {
        if (m_FreeMovePanel.IsFreeMoving) OnClose();
    }

    void PlayAnim(bool isOpen, bool withAnim = true)
    {
        if (!isOpen && !withAnim)
        {
            OnClose();
            return;
        }

        if (isOpen) this.gameObject.SetActive(true);
        StartCoroutine(_CoPlayAnim());
        IEnumerator _CoPlayAnim()
        {
            m_Animation.AnimationToPlay = isOpen ? m_UnFoldAnimClip : m_FoldAnimClip;
            m_Animation.SampleProgress(0);
            yield return m_Animation.ShowAnimation();
            if (!isOpen) this.gameObject.SetActive(false);
        }
    }

    private void CreateButtons(ContentMenuType menuType, MainRootPageControlButton buttonTemplate, Transform buttonsRoot)
    {
        var buttonProtos = ProtoData.Current.ContentMenus.Where(m => m.ContentMenuType == menuType)
                                                         .OrderBy(m => m.ContentMenuOrder);
        foreach (var proto in buttonProtos)
        {
            if (!ProtoData.Current.Contents.TryGet(proto.ContentGroup, out var contentProto)) continue;
            var button = Instantiate(buttonTemplate, buttonsRoot);
            button.InjectContentInfo(m_ServiceLocator);
            button.SetData(contentProto.Icon, contentProto.NameSid, contentProto.ContentSid, contentProto.ContentDotSid);
            button.AddButtonEvent(GetButtonClickedDelegate(proto.ContentGroup));
        }
    }

    public OnButtonClickedDelegate GetButtonClickedDelegate(string contentSid) => contentSid switch
    {
        "MainInventory" => OnItemInventory,
        "MainCharacter" => OnStarmonInventory,
        "MainAchievement" => OnAchievement,
        "MainTreeOfStar" => OnStarTree,
        "MainGacha" => OnGachaShop,
        "MainArchive" => OnArchive,
        "MainPost" => OnPost,
        "MainRelicAnalysis" => OnRelic,
        "MainCashShop" => OnCashShop,
        "MainStoryReplay" => OnStoryPage,
        "MainShipSkin" => OnShipSkin,
        "MainCollection" => OnCollection,
        "MainGuide" => OnMainGuide,
        "MainMission" => OnMission,
        "MainOption" => OnOption,
        _ => null
    };

    public bool TryGetMainButton(string contentType, out MainRootPageControlButton button)
    {
        button = (MainRootPageControlButton)default;
        var children = m_MainButtonsRoot.GetComponentsInChildren<MainRootPageControlButton>(true);
        foreach (var child in children)
        {
            if (child.ButtonContentType == contentType)
            {
                button = child;
                return true;
            }
        }
        return false;
    }

    public void OnItemInventory()
    {
        OnClose(false);
        ViewRequest.Open<ItemInventoryPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnStarmonInventory()
    {
        OnClose(false);
        ViewRequest.Open<CharacterPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnAchievement()
    {
        OnClose(false);
        ViewRequest.Open<AchievementPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnDialog()
    {
        OnClose(false);
        ViewRequest.Open<StoryPage>(view => view.Open());
    }

    public void OnGachaShop()
    {
        OnClose(false);
        ViewRequest.Open<GachaShopPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnCollection()
    {
        OnClose(false);
        ViewRequest.Open<CollectionSelectPage>(page => page.Open());
    }

    public void OnMission()
    {
        OnClose(false);
        ViewRequest.Open<MissionPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnMainGuide()
    {
        OnClose(false);
        ViewRequest.Open<GuideQuestPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnShipSkin()
    {
        StartCoroutine(_CoEditShip());
        IEnumerator _CoEditShip()
        {
            var reqPage = ViewRequest.Open<ShipSkinPage>(p => p.Open());
            yield return reqPage;
        }

    }

    public void OnRelic()
    {
        OnClose(false);
        ViewRequest.Open<RelicPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnOption()
    {
        OnClose(false);
        ViewRequest.Open<OptionPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnStarTree()
    {
        OnClose(false);
        ViewRequest.Open<TreeOfStarSelectPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnArchive()
    {
        OnClose(false);
        ViewRequest<bool>.Open<ArchivePage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnPost()
    {
        OnClose(false);
        ViewRequest.Open<PostPage>(view => view.Open(PostType.Common)).WithFadein().Forget();
    }

    public void OnCashShop()
    {
        OnClose(false);
        ViewRequest.Open<CashShopPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnStoryPage()
    {
        OnClose(false);
        ViewRequest.Open<StoryPage>(view => view.Open()).WithFadein().Forget();
    }

    public void OnScenarioBattle()
    {
        OnClose(false);
        var stageProto = m_UserData.StageDatas.GetCurrentStageProto(BattleContentsType.StageBattle);
        if (stageProto != null)
        {
            ViewRequest.Open<BattleScenarioPage>(view => view.Open(stageProto.Chapter, stageProto.Id, BattleContentsType.StageBattle), true).WithFadein().Forget();
        }
        else
        {
            ViewRequest.Open<BattleScenarioPage>(view => view.Open(1, 0, BattleContentsType.StageBattle), true).WithFadein().Forget();
        }
    }

    public void OnContentsBattle()
    {
        OnClose(false);
        ViewRequest.Open<BattleSelectPage>(view => view.Open()).WithFadein().Forget();
    }

    void OnQuit()
    {
        StartCoroutine(_CoQuitGame());
        IEnumerator _CoQuitGame()
        {
            var req = ViewRequest<bool>.Open<GeneralNoticeModal>(view => view.Open(
                Strings.QUIT,
                Strings.REQ_QUIT));
            yield return req;
            if (req.TryGetResult(out var result) && result) GameInstance.Quit();
        }
    }
}
