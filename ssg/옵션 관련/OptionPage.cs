using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Latecia.Shared;
using UnityEngine;
using ViewSystem;

[ViewLoad("Common/OptionPage")]
[RequiredReference]
public class OptionPage : PageView,
    IViewBackButtonListener,
    IServiceLocatorComponent,
    IServiceLocatorSetupComponent,
    ITopbarBackButtonListener,
    ITopbarAttach
{
    [SerializeField] OptionPageTabPanel m_TabPanel;
    [SerializeField] Transform m_DetailPanelRoot;

    OptionType m_CurrentOptionType;
    List<OptionPageDetailOptionPanel> m_Panels = new();
    IServiceLocator m_ServiceLocator;

    [Inject] OptionPref m_OptionPref;

    public void Open()
    {
        m_TabPanel.Initialize();
        InitializePanels();
        ChangePanel(OptionType.Graphic);
    }

    void InitializePanels()
    {
        if (m_Panels.Count > 0) return;
        foreach (var panel in m_DetailPanelRoot.GetComponentsInChildren<OptionPageDetailOptionPanel>(true))
        {
            if (m_Panels.Contains(panel))
            {
                SharedDebug.Log($"same panel type already exist, skip it ({panel.name},{panel.DetailPanelOptionType})");
                continue;
            }
            m_ServiceLocator.Inject(panel.GetComponent<IServiceLocatorSetupComponent>());
            panel.Initialize();
            m_Panels.Add(panel);
        }
    }

    public List<OptionPageDetailOptionPanel> GetDetailOptionPanels() => m_Panels;

    public void ChangePanel(OptionType optionType)
    {
        var openedPanels = m_Panels.Where(p => p.gameObject.activeSelf).ToList();
        if (!openedPanels.Any())
        {
            OpenPanel(optionType);
            return;
        }
        if (openedPanels.Count > 1)
        {
            openedPanels.ForEach(panel => panel.OnClose());
            OpenPanel(optionType);
            return;
        }
        if (m_CurrentOptionType == optionType) return;

        m_Panels.Single(p => p.DetailPanelOptionType == m_CurrentOptionType).OnClose();
        OpenPanel(optionType);
    }

    void OpenPanel(OptionType optionType)
    {
        m_Panels.Single(p => p.DetailPanelOptionType == optionType).Open();
        m_CurrentOptionType = optionType;
    }

    void OnClose()
    {
        m_OptionPref.Save();
        this.Complete();
    }

    public IEnumerator OnCoTopbarBackButton()
    {
        OnClose();
        yield return null;
    }

    public void OnViewBackButton()
    {
        OnClose();
    }

    public void SetupServiceLocator(IServiceLocator service)
    {
        m_ServiceLocator = service;
    }
}