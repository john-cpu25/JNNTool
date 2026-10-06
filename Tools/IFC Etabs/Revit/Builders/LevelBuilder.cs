using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo và ánh xạ tầng (Level) Revit từ ETABS Level theo tiêu chí cao độ (Elevation).
    /// </summary>
    public class LevelBuilder
    {
        private readonly Document _doc;
        private readonly Dictionary<string, Level> _levelsByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(Level Level, double ElevMm)> _levelsByElevation = new();

        public LevelBuilder(Document doc)
        {
            _doc = doc;
            RefreshLevelCache();
        }

        public void RefreshLevelCache()
        {
            _levelsByName.Clear();
            _levelsByElevation.Clear();

            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>();

            foreach (var lvl in collector)
            {
                double elevMm = UnitConverter.FeetToMm(lvl.Elevation);
                _levelsByName[lvl.Name] = lvl;
                _levelsByElevation.Add((lvl, elevMm));
            }

            _levelsByElevation.Sort((a, b) => a.ElevMm.CompareTo(b.ElevMm));
        }

        /// <summary>
        /// Lấy tầng kế tiếp cao hơn tầng hiện tại.
        /// </summary>
        public Level? GetNextLevelAbove(Level currentLevel)
        {
            if (currentLevel == null) return null;
            double currentElevMm = UnitConverter.FeetToMm(currentLevel.Elevation);
            return _levelsByElevation.FirstOrDefault(x => x.ElevMm > currentElevMm + 10.0).Level;
        }

        /// <summary>
        /// Tìm Level Revit tương ứng theo cao độ hoặc tạo mới nếu chưa có.
        /// </summary>
        public Level GetOrCreateLevel(ETABSLevel etabsLevel, double toleranceMm = 2.0)
        {
            // 1. Tìm Level có sẵn với cao độ sai lệch trong phạm vi tolerance
            var match = _levelsByElevation.FirstOrDefault(x => Math.Abs(x.ElevMm - etabsLevel.ElevationMm) <= toleranceMm);
            if (match.Level != null)
            {
                AttachParameters(match.Level, etabsLevel);
                return match.Level;
            }

            // 2. Tìm theo tên nếu cao độ chưa khớp
            if (_levelsByName.TryGetValue(etabsLevel.Name, out var namedLevel))
            {
                AttachParameters(namedLevel, etabsLevel);
                return namedLevel;
            }

            // 3. Tạo Level mới bằng Revit API
            double elevationFeet = UnitConverter.MmToFeet(etabsLevel.ElevationMm);
            Level newLevel = Level.Create(_doc, elevationFeet);

            // Đặt tên an toàn tránh trùng lặp
            string desiredName = etabsLevel.Name;
            string finalName = desiredName;
            int counter = 1;
            while (_levelsByName.ContainsKey(finalName))
            {
                finalName = $"{desiredName}_{counter++}";
            }

            try
            {
                newLevel.Name = finalName;
            }
            catch { }

            _levelsByName[finalName] = newLevel;
            _levelsByElevation.Add((newLevel, etabsLevel.ElevationMm));
            _levelsByElevation.Sort((a, b) => a.ElevMm.CompareTo(b.ElevMm));

            AttachParameters(newLevel, etabsLevel);
            return newLevel;
        }

        private static void AttachParameters(Level level, ETABSLevel etabsLevel)
        {
            SharedParameterManager.SetParameterValue(level, SharedParameterManager.ParamSource, etabsLevel.Source);
            SharedParameterManager.SetParameterValue(level, SharedParameterManager.ParamEtabsId, etabsLevel.SourceId);
            SharedParameterManager.SetParameterValue(level, SharedParameterManager.ParamSourceHash, etabsLevel.Hash);
            SharedParameterManager.SetParameterValue(level, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
