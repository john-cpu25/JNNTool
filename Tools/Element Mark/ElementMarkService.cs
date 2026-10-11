#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.ElementMark.Builders;
using JNNTool.Tools.ElementMark.Collectors;
using JNNTool.Tools.ElementMark.Engine;
using JNNTool.Tools.ElementMark.Models;

namespace JNNTool.Tools.ElementMark
{
    internal class MarkRunResult
    {
        public CollectedData Data { get; set; } = new CollectedData();
        public List<MarkAssignment> Assignments { get; } = new List<MarkAssignment>();
        public WriteReport? Report { get; set; }
        public long ElapsedMs { get; set; }
    }

    /// <summary>Ghép Collector → Engine → Writer. Chỉ gọi trong Revit API context (ExternalEvent).</summary>
    internal static class ElementMarkService
    {
        public static MarkRunResult Run(Document doc, View activeView, ElementMarkSettings s,
            IDictionary<string, string> levelCodes, bool write)
        {
            var sw = Stopwatch.StartNew();
            var result = new MarkRunResult
            {
                Data = ElementMarkCollector.Collect(doc, activeView, s)
            };

            if (s.BeamEnabled) result.Assignments.AddRange(ElementMarkEngine.MarkBeams(result.Data.Beams, s, levelCodes));
            if (s.ColumnEnabled) result.Assignments.AddRange(ElementMarkEngine.MarkColumns(result.Data.Columns, s, levelCodes));
            if (s.FloorEnabled) result.Assignments.AddRange(ElementMarkEngine.MarkFloors(result.Data.Floors, s, levelCodes));

            if (write && result.Assignments.Count > 0)
                result.Report = ElementMarkWriter.Write(doc, result.Assignments, s);

            result.ElapsedMs = sw.ElapsedMilliseconds;
            return result;
        }

        public static List<LevelInfo> ReadLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new LevelInfo(l.Name, UnitUtils.ConvertFromInternalUnits(l.Elevation, UnitTypeId.Millimeters)))
                .ToList();
        }
    }
}
