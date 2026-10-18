using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class LevelBuilder
    {
        public static void Build(Document doc, List<StoryData> stories)
        {
            if (stories == null) return;
            var items = stories.Select(s => new LevelMappingItem
            {
                IsSelected = s.IsSelected,
                EtabsStoryName = s.Name,
                TargetLevelName = !string.IsNullOrEmpty(s.TargetName) ? s.TargetName : s.Name,
                ElevationFeet = s.Elevation,
                HeightFeet = s.Height
            }).ToList();
            Build(doc, items);
        }

        public static void Build(Document doc, List<LevelMappingItem> levelItems)
        {
            if (doc == null || levelItems == null || levelItems.Count == 0) return;

            var existingLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var item in levelItems)
            {
                if (!item.IsSelected) continue;

                // 1. Trường hợp người dùng chọn Map vào một Level đã có sẵn trong Revit
                if (!string.IsNullOrWhiteSpace(item.MappedLevelName))
                {
                    var mappedLvl = existingLevels.FirstOrDefault(l => l.Name.Equals(item.MappedLevelName, StringComparison.OrdinalIgnoreCase));
                    if (mappedLvl != null)
                    {
                        if (Math.Abs(mappedLvl.Elevation - item.ElevationFeet) > 0.01)
                        {
                            try { mappedLvl.Elevation = item.ElevationFeet; } catch { }
                        }
                        if (!string.IsNullOrWhiteSpace(item.TargetLevelName) && !mappedLvl.Name.Equals(item.TargetLevelName, StringComparison.OrdinalIgnoreCase))
                        {
                            try { mappedLvl.Name = item.TargetLevelName; } catch { }
                        }
                        continue;
                    }
                }

                // 2. Trường hợp tạo mới hoặc so khớp theo TargetLevelName / EtabsStoryName
                string targetName = !string.IsNullOrWhiteSpace(item.TargetLevelName) ? item.TargetLevelName.Trim() : item.EtabsStoryName;
                var level = existingLevels.FirstOrDefault(l => l.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase)
                                                            || l.Name.Equals(item.EtabsStoryName, StringComparison.OrdinalIgnoreCase));
                if (level != null)
                {
                    if (Math.Abs(level.Elevation - item.ElevationFeet) > 0.01)
                    {
                        try { level.Elevation = item.ElevationFeet; } catch { }
                    }
                    if (!level.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        try { level.Name = targetName; } catch { }
                    }
                }
                else
                {
                    try
                    {
                        var newLevel = Level.Create(doc, item.ElevationFeet);
                        try
                        {
                            newLevel.Name = targetName;
                        }
                        catch
                        {
                            // Nếu tên đã tồn tại trong dự án, thử thêm hậu tố
                            try { newLevel.Name = targetName + " (ETABS)"; } catch { }
                        }
                        existingLevels.Add(newLevel);
                    }
                    catch (Exception ex)
                    {
                        JNNTool.Core.Logging.Logger.Warning($"Lỗi tạo Level {targetName}: {ex.Message}");
                    }
                }
            }
        }
    }
}
