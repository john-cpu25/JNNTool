#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.AutoDimColumn.Engine;
using JNNTool.Tools.AutoDimColumn.Models;

namespace JNNTool.Tools.AutoDimColumn.Services
{
    /// <summary>
    /// Trích xuất hình học, Solid, PlanarFace và các cặp mặt song song đối diện của cột.
    /// </summary>
    public static class ColumnGeometryService
    {
        private const double ParallelTol = 0.985; // cos(~10 deg)

        public static ColumnGeometryInfo ExtractGeometry(FamilyInstance column, View activeView)
        {
            var info = new ColumnGeometryInfo
            {
                ColumnId = column.Id,
                Mark = column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty,
                TypeName = column.Symbol?.Name ?? "Column"
            };

            // Điểm tâm từ LocationPoint nếu có
            if (column.Location is LocationPoint lp)
            {
                info.Center = lp.Point;
            }

            var opt = new Options
            {
                ComputeReferences = true,
                View = activeView,
                IncludeNonVisibleObjects = false
            };

            GeometryElement? geoElem = column.get_Geometry(opt);
            if (geoElem == null)
            {
                info.IsSupported = false;
                info.StatusMessage = "No geometry found in view";
                return info;
            }

            // Thu thập các PlanarFace đứng
            var verticalFaces = new List<PlanarFace>();
            foreach (Solid solid in EnumerateSolids(geoElem))
            {
                foreach (Face face in solid.Faces)
                {
                    if (face is PlanarFace pf && pf.Reference != null)
                    {
                        // Mặt đứng: pháp tuyến Z gần bằng 0
                        if (Math.Abs(pf.FaceNormal.Z) < 0.05)
                        {
                            verticalFaces.Add(pf);
                        }
                    }
                }
            }

            if (verticalFaces.Count < 2)
            {
                info.IsSupported = false;
                info.StatusMessage = verticalFaces.Count == 0 ? "Circular / No planar faces" : "Insufficient planar faces";
                return info;
            }

            // Nhóm các mặt theo phương đối đỉnh
            var facePairs = FindOppositeFacePairs(verticalFaces);
            if (facePairs.Count == 0)
            {
                info.IsSupported = false;
                info.StatusMessage = "Cannot find parallel opposite faces";
                return info;
            }

            // Lấy 2 cặp mặt vuông góc với nhau (U và V)
            var pairU = facePairs[0];
            ColumnFacePair? pairV = null;

            for (int i = 1; i < facePairs.Count; i++)
            {
                double dot = Math.Abs(pairU.Normal.DotProduct(facePairs[i].Normal));
                // Vuông góc: dot ≈ 0
                if (dot < 0.2)
                {
                    pairV = facePairs[i];
                    break;
                }
            }

            info.WidthPair = pairU;
            info.WidthMm = pairU.DistanceMm;

            if (pairV != null)
            {
                info.LengthPair = pairV;
                info.LengthMm = pairV.DistanceMm;
            }
            else
            {
                // Chỉ tìm được 1 cặp mặt (ví dụ vách mỏng hoặc 1 phương)
                info.LengthMm = 0;
            }

            // Tính góc xoay của cột theo phương U
            double rotRad = Math.Atan2(pairU.Normal.Y, pairU.Normal.X);
            double rotDeg = rotRad * AutoDimColumnEngine.RadToDeg;
            if (rotDeg < 0) rotDeg += 180.0;
            if (rotDeg >= 180.0) rotDeg -= 180.0;
            info.RotationAngleDeg = Math.Round(rotDeg, 1);

            info.IsSupported = true;
            info.StatusMessage = "Ready";
            return info;
        }

        private static List<ColumnFacePair> FindOppositeFacePairs(List<PlanarFace> faces)
        {
            var pairs = new List<ColumnFacePair>();

            // Duyệt các cặp mặt để tìm các cặp có FaceNormal đối song song (dot ≈ -1)
            for (int i = 0; i < faces.Count; i++)
            {
                var f1 = faces[i];
                XYZ n1 = new XYZ(f1.FaceNormal.X, f1.FaceNormal.Y, 0).Normalize();

                for (int j = i + 1; j < faces.Count; j++)
                {
                    var f2 = faces[j];
                    XYZ n2 = new XYZ(f2.FaceNormal.X, f2.FaceNormal.Y, 0).Normalize();

                    double dot = n1.DotProduct(n2);
                    if (dot <= -ParallelTol) // Ngược hướng (đối song song)
                    {
                        double dist = Math.Abs((f2.Origin - f1.Origin).DotProduct(n1));
                        // Bỏ qua mặt trùng nhau
                        if (dist > 0.05) // > 15mm
                        {
                            pairs.Add(new ColumnFacePair
                            {
                                Face1 = f1,
                                Face2 = f2,
                                Ref1 = f1.Reference,
                                Ref2 = f2.Reference,
                                Normal = n1,
                                DistanceFt = dist
                            });
                        }
                    }
                }
            }

            // Sắp xếp ưu tiên các cặp mặt có diện tích mặt lớn nhất hoặc khoảng cách hợp lý
            return pairs
                .OrderByDescending(p => (p.Face1?.Area ?? 0) + (p.Face2?.Area ?? 0))
                .ToList();
        }

        private static IEnumerable<Solid> EnumerateSolids(GeometryElement geo)
        {
            foreach (GeometryObject obj in geo)
            {
                if (obj is Solid s && s.Faces.Size > 0 && s.Volume > 1e-6)
                {
                    yield return s;
                }
                else if (obj is GeometryInstance gi)
                {
                    GeometryElement? instGeo = gi.GetInstanceGeometry();
                    if (instGeo == null) continue;
                    foreach (Solid inner in EnumerateSolids(instGeo))
                    {
                        yield return inner;
                    }
                }
            }
        }
    }
}
