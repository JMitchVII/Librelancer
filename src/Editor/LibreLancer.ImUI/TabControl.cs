using System;
using System.Collections.Generic;
using ImGuiNET;

namespace LibreLancer.ImUI;

public class TabControl
{
    public List<DockTab> Tabs { get; private set; } = new List<DockTab>();
    public DockTab Selected { get; private set; } = null;

    private DockTab _setSelected;
    
    public void SetSelected(DockTab tab)
    {
        _setSelected = tab;
    }

    public void CloseAll()
    {
        foreach(var t in Tabs)
            t.Dispose();
        Tabs.Clear();
        Selected = null;
    }

    public void CloseTab(DockTab t)
    {
        if (!Tabs.Contains(t)) throw new InvalidOperationException();
        t.Dispose();
        if (Selected == t) Selected = null;
        Tabs.Remove(t);
    }

    public void TabLabels()
    {
        if (Tabs.Count > 0)
        {
            var flags = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyScroll |
                        ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.TabListPopupButton;
            ImGui.BeginTabBar("##tabbar", flags);
            for (int i = 0; i < Tabs.Count; i++)
            {
                bool isTabOpen = true;
                bool selectedThis = false;
                var tabFlags = Tabs[i] == _setSelected ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                if (Tabs[i].UnsavedDocument) tabFlags |= ImGuiTabItemFlags.UnsavedDocument;
                if (ImGui.BeginTabItem(Tabs[i].RenderTitle, ref isTabOpen,tabFlags))
                {
                    selectedThis = true;
                    ImGui.EndTabItem();
                }
                if (!isTabOpen)
                {
                    if(Selected == Tabs[i]) Selected = null;
                    Tabs[i].Dispose();
                    Tabs.RemoveAt(i);
                }
                else if (selectedThis)
                    Selected = Tabs[i];
            }
            ImGui.EndTabBar();
        }

        _setSelected = null;
    }
}