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
            var existingLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var story in stories)
            {
                var level = existingLevels.FirstOrDefault(l => l.Name.Equals(story.Name, StringComparison.OrdinalIgnoreCase));
                if (level != null)
                {
                    if (Math.Abs(level.Elevation - story.Elevation) > 0.01)
                    {
                        try { level.Elevation = story.Elevation; } catch { }
                    }
                }
                else
                {
                    try
                    {
                        var newLevel = Level.Create(doc, story.Elevation);
                        newLevel.Name = story.Name;
                    }
                    catch { }
                }
            }
        }
    }
}
