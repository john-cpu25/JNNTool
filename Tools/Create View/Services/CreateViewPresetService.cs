using System;
using System.Collections.Generic;
using System.IO;
using JNNTool.Tools.CreateView.Models;
using Newtonsoft.Json;

namespace JNNTool.Tools.CreateView.Services
{
    public static class CreateViewPresetService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JNNTool", "Settings"
        );

        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "CreateViewTabPresets.json");

        public static List<CreateViewPreset> LoadPresets()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var presets = JsonConvert.DeserializeObject<List<CreateViewPreset>>(json);
                    if (presets != null && presets.Count > 0)
                        return presets;
                }
            }
            catch { }

            return new List<CreateViewPreset> { GetDefaultAthenaPreset() };
        }

        public static void SavePresets(List<CreateViewPreset> presets)
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                string json = JsonConvert.SerializeObject(presets, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }

        public static CreateViewPreset GetDefaultAthenaPreset()
        {
            return new CreateViewPreset
            {
                PresetName = "ATHENA 5 View Types (Mặc định)",
                Tabs = new List<TabPresetDto>
                {
                    // 1. XR-GRID (Tự lấy 1 view MÓNG để sẵn)
                    new TabPresetDto
                    {
                        TabId = "GRID",
                        Title = "XR-GRID",
                        ViewFamilyTypeName = "XR-GRID",
                        BrowserSubDiscipline = "XR GRID",
                        DefaultTemplateName = "ATHENA_GRID",
                        LevelScope = "SingleLevel",
                        TargetLevelKeyword = "MONG",
                        Views = new List<ViewRulePresetDto>
                        {
                            new ViewRulePresetDto { ViewNamePattern = "XR-GRID", TemplateName = "ATHENA_GRID" }
                        }
                    },
                    // 2. XR-COLUMN (Có đủ các tầng)
                    new TabPresetDto
                    {
                        TabId = "COLUMN",
                        Title = "XR-COLUMN",
                        ViewFamilyTypeName = "XR-COLUMN",
                        BrowserSubDiscipline = "XR-COLUMN",
                        DefaultTemplateName = "ATHENA_COLUMN",
                        LevelScope = "AllSelectedLevels",
                        Views = new List<ViewRulePresetDto>
                        {
                            new ViewRulePresetDto { ViewNamePattern = "XR-COLUMN {Level}", TemplateName = "ATHENA_COLUMN" }
                        }
                    },
                    // 3. XR-FOUNDATION (Tự lấy view MÓNG để sẵn)
                    new TabPresetDto
                    {
                        TabId = "FOUNDATION",
                        Title = "XR-FOUNDATION",
                        ViewFamilyTypeName = "XR-FOUNDATION",
                        BrowserSubDiscipline = "XR-FOUNDATION",
                        DefaultTemplateName = "ATHENA_MẶT BẰNG MÓNG",
                        LevelScope = "SingleLevel",
                        TargetLevelKeyword = "MONG",
                        Views = new List<ViewRulePresetDto>
                        {
                            new ViewRulePresetDto { ViewNamePattern = "XR-PILE", TemplateName = "ATHENA_MẶT BẰNG MÓNG" },
                            new ViewRulePresetDto { ViewNamePattern = "XR-PILECAP", TemplateName = "ATHENA_MẶT BẰNG MÓNG" }
                        }
                    },
                    // 4. XR-MBKC (Có đủ các tầng)
                    new TabPresetDto
                    {
                        TabId = "MBKC",
                        Title = "XR-MBKC",
                        ViewFamilyTypeName = "XR-MBKC",
                        BrowserSubDiscipline = "XR-MBKC",
                        DefaultTemplateName = "ATHENA_MBKC",
                        LevelScope = "AllSelectedLevels",
                        Views = new List<ViewRulePresetDto>
                        {
                            new ViewRulePresetDto { ViewNamePattern = "XR-BE NUOC", TemplateName = "ATHENA_MBKC" },
                            new ViewRulePresetDto { ViewNamePattern = "XR-{Level}", TemplateName = "ATHENA_MBKC" }
                        }
                    },
                    // 5. XR-STAIR PLAN (Tự lấy 1 view MÓNG để sẵn)
                    new TabPresetDto
                    {
                        TabId = "STAIR",
                        Title = "XR-STAIR PLAN",
                        ViewFamilyTypeName = "XR-STAIR PLAN",
                        BrowserSubDiscipline = "XR-STAIR PLAN",
                        DefaultTemplateName = "ATHENA_MBKC",
                        LevelScope = "SingleLevel",
                        TargetLevelKeyword = "MONG",
                        Views = new List<ViewRulePresetDto>
                        {
                            new ViewRulePresetDto { ViewNamePattern = "XR-STAIR {Level}", TemplateName = "ATHENA_MBKC" }
                        }
                    }
                }
            };
        }
    }
}
