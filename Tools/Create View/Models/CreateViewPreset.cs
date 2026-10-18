using System;
using System.Collections.Generic;

namespace JNNTool.Tools.CreateView.Models
{
    public class ViewRulePresetDto
    {
        public bool IsSelected { get; set; } = true;
        public string ViewNamePattern { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string ScopeBoxName { get; set; } = string.Empty;
    }

    public class TabPresetDto
    {
        public bool IsEnabled { get; set; } = true;
        public string TabId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ViewFamilyTypeName { get; set; } = string.Empty;
        public string BrowserSubDiscipline { get; set; } = string.Empty;
        public string DefaultTemplateName { get; set; } = string.Empty;
        public string DefaultScopeBoxName { get; set; } = string.Empty;
        public string LevelScope { get; set; } = "AllSelectedLevels"; // "SingleLevel" or "AllSelectedLevels"
        public string TargetLevelKeyword { get; set; } = string.Empty; // e.g. "MONG"
        public List<ViewRulePresetDto> Views { get; set; } = new List<ViewRulePresetDto>();
    }

    public class CreateViewPreset
    {
        public string PresetName { get; set; } = "Default";
        public List<TabPresetDto> Tabs { get; set; } = new List<TabPresetDto>();
    }
}
