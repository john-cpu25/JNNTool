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

            // 1. Kiểm tra BoundingBox của cột
            var bbox = column.get_BoundingBox(activeView) ?? column.get_BoundingBox(null);
            if (bbox != null)
            {
                info.BoundingBox = bbox;
                info.Center = (bbox.Min + bbox.Max) * 0.5;
            }
            else if (column.Location is LocationPoint lpCenter)
            {
                info.Center = lpCenter.Point;
            }

            double bbWidthFt = bbox != null ? Math.Abs(bbox.Max.X - bbox.Min.X) : 0.0;
            double bbLengthFt = bbox != null ? Math.Abs(bbox.Max.Y - bbox.Min.Y) : 0.0;

            // 2. Góc xoay từ LocationPoint nếu có
            if (column.Location is LocationPoint lp)
            {
                double deg = lp.Rotation * AutoDimColumnEngine.RadToDeg;
                if (deg < 0) deg += 180.0;
                if (deg >= 180.0) deg -= 180.0;
                info.RotationAngleDeg = Math.Round(deg, 1);
            }

            // 3. Thử lấy Reference Planes chuẩn của FamilyInstance (Left, Right, Front, Back)
            try
            {
                var leftRefs = column.GetReferences(FamilyInstanceReferenceType.Left);
                var rightRefs = column.GetReferences(FamilyInstanceReferenceType.Right);
                var frontRefs = column.GetReferences(FamilyInstanceReferenceType.Front);
                var backRefs = column.GetReferences(FamilyInstanceReferenceType.Back);

                bool hasWidth = leftRefs != null && leftRefs.Count > 0 && rightRefs != null && rightRefs.Count > 0;
                bool hasLength = frontRefs != null && frontRefs.Count > 0 && backRefs != null && backRefs.Count > 0;

                if (hasWidth && hasLength)
                {
                    info.WidthPair = new ColumnFacePair
                    {
                        Ref1 = leftRefs![0],
                        Ref2 = rightRefs![0],
                        Normal = XYZ.BasisX,
                        DistanceFt = bbWidthFt > 0 ? bbWidthFt : 1.0
                    };
                    info.WidthMm = info.WidthPair.DistanceMm;

                    info.LengthPair = new ColumnFacePair
                    {
                        Ref1 = frontRefs![0],
                        Ref2 = backRefs![0],
                        Normal = XYZ.BasisY,
                        DistanceFt = bbLengthFt > 0 ? bbLengthFt : 1.0
                    };
                    info.LengthMm = info.LengthPair.DistanceMm;

                    info.IsSupported = true;
                    info.StatusMessage = "Ready";
                    return info;
                }
            }
            catch
            {
                // Fall back to Solid geometry
            }

            // 4. Fallback: Lấy Geometry với Options phù hợp
            var opt = new Options
            {
                ComputeReferences = true,
                View = activeView,
                IncludeNonVisibleObjects = false
            };

            GeometryElement? geoElem = column.get_Geometry(opt);

            // Nếu không tìm thấy Solid qua View hiện hành (ví dụ Family dùng Symbolic line trên mặt bằng),
            // fallback sang lấy hình học 3D chi tiết (Fine Detail)
            var solids = geoElem != null ? EnumerateSolids(geoElem).ToList() : new List<Solid>();
            if (solids.Count == 0)
            {
                var fallbackOpt = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine,
                    IncludeNonVisibleObjects = false
                };
                geoElem = column.get_Geometry(fallbackOpt);
                if (geoElem != null)
                {
                    solids = EnumerateSolids(geoElem).ToList();
                }
            }

            if (solids.Count == 0)
            {
                info.IsSupported = false;
                info.StatusMessage = "No solids found in column family";
                return info;
            }

            // 3. Thu thập các PlanarFace đứng
            var verticalFaces = new List<PlanarFace>();
            foreach (Solid solid in solids)
            {
                foreach (Face face in solid.Faces)
                {
                    if (face is PlanarFace pf && pf.Reference != null)
                    {
                        // Mặt đứng: pháp tuyến Z gần bằng 0
                        if (Math.Abs(pf.FaceNormal.Z) < 0.1)
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

            // 4. Nhóm các mặt theo phương đối đỉnh
            var facePairs = FindOppositeFacePairs(verticalFaces);
            if (facePairs.Count == 0)
            {
                info.IsSupported = false;
                info.StatusMessage = "Cannot find parallel opposite faces";
                return info;
            }

            // 5. Lấy 2 cặp mặt vuông góc với nhau (U và V)
            var pairU = facePairs[0];
            ColumnFacePair? pairV = null;

            for (int i = 1; i < facePairs.Count; i++)
            {
                double dot = Math.Abs(pairU.Normal.DotProduct(facePairs[i].Normal));
                // Vuông góc: dot ≈ 0
                if (dot < 0.25)
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
                info.LengthMm = 0;
            }

            // 6. Tính tâm nếu LocationPoint chưa có hoặc là Zero
            if (info.Center == XYZ.Zero)
            {
                if (bbox != null)
                {
                    info.Center = (bbox.Min + bbox.Max) * 0.5;
                }
                else if (pairU.Face1 != null && pairU.Face2 != null)
                {
                    info.Center = (pairU.Face1.Origin + pairU.Face2.Origin) * 0.5;
                }
            }

            // 7. Tính góc xoay của cột theo phương U
            double rotRad = Math.Atan2(pairU.Normal.Y, pairU.Normal.X);
            double rotDeg = rotRad * AutoDimColumnEngine.RadToDeg;
            if (rotDeg < 0) rotDeg += 180.0;
            if (rotDeg >= 180.0) rotDeg -= 180.0;
            info.RotationAngleDeg = Math.Round(rotDeg, 1);

            info.IsSupported = true;
            info.StatusMessage = "Ready";
            return info;
        }

        /// <summary>
        /// Trích xuất hình học, BoundingBox và các cặp mặt bên / mặt đầu của Vách (Wall).
        /// </summary>
        public static ColumnGeometryInfo ExtractWallGeometry(Wall wall, View activeView)
        {
            var info = new ColumnGeometryInfo
            {
                ColumnId = wall.Id,
                Mark = wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty,
                TypeName = wall.WallType?.Name ?? "Wall"
            };

            // 1. Kiểm tra BoundingBox của vách trên view
            var bbox = wall.get_BoundingBox(activeView) ?? wall.get_BoundingBox(null);
            if (bbox != null)
            {
                info.BoundingBox = bbox;
                info.Center = (bbox.Min + bbox.Max) * 0.5;
            }

            // 2. Chiều dài và hướng của vách từ LocationCurve
            XYZ wallDir = XYZ.BasisX;
            XYZ wallNormal = XYZ.BasisY;
            double wallLengthFt = 0.0;

            if (wall.Location is LocationCurve lc && lc.Curve is Line line)
            {
                XYZ p0 = line.GetEndPoint(0);
                XYZ p1 = line.GetEndPoint(1);
                wallLengthFt = (p1 - p0).GetLength();
                wallDir = new XYZ(p1.X - p0.X, p1.Y - p0.Y, 0).Normalize();
                wallNormal = new XYZ(-wallDir.Y, wallDir.X, 0).Normalize();
                if (info.Center == XYZ.Zero)
                {
                    info.Center = (p0 + p1) * 0.5;
                }
            }
            else if (bbox != null)
            {
                wallLengthFt = Math.Max(Math.Abs(bbox.Max.X - bbox.Min.X), Math.Abs(bbox.Max.Y - bbox.Min.Y));
            }

            // 3. Góc xoay của vách (tính theo wallDir)
            double rotRad = Math.Atan2(wallDir.Y, wallDir.X);
            double rotDeg = rotRad * AutoDimColumnEngine.RadToDeg;
            if (rotDeg < 0) rotDeg += 180.0;
            if (rotDeg >= 180.0) rotDeg -= 180.0;
            info.RotationAngleDeg = Math.Round(rotDeg, 1);

            // 4. Bề dày vách (Width / Thickness): Lấy mặt bên qua HostObjectUtils
            IList<Reference>? extFaces = null;
            IList<Reference>? intFaces = null;
            try
            {
                extFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                intFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Interior);
            }
            catch { }

            double thicknessFt = wall.Width;

            if (extFaces != null && extFaces.Count > 0 && intFaces != null && intFaces.Count > 0)
            {
                info.WidthPair = new ColumnFacePair
                {
                    Ref1 = extFaces[0],
                    Ref2 = intFaces[0],
                    Normal = wallNormal,
                    DistanceFt = thicknessFt
                };
                info.WidthMm = thicknessFt * 304.8;
            }

            // 5. Chiều dài vách (Length): Tìm 2 mặt đầu (End faces) từ Solid
            var opt = new Options
            {
                ComputeReferences = true,
                View = activeView,
                IncludeNonVisibleObjects = false
            };
            var geoElem = wall.get_Geometry(opt);
            var solids = geoElem != null ? EnumerateSolids(geoElem).ToList() : new List<Solid>();
            if (solids.Count == 0)
            {
                var fallbackOpt = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine,
                    IncludeNonVisibleObjects = false
                };
                geoElem = wall.get_Geometry(fallbackOpt);
                if (geoElem != null)
                {
                    solids = EnumerateSolids(geoElem).ToList();
                }
            }

            PlanarFace? endFace1 = null;
            PlanarFace? endFace2 = null;

            foreach (var s in solids)
            {
                foreach (Face f in s.Faces)
                {
                    if (f is PlanarFace pf && pf.Reference != null)
                    {
                        if (Math.Abs(pf.FaceNormal.Z) < 0.1)
                        {
                            XYZ fn2D = new XYZ(pf.FaceNormal.X, pf.FaceNormal.Y, 0).Normalize();
                            double dot = fn2D.DotProduct(wallDir);
                            if (dot > 0.85 && endFace1 == null)
                            {
                                endFace1 = pf;
                            }
                            else if (dot < -0.85 && endFace2 == null)
                            {
                                endFace2 = pf;
                            }
                        }
                    }
                }
            }

            if (endFace1 != null && endFace2 != null)
            {
                double dist = Math.Abs((endFace2.Origin - endFace1.Origin).DotProduct(wallDir));
                if (dist < 0.1 && wallLengthFt > 0) dist = wallLengthFt;

                info.LengthPair = new ColumnFacePair
                {
                    Face1 = endFace1,
                    Face2 = endFace2,
                    Ref1 = endFace1.Reference,
                    Ref2 = endFace2.Reference,
                    Normal = wallDir,
                    DistanceFt = dist
                };
                info.LengthMm = dist * 304.8;
            }
            else
            {
                info.LengthMm = wallLengthFt * 304.8;
            }

            // Fallback nếu WidthPair chưa lấy được từ HostObjectUtils
            if (info.WidthPair == null && solids.Count > 0)
            {
                var verticalFaces = new List<PlanarFace>();
                foreach (var s in solids)
                {
                    foreach (Face f in s.Faces)
                    {
                        if (f is PlanarFace pf && pf.Reference != null && Math.Abs(pf.FaceNormal.Z) < 0.1)
                        {
                            verticalFaces.Add(pf);
                        }
                    }
                }
                var pairs = FindOppositeFacePairs(verticalFaces);
                if (pairs.Count > 0)
                {
                    info.WidthPair = pairs[0];
                    info.WidthMm = pairs[0].DistanceMm;
                }
            }

            info.IsSupported = info.WidthPair != null && info.WidthPair.IsValid;
            info.StatusMessage = info.IsSupported ? "Ready" : "Unable to get wall side faces";
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

            // Sắp xếp ưu tiên các cặp mặt có diện tích mặt lớn nhất
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
                    if (instGeo != null)
                    {
                        foreach (Solid inner in EnumerateSolids(instGeo))
                        {
                            yield return inner;
                        }
                    }
                }
            }
        }
    }
}
