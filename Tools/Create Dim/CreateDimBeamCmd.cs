using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool.Tools.CreateDim
{
    /// <summary>
    /// Chọn 1 hoặc nhiều dầm trên mặt bằng kết cấu → tự tạo Dim chiều dài tổng (2 đầu dầm).
    /// - Nếu đã chọn sẵn dầm trước khi bấm lệnh → dùng luôn các dầm đó.
    /// - Nếu chưa → cho phép pick đơn hoặc quét chọn nhiều, bấm Finish để kết thúc.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CreateDimBeamCmd : IExternalCommand
    {
        /// <summary>Khoảng cách từ mép dầm tới đường Dim, tính trên giấy (mm).</summary>
        private const double PaperOffsetMm = 5.0;

        private const double ParallelTol = 0.985;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            if (view is not ViewPlan || view.ViewType == ViewType.CeilingPlan)
            {
                TaskDialog.Show("Create Dim", "Vui lòng mở một mặt bằng kết cấu (Structural / Floor Plan) trước khi chạy lệnh.");
                return Result.Cancelled;
            }

            try
            {
                List<FamilyInstance> beams = GetBeams(uidoc, doc);
                if (beams.Count == 0) return Result.Cancelled;

                int ok = 0;
                var failed = new List<string>();

                using (Transaction tx = new Transaction(doc, "JNN - Create Dim Beam"))
                {
                    tx.Start();

                    foreach (FamilyInstance beam in beams)
                    {
                        string reason = CreateLengthDimension(doc, view, beam);
                        if (reason == null) ok++;
                        else failed.Add($"• Id {GetIdValue(beam.Id)}: {reason}");
                    }

                    tx.Commit();
                }

                string report = $"Đã tạo Dim cho {ok}/{beams.Count} dầm.";
                if (failed.Count > 0)
                {
                    report += "\n\nBỏ qua:\n" + string.Join("\n", failed.Take(15));
                    if (failed.Count > 15) report += $"\n... và {failed.Count - 15} dầm khác.";
                }
                TaskDialog.Show("Create Dim", report);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Selection
        // ─────────────────────────────────────────────────────────────
        private static List<FamilyInstance> GetBeams(UIDocument uidoc, Document doc)
        {
            var filter = new BeamDimSelectionFilter();

            // 1. Ưu tiên các dầm đã được chọn sẵn
            List<FamilyInstance> preSelected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(filter.AllowElement)
                .OfType<FamilyInstance>()
                .ToList();
            if (preSelected.Count > 0) return preSelected;

            // 2. Pick đơn hoặc quét chọn nhiều → Finish
            IList<Reference> refs = uidoc.Selection.PickObjects(
                ObjectType.Element, filter,
                "Chọn dầm cần tạo Dim (click từng dầm hoặc quét chọn) → bấm Finish");

            return refs.Select(r => doc.GetElement(r))
                       .OfType<FamilyInstance>()
                       .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        //  Dimension
        // ─────────────────────────────────────────────────────────────
        /// <returns>null nếu thành công, ngược lại là lý do thất bại.</returns>
        private static string CreateLengthDimension(Document doc, View view, FamilyInstance beam)
        {
            if (beam.Location is not LocationCurve lc || lc.Curve is not Line line)
                return "Dầm cong / không có đường định vị thẳng.";

            XYZ p0 = line.GetEndPoint(0);
            XYZ p1 = line.GetEndPoint(1);
            XYZ dir3 = (p1 - p0);
            XYZ dir = new XYZ(dir3.X, dir3.Y, 0);
            if (dir.GetLength() < 1e-6) return "Dầm đứng (vuông góc mặt bằng).";
            dir = dir.Normalize();

            // Pháp tuyến ngang (vuông góc với dầm trên mặt bằng), luôn hướng lên trên / sang trái của view
            XYZ normal = OrientNormal(new XYZ(-dir.Y, dir.X, 0), view);

            // Lấy 2 mặt đầu dầm + nửa bề rộng từ geometry
            if (!TryGetEndFaces(beam, view, p0, dir, normal,
                                out Reference refStart, out Reference refEnd,
                                out double startPos, out double endPos, out double sideExtent))
                return "Không tìm thấy mặt đầu dầm (đầu dầm bị cắt xiên hoặc dầm dốc).";

            if (Math.Abs(endPos - startPos) < 1e-4) return "Chiều dài dầm bằng 0.";

            double offset = sideExtent + PaperOffsetMm / 304.8 * view.Scale;

            double z = (view as ViewPlan)?.GenLevel?.Elevation ?? p0.Z;
            XYZ basePt = new XYZ(p0.X, p0.Y, z) + normal * offset;
            XYZ a = basePt + dir * startPos;
            XYZ b = basePt + dir * endPos;

            var refArr = new ReferenceArray();
            refArr.Append(refStart);
            refArr.Append(refEnd);

            try
            {
                Dimension dim = doc.Create.NewDimension(view, Line.CreateBound(a, b), refArr);
                return dim == null ? "Revit không tạo được Dim." : null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Duyệt geometry của dầm (theo view hiện hành) để tìm 2 mặt phẳng đầu dầm
        /// (pháp tuyến song song phương dầm) nằm xa nhất về 2 phía.
        /// </summary>
        private static bool TryGetEndFaces(
            FamilyInstance beam, View view, XYZ origin, XYZ dir, XYZ normal,
            out Reference refStart, out Reference refEnd,
            out double startPos, out double endPos, out double sideExtent)
        {
            refStart = refEnd = null;
            startPos = double.MaxValue;
            endPos = double.MinValue;
            sideExtent = 0;

            var opt = new Options
            {
                ComputeReferences = true,
                View = view,
                IncludeNonVisibleObjects = false
            };

            GeometryElement geo = beam.get_Geometry(opt);
            if (geo == null) return false;

            foreach (Solid solid in EnumerateSolids(geo))
            {
                foreach (Face face in solid.Faces)
                {
                    if (face is not PlanarFace pf || pf.Reference == null) continue;

                    XYZ fn = pf.FaceNormal;
                    // Mặt đầu dầm phải thẳng đứng để Dim được trên mặt bằng
                    if (Math.Abs(fn.Z) > 1e-3) continue;

                    double d = fn.DotProduct(dir);
                    double pos = (pf.Origin - origin).DotProduct(dir);

                    if (Math.Abs(d) >= ParallelTol)
                    {
                        if (pos < startPos) { startPos = pos; refStart = pf.Reference; }
                        if (pos > endPos) { endPos = pos; refEnd = pf.Reference; }
                    }
                    else if (Math.Abs(fn.DotProduct(normal)) >= ParallelTol)
                    {
                        double side = (pf.Origin - origin).DotProduct(normal);
                        if (side > sideExtent) sideExtent = side;
                    }
                }
            }

            return refStart != null && refEnd != null && refStart != refEnd;
        }

        private static IEnumerable<Solid> EnumerateSolids(GeometryElement geo)
        {
            foreach (GeometryObject obj in geo)
            {
                if (obj is Solid s && s.Faces.Size > 0 && s.Volume > 1e-9)
                {
                    yield return s;
                }
                else if (obj is GeometryInstance gi)
                {
                    GeometryElement instGeo = gi.GetInstanceGeometry();
                    if (instGeo == null) continue;
                    foreach (Solid inner in EnumerateSolids(instGeo))
                        yield return inner;
                }
            }
        }

        /// <summary>Đặt Dim phía trên (dầm ngang) hoặc bên trái (dầm dọc) theo hướng của view.</summary>
        private static XYZ OrientNormal(XYZ n, View view)
        {
            double up = n.DotProduct(view.UpDirection);
            double right = n.DotProduct(view.RightDirection);

            if (up < -1e-6 || (Math.Abs(up) <= 1e-6 && right > 0))
                return n.Negate();
            return n;
        }

        private static long GetIdValue(ElementId id)
        {
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }

    /// <summary>Chỉ cho phép chọn dầm (Structural Framing).</summary>
    internal class BeamDimSelectionFilter : ISelectionFilter
    {
        private static readonly ElementId FramingCatId = new ElementId(BuiltInCategory.OST_StructuralFraming);

        public bool AllowElement(Element elem)
            => elem is FamilyInstance && elem.Category != null && elem.Category.Id == FramingCatId;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
