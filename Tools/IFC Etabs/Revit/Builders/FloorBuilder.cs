using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Core.Geometry;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo cấu kiện sàn kết cấu native trong Revit (Floor) từ ETABSSlab kèm xử lý lỗ mở (Openings).
    /// </summary>
    public class FloorBuilder
    {
        private readonly Document _doc;
        private FloorType? _defaultFloorType;

        public FloorBuilder(Document doc)
        {
            _doc = doc;
            ResolveFloorType();
        }

        private void ResolveFloorType()
        {
            var floorTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();

            _defaultFloorType = floorTypes.FirstOrDefault(ft => ft.Name.Contains("150") || ft.Name.Contains("Concrete"))
                                ?? floorTypes.FirstOrDefault();
        }

        public Floor? CreateFloor(ETABSSlab slab, Level level)
        {
            if (slab == null || level == null || slab.OuterBoundary.Count < 3) return null;

            if (_defaultFloorType == null)
            {
                ResolveFloorType();
                if (_defaultFloorType == null) return null;
            }

            try
            {
                var profile = new List<CurveLoop>();

                // 1. Tạo CurveLoop cho đường bao ngoài sàn
                CurveLoop? outerLoop = CreateCurveLoopFromPoints(slab.OuterBoundary, level.Elevation);
                if (outerLoop == null) return null;
                profile.Add(outerLoop);

                // 2. Tạo CurveLoop cho các lỗ mở (Openings)
                foreach (var opening in slab.Openings)
                {
                    if (opening.Count >= 3)
                    {
                        CurveLoop? opLoop = CreateCurveLoopFromPoints(opening, level.Elevation);
                        if (opLoop != null)
                        {
                            profile.Add(opLoop);
                        }
                    }
                }

                // 3. Tạo Native Floor bằng Revit API
                Floor newFloor = Floor.Create(_doc, profile, _defaultFloorType.Id, level.Id);
                if (newFloor == null) return null;

                // 4. Gán tham số JNN
                AttachParameters(newFloor, slab);

                return newFloor;
            }
            catch
            {
                return null;
            }
        }

        private static CurveLoop? CreateCurveLoopFromPoints(List<Point3D> points, double targetElevFeet)
        {
            if (points == null || points.Count < 3) return null;

            var loop = new CurveLoop();
            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                var pt1 = points[i];
                var pt2 = points[(i + 1) % count];

                var p1 = new XYZ(UnitConverter.MmToFeet(pt1.X), UnitConverter.MmToFeet(pt1.Y), targetElevFeet);
                var p2 = new XYZ(UnitConverter.MmToFeet(pt2.X), UnitConverter.MmToFeet(pt2.Y), targetElevFeet);

                if (p1.DistanceTo(p2) > 0.05) // Bỏ qua đoạn quá nhỏ
                {
                    loop.Append(Line.CreateBound(p1, p2));
                }
            }

            return loop.IsOpen() ? null : loop;
        }

        private static void AttachParameters(Floor floorInstance, ETABSSlab slab)
        {
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamSource, slab.Source);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamEtabsId, slab.SourceId);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamSection, slab.Section);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamMaterial, slab.Material);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamStory, slab.Story);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamSourceHash, slab.Hash);
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamConversionStatus, slab.Status.ToString());
            SharedParameterManager.SetParameterValue(floorInstance, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
