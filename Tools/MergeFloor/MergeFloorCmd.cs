using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool.Tools.MergeFloor
{
    /// <summary>
    /// Merge Floor (Gộp nhiều sàn thành 1 sàn duy nhất):
    /// - Cho phép quét chọn trước các mảnh sàn hoặc quét chọn khi chạy lệnh.
    /// - Tự động gom nhóm các sàn có cùng Tiết diện (FloorType) và cùng Cao độ mặt sàn (Elevation ±5 mm).
    /// - Hợp nhất các mảnh sàn liền kề / chạm cạnh / chồng lấn thành 1 sàn liên tục duy nhất:
    ///     + Tự động hàn kín khe hở vi mô (≤ 8 mm) và xử lý điểm giao chữ T (T-junctions) từ ETABS.
    ///     + Triệt tiêu toàn bộ các đường phân cách thừa bên trong lòng ô sàn.
    ///     + Làm sạch các điểm gãy thừa nằm thẳng hàng (Collinear simplification) để biên sàn gọn sạch.
    /// <summary>
    /// Phạm vi thực thi gộp sàn (Active View tự động hoặc Quét chọn thủ công).
    /// </summary>
    public enum MergeFloorScope
    {
        /// <summary>Tự động quét và gộp tất cả các mảnh sàn tiếp giáp/gần nhau trong View hiện hành mà không cần chọn.</summary>
        ActiveView,
        /// <summary>Quét chọn thủ công các mảnh sàn cụ thể trên màn hình.</summary>
        Selection
    }

    /// <summary>
    /// Chạy 1-click tự động gộp các mảnh sàn tiếp giáp/gần nhau trong View hiện hành (Active View)
    /// mà không cần phải quét chọn thủ công bằng chuột.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class MergeFloorActiveViewCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return MergeFloorCmd.RunMerge(commandData, MergeFloorScope.ActiveView, ref message);
        }
    }

    /// <summary>
    /// Quét chọn thủ công các mảnh sàn trên màn hình để gộp thành 1 sàn duy nhất.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class MergeFloorSelectCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return MergeFloorCmd.RunMerge(commandData, MergeFloorScope.Selection, ref message);
        }
    }

    /// <summary>
    /// Merge Floor (Gộp nhiều sàn thành 1 sàn duy nhất):
    /// - Cho phép quét chọn trước các mảnh sàn hoặc quét chọn khi chạy lệnh.
    /// - Hỗ trợ chế độ Active View (tự động tìm và gộp các sàn gần nhau trong view mà không cần chọn).
    /// - Tự động gom nhóm các sàn có cùng Tiết diện (FloorType) và cùng Cao độ mặt sàn (Elevation ±5 mm).
    /// - Hợp nhất các mảnh sàn liền kề / chạm cạnh / chồng lấn thành 1 sàn liên tục duy nhất:
    ///     + Tự động hàn kín khe hở vi mô (≤ 8 mm) và xử lý điểm giao chữ T (T-junctions) từ ETABS.
    ///     + Triệt tiêu toàn bộ các đường phân cách thừa bên trong lòng ô sàn.
    ///     + Làm sạch các điểm gãy thừa nằm thẳng hàng (Collinear simplification) để biên sàn gọn sạch.
    ///     + Giữ nguyên lỗ mở (Openings), tham số cao độ (Level + Offset), Structural và tự khôi phục Join Geometry.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class MergeFloorCmd : IExternalCommand
    {
        /// <summary>Dung sai so khớp cao độ mặt trên của sàn (5 mm đổi ra feet).</summary>
        private const double ElevationTolFt = 5.0 / 304.8;

        /// <summary>Dung sai hàn đỉnh và bắt điểm T-junction giữa các mảnh sàn liền kề (8 mm đổi ra feet).</summary>
        private const double SnapTolFt = 8.0 / 304.8;

        /// <summary>Chiều dài cạnh tối thiểu hợp lệ trong Revit (3 mm đổi ra feet, lớn hơn ShortCurveTolerance).</summary>
        private const double MinEdgeLengthFt = 3.0 / 304.8;

        /// <summary>Dung sai độ lệch phương khi gộp 2 đoạn thẳng hàng (2 mm đổi ra feet).</summary>
        private const double CollinearDeviationFt = 2.0 / 304.8;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
                return Result.Cancelled;

            Document doc = uidoc.Document;

            // Kiểm tra xem người dùng đã quét chọn sẵn >= 2 sàn chưa
            var filter = new FloorSelectionFilter();
            List<Floor> preSelected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(filter.AllowElement)
                .OfType<Floor>()
                .ToList();

            if (preSelected.Count >= 2)
            {
                // Đã chọn trước >= 2 sàn -> gộp ngay lập tức
                return RunMerge(commandData, MergeFloorScope.Selection, ref message, preSelected);
            }

            // Nếu chưa chọn sàn nào -> hiển thị TaskDialog 2 tùy chọn
            TaskDialog dialog = new TaskDialog("JNN - Merge Floor")
            {
                MainInstruction = "Chọn phạm vi gộp sàn (Merge Floor)",
                MainContent = "Tự động hợp nhất các mảnh sàn liền kề / gần nhau có cùng Tiết diện & Cao độ thành 1 sàn duy nhất:\n" +
                              "• Tự động hàn kín khe hở vi mô và xử lý điểm giao T-junctions\n" +
                              "• Triệt tiêu đường phân cách thừa bên trong ô sàn\n" +
                              "• Khôi phục liên kết Join Geometry với Cột / Dầm / Tường.",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                CommonButtons = TaskDialogCommonButtons.Close
            };

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                "Tự động gộp trong View hiện hành (Active View) [Khuyên dùng]",
                "Tự động quét và gộp tất cả các mảnh sàn tiếp giáp/gần nhau trong view này mà không cần chọn tay.");

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                "Quét chọn sàn thủ công (Pick Elements)",
                "Dùng chuột quét chọn các mảnh sàn cụ thể trên màn hình để gộp.");

            var res = dialog.Show();
            if (res == TaskDialogResult.CommandLink1)
            {
                return RunMerge(commandData, MergeFloorScope.ActiveView, ref message);
            }
            else if (res == TaskDialogResult.CommandLink2)
            {
                return RunMerge(commandData, MergeFloorScope.Selection, ref message);
            }

            return Result.Cancelled;
        }

        /// <summary>
        /// Thực thi gộp sàn theo phạm vi (Active View hoặc Selection).
        /// </summary>
        public static Result RunMerge(
            ExternalCommandData commandData,
            MergeFloorScope scope,
            ref string message,
            List<Floor> preSelected = null)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
                return Result.Cancelled;

            Document doc = uidoc.Document;

            try
            {
                List<Floor> targetFloors;

                if (scope == MergeFloorScope.ActiveView)
                {
                    targetFloors = GetFloorsInActiveView(doc, out string viewErr);
                    if (targetFloors == null)
                    {
                        TaskDialog.Show("Merge Floor (Active View)", viewErr ?? "View hiện hành không hỗ trợ gộp sàn.");
                        return Result.Cancelled;
                    }

                    if (targetFloors.Count == 0)
                    {
                        TaskDialog.Show("Merge Floor (Active View)", "Không tìm thấy đối tượng Sàn (Floor) nào đang hiển thị trong View hiện hành.");
                        return Result.Cancelled;
                    }

                    if (targetFloors.Count < 2)
                    {
                        TaskDialog.Show("Merge Floor (Active View)", "Trong View hiện hành chỉ có 1 sàn duy nhất (không có sàn thứ 2 để gộp).");
                        return Result.Cancelled;
                    }
                }
                else
                {
                    targetFloors = preSelected ?? GetTargetFloorsBySelection(uidoc, doc);
                    if (targetFloors.Count == 0)
                        return Result.Cancelled;

                    if (targetFloors.Count < 2)
                    {
                        TaskDialog.Show("Merge Floor (Selection)", "Vui lòng chọn ít nhất 2 mảnh sàn để thực hiện gộp sàn.");
                        return Result.Cancelled;
                    }
                }

                // 1. Trích xuất thông tin hình học & phân nhóm theo (FloorTypeId + Cao độ Z)
                var floorInfos = new List<FloorInputInfo>();
                var skippedReasons = new List<string>();

                foreach (Floor f in targetFloors)
                {
                    if (TryExtractFloorInfo(doc, f, out FloorInputInfo info, out string skipReason))
                    {
                        floorInfos.Add(info);
                    }
                    else if (!string.IsNullOrEmpty(skipReason))
                    {
                        skippedReasons.Add($"#{GetIdValue(f.Id)}: {skipReason}");
                    }
                }

                if (floorInfos.Count < 2)
                {
                    string detail = skippedReasons.Count > 0
                        ? "\n\nChi tiết:\n• " + string.Join("\n• ", skippedReasons.Take(10))
                        : string.Empty;
                    TaskDialog.Show("Merge Floor", "Không đủ sàn phẳng hợp lệ để gộp." + detail);
                    return Result.Cancelled;
                }

                List<List<FloorInputInfo>> compatibleGroups = GroupByTypeAndElevation(floorInfos);

                // Kiểm tra trước: có cụm nào >= 2 sàn chạm/gần nhau không
                int totalMergeableClusters = 0;
                foreach (var grp in compatibleGroups)
                {
                    var clusters = FindConnectedComponents(grp, SnapTolFt * 1.5);
                    totalMergeableClusters += clusters.Count(c => c.Count >= 2);
                }

                if (totalMergeableClusters == 0)
                {
                    string scopeDesc = (scope == MergeFloorScope.ActiveView) ? "trong View hiện hành" : "trong các sàn đã chọn";
                    TaskDialog.Show("Merge Floor",
                        $"Đã kiểm tra {targetFloors.Count} sàn {scopeDesc} nhưng không có mảnh sàn nào tiếp giáp/gần nhau để gộp.\n\n" +
                        "• Tất cả các sàn đều đứng độc lập hoặc khác loại / khác cao độ.\n" +
                        "• Mô hình của bạn đã được giữ nguyên an toàn.");
                    return Result.Cancelled;
                }

                var mergeResults = new List<MergeGroupResult>();
                var createdFloorIds = new List<ElementId>();

                string txName = scope == MergeFloorScope.ActiveView
                    ? "JNN - Merge Floor (Active View)"
                    : "JNN - Merge Floor (Selection)";

                using (Transaction tx = new Transaction(doc, txName))
                {
                    tx.Start();

                    FailureHandlingOptions fho = tx.GetFailureHandlingOptions();
                    fho.SetFailuresPreprocessor(new WarningSwallower());
                    tx.SetFailureHandlingOptions(fho);

                    foreach (List<FloorInputInfo> group in compatibleGroups)
                    {
                        if (group.Count < 2)
                        {
                            mergeResults.Add(new MergeGroupResult
                            {
                                TypeName = group[0].TypeName,
                                SourceFloorCount = 1,
                                MergedFloorCount = 0,
                                Note = "Chỉ có 1 sàn riêng lẻ cùng tiết diện & cao độ (không có sàn thứ 2 để gộp)."
                            });
                            continue;
                        }

                        MergeGroupResult groupResult = ProcessCompatibleGroup(doc, group, createdFloorIds);
                        mergeResults.Add(groupResult);
                    }

                    tx.Commit();
                }

                // Highlight các sàn mới vừa được gộp trên màn hình Revit
                if (createdFloorIds.Count > 0)
                {
                    try
                    {
                        uidoc.Selection.SetElementIds(createdFloorIds);
                    }
                    catch { }
                }

                ShowReport(targetFloors.Count, mergeResults, skippedReasons, scope);
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

        // ═════════════════════════════════════════════════════════════════
        //  1. Selection & Extraction
        // ═════════════════════════════════════════════════════════════════

        private static List<Floor> GetFloorsInActiveView(Document doc, out string invalidViewReason)
        {
            invalidViewReason = null;
            View activeView = doc.ActiveView;
            if (activeView == null)
            {
                invalidViewReason = "Không tìm thấy View hiện hành.";
                return null;
            }

            if (!(activeView is ViewPlan) && !(activeView is View3D))
            {
                invalidViewReason = "Chức năng gộp sàn theo View hiện hành (Active View) chỉ áp dụng cho Mặt bằng (Floor Plan, Structural Plan) hoặc 3D View.\n\nVui lòng mở một mặt bằng trước khi thực hiện.";
                return null;
            }

            var filter = new FloorSelectionFilter();
            var collector = new FilteredElementCollector(doc, activeView.Id)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType();

            var result = new List<Floor>();
            foreach (Element elem in collector)
            {
                if (elem is Floor f && !f.IsHidden(activeView) && filter.AllowElement(f))
                {
                    result.Add(f);
                }
            }
            return result;
        }

        private static List<Floor> GetTargetFloorsBySelection(UIDocument uidoc, Document doc)
        {
            var filter = new FloorSelectionFilter();

            // 1. Nếu người dùng đã quét chọn sẵn >= 2 sàn trước khi bấm nút -> lấy luôn
            List<Floor> preSelected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(filter.AllowElement)
                .OfType<Floor>()
                .ToList();

            if (preSelected.Count >= 2)
                return preSelected;

            // 2. Nếu chưa chọn (hoặc mới chọn 1 sàn) -> bật chế độ quét chọn trực tiếp
            IList<Reference> pickedRefs = uidoc.Selection.PickObjects(
                ObjectType.Element,
                filter,
                "Quét chọn các mảnh sàn cần gộp thành 1 sàn rồi nhấn Finish trên thanh Options Bar");

            if (pickedRefs == null || pickedRefs.Count == 0)
                return new List<Floor>();

            return pickedRefs
                .Select(r => doc.GetElement(r.ElementId))
                .Where(filter.AllowElement)
                .OfType<Floor>()
                .GroupBy(f => GetIdValue(f.Id))
                .Select(g => g.First())
                .ToList();
        }

        private static bool TryExtractFloorInfo(Document doc, Floor floor, out FloorInputInfo info, out string skipReason)
        {
            info = null;
            skipReason = null;

            if (floor == null || !floor.IsValidObject)
            {
                skipReason = "Đối tượng không hợp lệ.";
                return false;
            }

            ElementId typeId = floor.GetTypeId();
            FloorType floorType = doc.GetElement(typeId) as FloorType;
            if (floorType == null)
            {
                skipReason = "Không tìm thấy FloorType.";
                return false;
            }

            ElementId levelId = floor.LevelId;
            Level level = doc.GetElement(levelId) as Level;
            if (level == null)
            {
                skipReason = "Sàn không gắn với Level hợp lệ.";
                return false;
            }

            // Kiểm tra độ dốc của sàn: chỉ gộp sàn phẳng ngang
            double slopeAngle = 0.0;
            Parameter slopeParam = floor.get_Parameter(BuiltInParameter.ROOF_SLOPE);
            if (slopeParam != null && slopeParam.HasValue)
                slopeAngle = Math.Abs(slopeParam.AsDouble());

            if (slopeAngle > 1e-4)
            {
                skipReason = "Sàn có độ dốc (Sloped Floor) không hỗ trợ gộp phẳng.";
                return false;
            }

            double heightOffset = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;
            double topZ = level.Elevation + heightOffset;

            // Trích xuất các CurveLoop từ Sketch (ưu tiên số 1 để không bị mẻ góc do Join Cột/Dầm)
            bool hasCurvedEdges = false;
            List<CurveLoop> rawLoops = ExtractSketchLoops(doc, floor, topZ, out hasCurvedEdges);

            if (rawLoops.Count == 0)
            {
                // Dự phòng: lấy từ Top PlanarFace nếu không đọc được Sketch
                rawLoops = ExtractTopFaceLoops(floor, topZ, out hasCurvedEdges, out string faceErr);
                if (rawLoops.Count == 0)
                {
                    skipReason = faceErr ?? "Không đọc được biên dạng sàn.";
                    return false;
                }
            }

            // Chuyển các loop sang đa giác 2D (nếu toàn đoạn thẳng)
            var polyLoops = new List<List<Pt2D>>();
            if (!hasCurvedEdges)
            {
                foreach (CurveLoop cl in rawLoops)
                {
                    var pts = new List<Pt2D>();
                    foreach (Curve c in cl)
                    {
                        XYZ p = c.GetEndPoint(0);
                        pts.Add(new Pt2D(p.X, p.Y));
                    }
                    pts = CleanPolygonVertices(pts, 1e-4);
                    if (pts.Count >= 3)
                        polyLoops.Add(pts);
                }

                if (polyLoops.Count == 0)
                {
                    skipReason = "Biên dạng 2D của sàn bị suy biến.";
                    return false;
                }
            }

            // Lưu danh sách các cấu kiện đang Join với sàn này để khôi phục sau khi gộp
            var joinedItems = new List<JoinedElementRecord>();
            try
            {
                ICollection<ElementId> joinedIds = JoinGeometryUtils.GetJoinedElements(doc, floor);
                if (joinedIds != null)
                {
                    foreach (ElementId jId in joinedIds)
                    {
                        Element jElem = doc.GetElement(jId);
                        if (jElem != null)
                        {
                            bool floorCutsOther = false;
                            try
                            {
                                floorCutsOther = JoinGeometryUtils.IsCuttingElementInJoin(doc, floor, jElem);
                            }
                            catch { }

                            joinedItems.Add(new JoinedElementRecord
                            {
                                JoinedId = jId,
                                FloorWasCutting = floorCutsOther
                            });
                        }
                    }
                }
            }
            catch { }

            int isStructural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() ?? 1;
            string comments = floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
            string mark = floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();

            double areaApprox = 0.0;
            foreach (var pl in polyLoops)
                areaApprox += Math.Abs(ComputeSignedArea(pl));

            // Tính BoundingBox 2D để phân cụm nhanh
            BoundingBoxUV bbox2D = ComputeBounds2D(rawLoops);

            info = new FloorInputInfo
            {
                FloorId = floor.Id,
                FloorTypeId = typeId,
                TypeName = floorType.Name,
                LevelId = levelId,
                LevelElevation = level.Elevation,
                HeightOffset = heightOffset,
                TopZ = topZ,
                IsStructural = isStructural,
                Comments = comments,
                Mark = mark,
                RawLoops = rawLoops,
                PolyLoops = polyLoops,
                HasCurvedEdges = hasCurvedEdges,
                AreaApprox = areaApprox,
                Bounds2D = bbox2D,
                JoinedElements = joinedItems
            };
            return true;
        }

        private static List<CurveLoop> ExtractSketchLoops(Document doc, Floor floor, double targetZ, out bool hasCurvedEdges)
        {
            hasCurvedEdges = false;
            var result = new List<CurveLoop>();

            try
            {
                Sketch sketch = null;
                try
                {
                    if (floor.SketchId != null && floor.SketchId != ElementId.InvalidElementId)
                        sketch = doc.GetElement(floor.SketchId) as Sketch;
                }
                catch { }

                if (sketch == null)
                {
                    IList<ElementId> depSketches = floor.GetDependentElements(new ElementClassFilter(typeof(Sketch)));
                    if (depSketches != null && depSketches.Count > 0)
                        sketch = doc.GetElement(depSketches[0]) as Sketch;
                }

                if (sketch?.Profile == null || sketch.Profile.Size == 0)
                    return result;

                foreach (CurveArray ca in sketch.Profile)
                {
                    if (ca == null || ca.Size == 0) continue;
                    if (TryBuildHorizontalCurveLoop(ca.Cast<Curve>().ToList(), targetZ, ref hasCurvedEdges, out CurveLoop loop))
                        result.Add(loop);
                }

                // Kiểm tra thêm các đối tượng Opening độc lập đục trên sàn này (nếu có)
                IList<ElementId> openingIds = floor.GetDependentElements(new ElementClassFilter(typeof(Opening)));
                if (openingIds != null)
                {
                    foreach (ElementId opId in openingIds)
                    {
                        Opening op = doc.GetElement(opId) as Opening;
                        if (op == null) continue;

                        if (op.IsRectBoundary && op.BoundaryRect != null && op.BoundaryRect.Count >= 2)
                        {
                            XYZ pMin = op.BoundaryRect[0];
                            XYZ pMax = op.BoundaryRect[1];
                            double x0 = Math.Min(pMin.X, pMax.X);
                            double x1 = Math.Max(pMin.X, pMax.X);
                            double y0 = Math.Min(pMin.Y, pMax.Y);
                            double y1 = Math.Max(pMin.Y, pMax.Y);
                            if (x1 - x0 > MinEdgeLengthFt && y1 - y0 > MinEdgeLengthFt)
                            {
                                // Lỗ mở theo chiều kim đồng hồ (CW)
                                var pts = new[]
                                {
                                    new XYZ(x0, y0, targetZ),
                                    new XYZ(x0, y1, targetZ),
                                    new XYZ(x1, y1, targetZ),
                                    new XYZ(x1, y0, targetZ)
                                };
                                var holeLoop = new CurveLoop();
                                for (int i = 0; i < 4; i++)
                                    holeLoop.Append(Line.CreateBound(pts[i], pts[(i + 1) % 4]));
                                result.Add(holeLoop);
                            }
                        }
                        else if (op.BoundaryCurves != null && op.BoundaryCurves.Size > 0)
                        {
                            if (TryBuildHorizontalCurveLoop(op.BoundaryCurves.Cast<Curve>().ToList(), targetZ, ref hasCurvedEdges, out CurveLoop holeLoop))
                                result.Add(holeLoop);
                        }
                    }
                }
            }
            catch
            {
                result.Clear();
            }

            return result;
        }

        private static List<CurveLoop> ExtractTopFaceLoops(Floor floor, double targetZ, out bool hasCurvedEdges, out string error)
        {
            hasCurvedEdges = false;
            error = null;
            var result = new List<CurveLoop>();

            try
            {
                IList<Reference> topRefs = HostObjectUtils.GetTopFaces(floor);
                if (topRefs == null || topRefs.Count == 0)
                {
                    error = "Không tìm thấy mặt trên (Top Face) của sàn.";
                    return result;
                }

                foreach (Reference r in topRefs)
                {
                    PlanarFace pf = floor.GetGeometryObjectFromReference(r) as PlanarFace;
                    if (pf == null) continue;

                    if (Math.Abs(pf.FaceNormal.Z) < 0.999)
                    {
                        error = "Mặt sàn nghiêng (không nằm ngang).";
                        return new List<CurveLoop>();
                    }

                    foreach (CurveLoop cl in pf.GetEdgesAsCurveLoops())
                    {
                        if (TryBuildHorizontalCurveLoop(cl.ToList(), targetZ, ref hasCurvedEdges, out CurveLoop flatLoop))
                            result.Add(flatLoop);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        private static bool TryBuildHorizontalCurveLoop(List<Curve> curves, double targetZ, ref bool hasCurvedEdges, out CurveLoop loop)
        {
            loop = null;
            if (curves == null || curves.Count == 0) return false;

            try
            {
                // Chiếu toàn bộ các curve về cùng mặt phẳng Z = targetZ
                var projected = new List<Curve>();
                foreach (Curve c in curves)
                {
                    XYZ p0 = c.GetEndPoint(0);
                    XYZ p1 = c.GetEndPoint(1);
                    XYZ a = new XYZ(p0.X, p0.Y, targetZ);
                    XYZ b = new XYZ(p1.X, p1.Y, targetZ);

                    if (a.DistanceTo(b) < 1e-4 && c is Line) continue;

                    if (c is Line)
                    {
                        projected.Add(Line.CreateBound(a, b));
                    }
                    else if (c is Arc arc)
                    {
                        hasCurvedEdges = true;
                        XYZ mid = arc.Evaluate(0.5, true);
                        XYZ m = new XYZ(mid.X, mid.Y, targetZ);
                        projected.Add(Arc.Create(a, b, m));
                    }
                    else
                    {
                        // Nội suy đoạn cong khác thành đa giác
                        IList<XYZ> tess = c.Tessellate();
                        for (int i = 0; i < tess.Count - 1; i++)
                        {
                            XYZ ta = new XYZ(tess[i].X, tess[i].Y, targetZ);
                            XYZ tb = new XYZ(tess[i + 1].X, tess[i + 1].Y, targetZ);
                            if (ta.DistanceTo(tb) >= MinEdgeLengthFt)
                                projected.Add(Line.CreateBound(ta, tb));
                        }
                    }
                }

                if (projected.Count == 0) return false;

                // Sắp xếp nối tiếp các curve thành chuỗi khép kín
                var ordered = OrderCurvesContiguously(projected);
                var candidate = new CurveLoop();
                foreach (Curve c in ordered)
                    candidate.Append(c);

                if (!candidate.IsOpen())
                {
                    loop = candidate;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static List<Curve> OrderCurvesContiguously(List<Curve> curves)
        {
            var remaining = new List<Curve>(curves);
            var ordered = new List<Curve> { remaining[0] };
            remaining.RemoveAt(0);

            while (remaining.Count > 0)
            {
                XYZ currentEnd = ordered[ordered.Count - 1].GetEndPoint(1);
                int bestIdx = -1;
                bool reverse = false;
                double bestDist = double.MaxValue;

                for (int i = 0; i < remaining.Count; i++)
                {
                    double d0 = currentEnd.DistanceTo(remaining[i].GetEndPoint(0));
                    double d1 = currentEnd.DistanceTo(remaining[i].GetEndPoint(1));
                    if (d0 < bestDist)
                    {
                        bestDist = d0;
                        bestIdx = i;
                        reverse = false;
                    }
                    if (d1 < bestDist)
                    {
                        bestDist = d1;
                        bestIdx = i;
                        reverse = true;
                    }
                }

                if (bestIdx < 0) break;

                Curve next = remaining[bestIdx];
                remaining.RemoveAt(bestIdx);
                if (reverse)
                    next = next.CreateReversed();

                // Khép khít điểm nối nếu lệch nhỏ
                XYZ expectedStart = currentEnd;
                XYZ nextEnd = next.GetEndPoint(1);
                if (remaining.Count == 0)
                    nextEnd = ordered[0].GetEndPoint(0);

                if (next is Line && expectedStart.DistanceTo(nextEnd) >= 1e-4)
                    next = Line.CreateBound(expectedStart, nextEnd);

                ordered.Add(next);
            }

            return ordered;
        }

        // ═════════════════════════════════════════════════════════════════
        //  2. Grouping & Connected Component Processing
        // ═════════════════════════════════════════════════════════════════

        private static List<List<FloorInputInfo>> GroupByTypeAndElevation(List<FloorInputInfo> floors)
        {
            var groups = new List<List<FloorInputInfo>>();

            foreach (FloorInputInfo f in floors)
            {
                List<FloorInputInfo> matched = null;
                foreach (var g in groups)
                {
                    FloorInputInfo rep = g[0];
                    if (rep.FloorTypeId == f.FloorTypeId && Math.Abs(rep.TopZ - f.TopZ) <= ElevationTolFt)
                    {
                        matched = g;
                        break;
                    }
                }

                if (matched != null)
                    matched.Add(f);
                else
                    groups.Add(new List<FloorInputInfo> { f });
            }

            return groups;
        }

        private static MergeGroupResult ProcessCompatibleGroup(
            Document doc,
            List<FloorInputInfo> group,
            List<ElementId> createdFloorIds)
        {
            var result = new MergeGroupResult
            {
                TypeName = group[0].TypeName,
                SourceFloorCount = group.Count
            };

            // Chia group thành các cụm liên thông (Connected Components) chạm nhau hoặc cách nhau ≤ SnapTolFt
            List<List<FloorInputInfo>> components = FindConnectedComponents(group, SnapTolFt * 1.5);

            foreach (List<FloorInputInfo> comp in components)
            {
                if (comp.Count < 2)
                {
                    result.SkippedSingleCount++;
                    continue;
                }

                // Chọn sàn chủ đạo (có diện tích lớn nhất) để kế thừa Level, Offset, Structural, Comments, Mark
                FloorInputInfo primary = comp.OrderByDescending(x => x.AreaApprox).First();
                double targetZ = primary.TopZ;

                // Thực hiện hợp nhất biên dạng 2D (hoặc 3D Solid fallback nếu có cạnh cong)
                List<List<CurveLoop>> mergedIslands = null;
                string mergeErr = null;

                bool anyCurved = comp.Any(c => c.HasCurvedEdges);
                if (!anyCurved)
                {
                    mergedIslands = TryMergeBy2DArrangement(comp, targetZ, out mergeErr);
                }

                if (mergedIslands == null || mergedIslands.Count == 0)
                {
                    mergedIslands = TryMergeBySolidBoolean(comp, targetZ, out mergeErr);
                }

                if (mergedIslands == null || mergedIslands.Count == 0)
                {
                    result.Errors.Add(mergeErr ?? "Không thể hợp nhất hình học các mảnh sàn.");
                    continue;
                }

                // Thực hiện thay thế các sàn cũ bằng sàn gộp mới trong SubTransaction an toàn
                using (SubTransaction subTx = new SubTransaction(doc))
                {
                    subTx.Start();
                    try
                    {
                        var compSourceIds = new HashSet<long>(comp.Select(c => GetIdValue(c.FloorId)));

                        // Xóa các mảnh sàn cũ trước khi tạo sàn mới để tránh cảnh báo trùng sàn
                        IList<ElementId> idsToDelete = comp.Select(c => c.FloorId).ToList();
                        doc.Delete(idsToDelete);

                        var newlyCreatedInComp = new List<Floor>();

                        foreach (List<CurveLoop> islandLoops in mergedIslands)
                        {
                            Floor newFloor = Floor.Create(doc, islandLoops, primary.FloorTypeId, primary.LevelId);
                            if (newFloor == null)
                                throw new InvalidOperationException("Revit Floor.Create trả về null.");

                            // Khôi phục đúng cao độ mặt sàn so với Level của primary floor
                            double newOffset = targetZ - primary.LevelElevation;
                            Parameter offsetParam = newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                            if (offsetParam != null && !offsetParam.IsReadOnly)
                                offsetParam.Set(newOffset);

                            Parameter structParam = newFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                            if (structParam != null && !structParam.IsReadOnly)
                                structParam.Set(primary.IsStructural);

                            if (!string.IsNullOrEmpty(primary.Comments))
                            {
                                Parameter cmtParam = newFloor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                if (cmtParam != null && !cmtParam.IsReadOnly)
                                    cmtParam.Set(primary.Comments);
                            }

                            if (!string.IsNullOrEmpty(primary.Mark))
                            {
                                Parameter markParam = newFloor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                                if (markParam != null && !markParam.IsReadOnly)
                                    markParam.Set(primary.Mark);
                            }

                            newlyCreatedInComp.Add(newFloor);
                        }

                        doc.Regenerate();

                        // Khôi phục liên kết Join Geometry từ các mảnh sàn cũ sang sàn mới
                        var uniqueJoins = comp
                            .SelectMany(c => c.JoinedElements)
                            .Where(j => !compSourceIds.Contains(GetIdValue(j.JoinedId)))
                            .GroupBy(j => GetIdValue(j.JoinedId))
                            .Select(g => g.First())
                            .ToList();

                        foreach (Floor nf in newlyCreatedInComp)
                        {
                            foreach (JoinedElementRecord jr in uniqueJoins)
                            {
                                Element other = doc.GetElement(jr.JoinedId);
                                if (other == null || !other.IsValidObject) continue;

                                try
                                {
                                    if (!JoinGeometryUtils.AreElementsJoined(doc, nf, other))
                                    {
                                        JoinGeometryUtils.JoinGeometry(doc, nf, other);
                                    }

                                    if (JoinGeometryUtils.AreElementsJoined(doc, nf, other))
                                    {
                                        bool currentFloorCutting = JoinGeometryUtils.IsCuttingElementInJoin(doc, nf, other);
                                        if (currentFloorCutting != jr.FloorWasCutting)
                                        {
                                            JoinGeometryUtils.SwitchJoinOrder(doc, nf, other);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }

                        subTx.Commit();

                        foreach (Floor nf in newlyCreatedInComp)
                        {
                            createdFloorIds.Add(nf.Id);
                            result.MergedFloorCount++;
                            result.CreatedIds.Add(nf.Id);
                        }
                        result.MergedSourceCount += comp.Count;
                    }
                    catch (Exception ex)
                    {
                        subTx.RollBack();
                        result.Errors.Add(ex.Message);
                    }
                }
            }

            return result;
        }

        private static List<List<FloorInputInfo>> FindConnectedComponents(List<FloorInputInfo> items, double bboxTol)
        {
            int n = items.Count;
            var visited = new bool[n];
            var components = new List<List<FloorInputInfo>>();

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;
                var comp = new List<FloorInputInfo>();
                var queue = new Queue<int>();
                visited[i] = true;
                queue.Enqueue(i);

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    comp.Add(items[cur]);

                    for (int j = 0; j < n; j++)
                    {
                        if (visited[j]) continue;
                        if (BoundsTouchOrOverlap(items[cur].Bounds2D, items[j].Bounds2D, bboxTol) &&
                            PolygonsTouchOrOverlap(items[cur], items[j], bboxTol))
                        {
                            visited[j] = true;
                            queue.Enqueue(j);
                        }
                    }
                }

                components.Add(comp);
            }

            return components;
        }

        private static bool BoundsTouchOrOverlap(BoundingBoxUV a, BoundingBoxUV b, double tol)
        {
            if (a == null || b == null) return true;
            if (a.Max.U + tol < b.Min.U || b.Max.U + tol < a.Min.U) return false;
            if (a.Max.V + tol < b.Min.V || b.Max.V + tol < a.Min.V) return false;
            return true;
        }

        private static bool PolygonsTouchOrOverlap(FloorInputInfo a, FloorInputInfo b, double tol)
        {
            if (a.HasCurvedEdges || b.HasCurvedEdges) return true;
            if (a.PolyLoops.Count == 0 || b.PolyLoops.Count == 0) return true;

            // Kiểm tra khoảng cách giữa các đoạn thẳng của 2 sàn ≤ tol hoặc điểm nằm trong đa giác
            foreach (var loopA in a.PolyLoops)
            {
                foreach (var loopB in b.PolyLoops)
                {
                    for (int i = 0; i < loopA.Count; i++)
                    {
                        Pt2D a0 = loopA[i];
                        Pt2D a1 = loopA[(i + 1) % loopA.Count];
                        for (int j = 0; j < loopB.Count; j++)
                        {
                            Pt2D b0 = loopB[j];
                            Pt2D b1 = loopB[(j + 1) % loopB.Count];
                            if (SegmentDistance2D(a0, a1, b0, b1) <= tol)
                                return true;
                        }
                    }

                    if (IsPointInPolygon2D(loopA[0], loopB) || IsPointInPolygon2D(loopB[0], loopA))
                        return true;
                }
            }

            return false;
        }

        // ═════════════════════════════════════════════════════════════════
        //  3. Engine 1: Exact 2D Planar Polygon Union + T-Junction Healing
        // ═════════════════════════════════════════════════════════════════

        private static List<List<CurveLoop>> TryMergeBy2DArrangement(
            List<FloorInputInfo> comp,
            double targetZ,
            out string error)
        {
            error = null;
            try
            {
                // Bước 1: Phân loại Outer Loop (CCW, Area > 0) và Hole Loop (CW, Area < 0) cho từng mảnh sàn
                var floorRegions = new List<FloorPolyRegion>();
                var rawVertices = new List<Pt2D>();

                foreach (FloorInputInfo f in comp)
                {
                    if (f.PolyLoops == null || f.PolyLoops.Count == 0) continue;

                    var sortedLoops = f.PolyLoops
                        .Select(l => new { Loop = l, Area = ComputeSignedArea(l) })
                        .Where(x => Math.Abs(x.Area) > 1e-4)
                        .OrderByDescending(x => Math.Abs(x.Area))
                        .ToList();

                    if (sortedLoops.Count == 0) continue;

                    var region = new FloorPolyRegion();
                    for (int i = 0; i < sortedLoops.Count; i++)
                    {
                        var pts = new List<Pt2D>(sortedLoops[i].Loop);
                        double area = sortedLoops[i].Area;
                        if (i == 0)
                        {
                            // Outer loop: chuẩn hóa ngược chiều kim đồng hồ (CCW: Area > 0)
                            if (area < 0) pts.Reverse();
                            region.Outer = pts;
                        }
                        else
                        {
                            // Inner hole: chuẩn hóa cùng chiều kim đồng hồ (CW: Area < 0)
                            if (area > 0) pts.Reverse();
                            region.Holes.Add(pts);
                        }
                        rawVertices.AddRange(pts);
                    }
                    floorRegions.Add(region);
                }

                if (floorRegions.Count == 0)
                {
                    error = "Không có biên dạng đa giác hợp lệ.";
                    return null;
                }

                // Bước 2: Gom cụm đỉnh (Vertex Clustering) trong phạm vi SnapTolFt (8 mm)
                // để triệt tiêu sai số làm tròn từ ETABS / vẽ tay
                List<Pt2D> canonicalVerts = BuildCanonicalVertices(rawVertices, SnapTolFt);

                foreach (FloorPolyRegion reg in floorRegions)
                {
                    reg.Outer = SnapLoopToCanonical(reg.Outer, canonicalVerts, SnapTolFt);
                    for (int h = reg.Holes.Count - 1; h >= 0; h--)
                    {
                        reg.Holes[h] = SnapLoopToCanonical(reg.Holes[h], canonicalVerts, SnapTolFt);
                        if (reg.Holes[h].Count < 3)
                            reg.Holes.RemoveAt(h);
                    }
                }
                floorRegions.RemoveAll(r => r.Outer == null || r.Outer.Count < 3);

                // Bước 3: Chiếu các đỉnh T-Junction nằm sát cạnh (≤ SnapTolFt) thẳng vào cạnh đó
                // rồi chèn đỉnh vào mọi cạnh đi qua để 2 mảnh sàn ghép khít tuyệt đối 100%
                ProjectTJunctionsToEdges(floorRegions, canonicalVerts, SnapTolFt);

                foreach (FloorPolyRegion reg in floorRegions)
                {
                    reg.Outer = SnapLoopToCanonical(reg.Outer, canonicalVerts, SnapTolFt);
                    reg.Outer = InjectTJunctionVertices(reg.Outer, canonicalVerts, SnapTolFt);
                    for (int h = 0; h < reg.Holes.Count; h++)
                    {
                        reg.Holes[h] = SnapLoopToCanonical(reg.Holes[h], canonicalVerts, SnapTolFt);
                        reg.Holes[h] = InjectTJunctionVertices(reg.Holes[h], canonicalVerts, SnapTolFt);
                    }
                }
                floorRegions.RemoveAll(r => r.Outer == null || r.Outer.Count < 3);

                // Bước 4: Thu thập toàn bộ đoạn thẳng & chia nhỏ tại các giao điểm thực (X-intersections)
                var allSegments = new List<Seg2D>();
                foreach (FloorPolyRegion reg in floorRegions)
                {
                    AddLoopSegments(reg.Outer, allSegments);
                    foreach (var hole in reg.Holes)
                        AddLoopSegments(hole, allSegments);
                }

                List<Seg2D> elementarySegments = SubdivideSegmentsAtIntersections(allSegments, canonicalVerts, 1e-4);

                if (elementarySegments.Count < 3)
                {
                    error = "Không tạo được lưới đoạn thẳng phẳng.";
                    return null;
                }

                // Bước 5: Xác định các cạnh biên thực sự của hợp nhất (Union Boundary Half-Edges)
                // Một đoạn cơ sở nằm trên biên của hợp nhất khi và chỉ khi 1 bên thuộc Hợp và 1 bên nằm Ngoài Hợp.
                // Mọi cạnh chung nằm giữa 2 mảnh sàn (cả 2 bên đều thuộc Hợp) sẽ tự động bị loại bỏ 100%!
                var boundaryHalfEdges = new List<DirectedEdge>();

                foreach (Seg2D seg in elementarySegments)
                {
                    Pt2D u = canonicalVerts[seg.U];
                    Pt2D v = canonicalVerts[seg.V];
                    double dx = v.X - u.X;
                    double dy = v.Y - u.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len < 1e-4) continue;

                    Pt2D mid = new Pt2D(0.5 * (u.X + v.X), 0.5 * (u.Y + v.Y));
                    double nx = -dy / len; // Vector pháp tuyến bên trái của u -> v
                    double ny = dx / len;

                    // Chọn khoảng cách dò tìm an toàn không vượt qua bất kỳ cạnh nào khác
                    double minDistOther = MinDistanceToOtherSegments(mid, seg, elementarySegments, canonicalVerts);
                    double probeDist = Math.Min(0.002, 0.25 * Math.Min(len, minDistOther));
                    if (probeDist < 1e-6) probeDist = 1e-5;

                    Pt2D probeLeft = new Pt2D(mid.X + nx * probeDist, mid.Y + ny * probeDist);
                    Pt2D probeRight = new Pt2D(mid.X - nx * probeDist, mid.Y - ny * probeDist);

                    bool inLeft = IsPointInAnyRegion(probeLeft, floorRegions);
                    bool inRight = IsPointInAnyRegion(probeRight, floorRegions);

                    if (inLeft && !inRight)
                    {
                        boundaryHalfEdges.Add(new DirectedEdge(seg.U, seg.V, u, v));
                    }
                    else if (!inLeft && inRight)
                    {
                        boundaryHalfEdges.Add(new DirectedEdge(seg.V, seg.U, v, u));
                    }
                }

                if (boundaryHalfEdges.Count < 3)
                {
                    error = "Không tìm thấy đường bao ngoài sau khi hợp nhất.";
                    return null;
                }

                // Bước 6: Lần theo các cạnh có hướng để tạo các chu trình khép kín (Outer CCW & Hole CW)
                // và gộp các đoạn thẳng hàng (Collinear Simplification)
                List<List<Pt2D>> rawCycles = TraceClosedCycles(boundaryHalfEdges, canonicalVerts);
                var outerLoops = new List<List<Pt2D>>();
                var holeLoops = new List<List<Pt2D>>();

                foreach (List<Pt2D> cycle in rawCycles)
                {
                    List<Pt2D> simplified = SimplifyCollinearPolygon(cycle, CollinearDeviationFt, MinEdgeLengthFt);
                    if (simplified.Count < 3) continue;

                    double signedArea = ComputeSignedArea(simplified);
                    if (Math.Abs(signedArea) < 0.01) // Bỏ qua mảnh vụn diện tích < ~9 cm2
                        continue;

                    if (signedArea > 0)
                        outerLoops.Add(simplified);
                    else
                        holeLoops.Add(simplified);
                }

                if (outerLoops.Count == 0)
                {
                    error = "Không tạo được chu trình biên ngoài hợp lệ.";
                    return null;
                }

                // Bước 7: Gắn các lỗ mở (Hole CW) vào đúng tấm sàn bao ngoài (Outer CCW) chứa nó
                var islands = new List<List<CurveLoop>>();
                foreach (List<Pt2D> outer in outerLoops)
                {
                    CurveLoop outerCl = BuildLineCurveLoop(outer, targetZ);
                    if (outerCl != null)
                        islands.Add(new List<CurveLoop> { outerCl });
                }

                foreach (List<Pt2D> hole in holeLoops)
                {
                    CurveLoop holeCl = BuildLineCurveLoop(hole, targetZ);
                    if (holeCl == null) continue;

                    // Tìm outer loop có diện tích nhỏ nhất chứa lỗ mở này
                    int bestIslandIdx = -1;
                    double bestOuterArea = double.MaxValue;
                    for (int i = 0; i < outerLoops.Count && i < islands.Count; i++)
                    {
                        if (IsPointInPolygon2D(hole[0], outerLoops[i]))
                        {
                            double area = Math.Abs(ComputeSignedArea(outerLoops[i]));
                            if (area < bestOuterArea)
                            {
                                bestOuterArea = area;
                                bestIslandIdx = i;
                            }
                        }
                    }

                    if (bestIslandIdx >= 0)
                        islands[bestIslandIdx].Add(holeCl);
                }

                return islands.Count > 0 ? islands : null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static List<Pt2D> BuildCanonicalVertices(List<Pt2D> rawVertices, double snapTol)
        {
            var canonical = new List<Pt2D>();
            double snapTolSq = snapTol * snapTol;

            // Gom cụm các điểm gần nhau trong phạm vi snapTol
            var clusters = new List<List<Pt2D>>();
            foreach (Pt2D p in rawVertices)
            {
                int foundIdx = -1;
                double bestDistSq = snapTolSq;
                for (int i = 0; i < canonical.Count; i++)
                {
                    double dSq = p.DistanceSqTo(canonical[i]);
                    if (dSq <= bestDistSq)
                    {
                        bestDistSq = dSq;
                        foundIdx = i;
                    }
                }

                if (foundIdx >= 0)
                {
                    clusters[foundIdx].Add(p);
                    // Cập nhật tâm cụm
                    double avgX = clusters[foundIdx].Average(pt => pt.X);
                    double avgY = clusters[foundIdx].Average(pt => pt.Y);
                    canonical[foundIdx] = new Pt2D(avgX, avgY);
                }
                else
                {
                    clusters.Add(new List<Pt2D> { p });
                    canonical.Add(p);
                }
            }

            // Căn thẳng tọa độ X và Y giữa các đỉnh gần thẳng hàng theo phương trục đứng/ngang (tolerance 2 mm)
            double axisAlignTol = 2.0 / 304.8;
            for (int i = 0; i < canonical.Count; i++)
            {
                for (int j = i + 1; j < canonical.Count; j++)
                {
                    if (Math.Abs(canonical[i].X - canonical[j].X) <= axisAlignTol)
                        canonical[j] = new Pt2D(canonical[i].X, canonical[j].Y);
                    if (Math.Abs(canonical[i].Y - canonical[j].Y) <= axisAlignTol)
                        canonical[j] = new Pt2D(canonical[j].X, canonical[i].Y);
                }
            }

            return canonical;
        }

        private static int FindOrAddCanonicalVertex(Pt2D pt, List<Pt2D> canonical, double tol)
        {
            double tolSq = tol * tol;
            int bestIdx = -1;
            double bestDistSq = tolSq;
            for (int i = 0; i < canonical.Count; i++)
            {
                double dSq = pt.DistanceSqTo(canonical[i]);
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0) return bestIdx;
            canonical.Add(pt);
            return canonical.Count - 1;
        }

        private static List<Pt2D> SnapLoopToCanonical(List<Pt2D> loop, List<Pt2D> canonical, double snapTol)
        {
            var snapped = new List<Pt2D>();
            foreach (Pt2D p in loop)
            {
                int idx = FindOrAddCanonicalVertex(p, canonical, snapTol);
                Pt2D cp = canonical[idx];
                if (snapped.Count == 0 || snapped[snapped.Count - 1].DistanceTo(cp) > 1e-5)
                    snapped.Add(cp);
            }

            if (snapped.Count > 1 && snapped[snapped.Count - 1].DistanceTo(snapped[0]) <= 1e-5)
                snapped.RemoveAt(snapped.Count - 1);

            return snapped;
        }

        private static void ProjectTJunctionsToEdges(List<FloorPolyRegion> regions, List<Pt2D> canonical, double edgeSnapTol)
        {
            var allEdges = new List<Tuple<Pt2D, Pt2D>>();
            foreach (FloorPolyRegion reg in regions)
            {
                if (reg.Outer != null && reg.Outer.Count >= 3)
                {
                    for (int i = 0; i < reg.Outer.Count; i++)
                        allEdges.Add(Tuple.Create(reg.Outer[i], reg.Outer[(i + 1) % reg.Outer.Count]));
                }
                foreach (var hole in reg.Holes)
                {
                    if (hole != null && hole.Count >= 3)
                    {
                        for (int i = 0; i < hole.Count; i++)
                            allEdges.Add(Tuple.Create(hole[i], hole[(i + 1) % hole.Count]));
                    }
                }
            }

            for (int vIdx = 0; vIdx < canonical.Count; vIdx++)
            {
                Pt2D p = canonical[vIdx];
                double bestDist = edgeSnapTol;
                Pt2D bestProj = p;
                bool found = false;

                foreach (var edge in allEdges)
                {
                    Pt2D a = edge.Item1;
                    Pt2D b = edge.Item2;
                    if (p.DistanceTo(a) <= 1e-4 || p.DistanceTo(b) <= 1e-4)
                        continue;

                    double abx = b.X - a.X;
                    double aby = b.Y - a.Y;
                    double lenSq = abx * abx + aby * aby;
                    if (lenSq < 1e-8) continue;

                    double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
                    if (t <= 1e-4 || t >= 1.0 - 1e-4)
                        continue;

                    double projX = a.X + t * abx;
                    double projY = a.Y + t * aby;
                    double perpDist = Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));

                    if (perpDist <= bestDist)
                    {
                        bestDist = perpDist;
                        bestProj = new Pt2D(projX, projY);
                        found = true;
                    }
                }

                if (found)
                {
                    canonical[vIdx] = bestProj;
                }
            }
        }

        private static List<Pt2D> InjectTJunctionVertices(List<Pt2D> loop, List<Pt2D> canonical, double edgeSnapTol)
        {
            if (loop == null || loop.Count < 3) return loop;

            var result = new List<Pt2D>();
            int n = loop.Count;

            for (int i = 0; i < n; i++)
            {
                Pt2D a = loop[i];
                Pt2D b = loop[(i + 1) % n];
                result.Add(a);

                double abx = b.X - a.X;
                double aby = b.Y - a.Y;
                double lenSq = abx * abx + aby * aby;
                if (lenSq < 1e-8) continue;
                double len = Math.Sqrt(lenSq);

                var splitPoints = new List< Tuple<double, Pt2D> >();

                for (int vIdx = 0; vIdx < canonical.Count; vIdx++)
                {
                    Pt2D p = canonical[vIdx];
                    if (p.DistanceTo(a) <= 1e-4 || p.DistanceTo(b) <= 1e-4)
                        continue;

                    double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
                    if (t <= 1e-4 || t >= 1.0 - 1e-4)
                        continue;

                    double projX = a.X + t * abx;
                    double projY = a.Y + t * aby;
                    double perpDist = Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));

                    if (perpDist <= edgeSnapTol)
                    {
                        splitPoints.Add(Tuple.Create(t, p));
                    }
                }

                if (splitPoints.Count > 0)
                {
                    foreach (var sp in splitPoints.OrderBy(x => x.Item1))
                    {
                        if (result[result.Count - 1].DistanceTo(sp.Item2) > 1e-4 && sp.Item2.DistanceTo(b) > 1e-4)
                            result.Add(sp.Item2);
                    }
                }
            }

            return CleanPolygonVertices(result, 1e-4);
        }

        private static void AddLoopSegments(List<Pt2D> loop, List<Seg2D> target)
        {
            if (loop == null || loop.Count < 3) return;
            for (int i = 0; i < loop.Count; i++)
            {
                Pt2D a = loop[i];
                Pt2D b = loop[(i + 1) % loop.Count];
                if (a.DistanceTo(b) > 1e-4)
                    target.Add(new Seg2D { A = a, B = b });
            }
        }

        private static List<Seg2D> SubdivideSegmentsAtIntersections(
            List<Seg2D> segments,
            List<Pt2D> canonical,
            double vertexMergeTol)
        {
            int m = segments.Count;
            var splitPointsPerSeg = new List<Pt2D>[m];
            for (int i = 0; i < m; i++)
            {
                splitPointsPerSeg[i] = new List<Pt2D> { segments[i].A, segments[i].B };
            }

            // Tìm giao điểm giữa các cặp đoạn thẳng
            for (int i = 0; i < m; i++)
            {
                for (int j = i + 1; j < m; j++)
                {
                    if (TryIntersectSegments(segments[i].A, segments[i].B, segments[j].A, segments[j].B, out Pt2D inter))
                    {
                        int idx = FindOrAddCanonicalVertex(inter, canonical, vertexMergeTol);
                        Pt2D snappedInter = canonical[idx];
                        splitPointsPerSeg[i].Add(snappedInter);
                        splitPointsPerSeg[j].Add(snappedInter);
                    }
                }

                // Kiểm tra các đỉnh canonical nằm đúng trên đoạn thứ i
                Pt2D a = segments[i].A;
                Pt2D b = segments[i].B;
                double abx = b.X - a.X;
                double aby = b.Y - a.Y;
                double lenSq = abx * abx + aby * aby;
                if (lenSq > 1e-8)
                {
                    for (int v = 0; v < canonical.Count; v++)
                    {
                        Pt2D p = canonical[v];
                        double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
                        if (t > 1e-4 && t < 1.0 - 1e-4)
                        {
                            double px = a.X + t * abx;
                            double py = a.Y + t * aby;
                            if ((p.X - px) * (p.X - px) + (p.Y - py) * (p.Y - py) <= 1e-7)
                                splitPointsPerSeg[i].Add(p);
                        }
                    }
                }
            }

            // Tạo tập các đoạn cơ sở không trùng lặp
            var uniqueUndirected = new Dictionary<long, Seg2D>();
            for (int i = 0; i < m; i++)
            {
                Pt2D a = segments[i].A;
                Pt2D b = segments[i].B;
                double abx = b.X - a.X;
                double aby = b.Y - a.Y;
                double lenSq = abx * abx + aby * aby;
                if (lenSq < 1e-8) continue;

                var orderedPts = splitPointsPerSeg[i]
                    .OrderBy(p => ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq)
                    .ToList();

                for (int k = 0; k < orderedPts.Count - 1; k++)
                {
                    int u = FindOrAddCanonicalVertex(orderedPts[k], canonical, vertexMergeTol);
                    int v = FindOrAddCanonicalVertex(orderedPts[k + 1], canonical, vertexMergeTol);
                    if (u == v) continue;

                    int minV = Math.Min(u, v);
                    int maxV = Math.Max(u, v);
                    long key = ((long)minV << 32) | (uint)maxV;
                    if (!uniqueUndirected.ContainsKey(key))
                    {
                        uniqueUndirected[key] = new Seg2D
                        {
                            U = minV,
                            V = maxV,
                            A = canonical[minV],
                            B = canonical[maxV]
                        };
                    }
                }
            }

            return uniqueUndirected.Values.ToList();
        }

        private static double MinDistanceToOtherSegments(
            Pt2D mid,
            Seg2D current,
            List<Seg2D> allSegs,
            List<Pt2D> canonical)
        {
            double minDist = 1.0;
            foreach (Seg2D other in allSegs)
            {
                if (other.U == current.U && other.V == current.V) continue;
                double d = DistancePointToSegment(mid, canonical[other.U], canonical[other.V]);
                if (d > 1e-6 && d < minDist)
                    minDist = d;
            }
            return minDist;
        }

        private static bool IsPointInAnyRegion(Pt2D pt, List<FloorPolyRegion> regions)
        {
            foreach (FloorPolyRegion reg in regions)
            {
                if (!IsPointInPolygon2D(pt, reg.Outer))
                    continue;

                bool inHole = false;
                foreach (var hole in reg.Holes)
                {
                    if (IsPointInPolygon2D(pt, hole))
                    {
                        inHole = true;
                        break;
                    }
                }

                if (!inHole)
                    return true;
            }
            return false;
        }

        private static List<List<Pt2D>> TraceClosedCycles(List<DirectedEdge> edges, List<Pt2D> canonical)
        {
            var adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                int u = edges[i].U;
                if (!adj.TryGetValue(u, out var list))
                {
                    list = new List<int>();
                    adj[u] = list;
                }
                list.Add(i);
            }

            var used = new bool[edges.Count];
            var cycles = new List<List<Pt2D>>();

            for (int i = 0; i < edges.Count; i++)
            {
                if (used[i]) continue;

                var cycleVertIndices = new List<int>();
                int curEdgeIdx = i;

                while (!used[curEdgeIdx])
                {
                    used[curEdgeIdx] = true;
                    DirectedEdge e = edges[curEdgeIdx];
                    cycleVertIndices.Add(e.U);

                    int nextVertex = e.V;
                    if (!adj.TryGetValue(nextVertex, out var outList))
                        break;

                    // Chọn cạnh đi ra tiếp theo chưa dùng; nếu có nút thắt nơ (nhiều cạnh ra),
                    // chọn cạnh rẽ trái gắt nhất để giữ phần đặc bên trái
                    int bestNextEdge = -1;
                    double bestAngle = double.MaxValue;
                    double inAngle = Math.Atan2(e.U_Pt.Y - e.V_Pt.Y, e.U_Pt.X - e.V_Pt.X);

                    foreach (int candIdx in outList)
                    {
                        if (used[candIdx]) continue;
                        DirectedEdge cand = edges[candIdx];
                        double outAngle = Math.Atan2(cand.V_Pt.Y - cand.U_Pt.Y, cand.V_Pt.X - cand.U_Pt.X);
                        double ccwDelta = outAngle - inAngle;
                        while (ccwDelta <= 1e-7) ccwDelta += 2.0 * Math.PI;
                        if (ccwDelta < bestAngle)
                        {
                            bestAngle = ccwDelta;
                            bestNextEdge = candIdx;
                        }
                    }

                    if (bestNextEdge < 0)
                        break;

                    curEdgeIdx = bestNextEdge;
                }

                if (cycleVertIndices.Count >= 3)
                {
                    var pts = cycleVertIndices.Select(idx => canonical[idx]).ToList();
                    cycles.Add(pts);
                }
            }

            return cycles;
        }

        private static List<Pt2D> SimplifyCollinearPolygon(List<Pt2D> pts, double collinearTol, double minEdgeLen)
        {
            if (pts == null || pts.Count < 3) return new List<Pt2D>();

            var work = CleanPolygonVertices(pts, minEdgeLen);
            if (work.Count < 3) return new List<Pt2D>();

            // Lặp loại bỏ các đỉnh trung gian thẳng hàng hoặc gai nhọn (spikes)
            bool changed = true;
            while (changed && work.Count >= 3)
            {
                changed = false;
                for (int i = 0; i < work.Count; i++)
                {
                    Pt2D prev = work[(i - 1 + work.Count) % work.Count];
                    Pt2D curr = work[i];
                    Pt2D next = work[(i + 1) % work.Count];

                    if (prev.DistanceTo(next) <= minEdgeLen)
                    {
                        // Gai nhọn quay đầu 180 độ
                        work.RemoveAt(i);
                        changed = true;
                        break;
                    }

                    // Kiểm tra curr có nằm thẳng hàng giữa đoạn prev -> next không
                    double dx1 = curr.X - prev.X;
                    double dy1 = curr.Y - prev.Y;
                    double dx2 = next.X - curr.X;
                    double dy2 = next.Y - curr.Y;
                    double l1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                    double l2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

                    if (l1 < minEdgeLen || l2 < minEdgeLen)
                    {
                        work.RemoveAt(i);
                        changed = true;
                        break;
                    }

                    double dot = (dx1 * dx2 + dy1 * dy2) / (l1 * l2);
                    double perpDist = DistancePointToSegment(curr, prev, next);
                    if (dot > 0.98 && perpDist <= collinearTol)
                    {
                        work.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }

            return work;
        }

        private static CurveLoop BuildLineCurveLoop(List<Pt2D> poly, double z)
        {
            if (poly == null || poly.Count < 3) return null;
            try
            {
                var cl = new CurveLoop();
                for (int i = 0; i < poly.Count; i++)
                {
                    Pt2D a = poly[i];
                    Pt2D b = poly[(i + 1) % poly.Count];
                    XYZ p0 = new XYZ(a.X, a.Y, z);
                    XYZ p1 = new XYZ(b.X, b.Y, z);
                    if (p0.DistanceTo(p1) < MinEdgeLengthFt)
                        return null;
                    cl.Append(Line.CreateBound(p0, p1));
                }
                return !cl.IsOpen() ? cl : null;
            }
            catch
            {
                return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  4. Engine 2 (Fallback & Curved Floors): 3D Solid Boolean Union
        // ═════════════════════════════════════════════════════════════════

        private static List<List<CurveLoop>> TryMergeBySolidBoolean(
            List<FloorInputInfo> comp,
            double targetZ,
            out string error)
        {
            error = null;
            try
            {
                Solid unionSolid = null;

                foreach (FloorInputInfo f in comp)
                {
                    if (f.RawLoops == null || f.RawLoops.Count == 0) continue;

                    Solid slabSolid = null;
                    try
                    {
                        slabSolid = GeometryCreationUtilities.CreateExtrusionGeometry(
                            f.RawLoops,
                            XYZ.BasisZ,
                            1.0);
                    }
                    catch
                    {
                        // Dự phòng: lấy chỉ outer loop nếu lỗ mở gây xung đột chiều
                        try
                        {
                            slabSolid = GeometryCreationUtilities.CreateExtrusionGeometry(
                                new List<CurveLoop> { f.RawLoops[0] },
                                XYZ.BasisZ,
                                1.0);
                        }
                        catch { }
                    }

                    if (slabSolid == null || slabSolid.Volume <= 1e-6) continue;

                    if (unionSolid == null)
                    {
                        unionSolid = slabSolid;
                    }
                    else
                    {
                        unionSolid = BooleanOperationsUtils.ExecuteBooleanOperation(
                            unionSolid,
                            slabSolid,
                            BooleanOperationsType.Union);
                    }
                }

                if (unionSolid == null)
                {
                    error = "Không tạo được khối Solid hợp nhất.";
                    return null;
                }

                // Tách các khối liên thông nếu có nhiều đảo rời nhau
                IList<Solid> subSolids = SolidUtils.SplitVolumes(unionSolid);
                if (subSolids == null || subSolids.Count == 0)
                    subSolids = new List<Solid> { unionSolid };

                var islands = new List<List<CurveLoop>>();

                foreach (Solid s in subSolids)
                {
                    PlanarFace topFace = null;
                    double maxZ = double.MinValue;

                    foreach (Face face in s.Faces)
                    {
                        if (face is PlanarFace pf && pf.FaceNormal.Z > 0.999)
                        {
                            if (pf.Origin.Z > maxZ)
                            {
                                maxZ = pf.Origin.Z;
                                topFace = pf;
                            }
                        }
                    }

                    if (topFace == null) continue;

                    var islandLoops = new List<CurveLoop>();
                    foreach (CurveLoop cl in topFace.GetEdgesAsCurveLoops())
                    {
                        CurveLoop healed = HealCollinearCurveLoop(cl, targetZ);
                        if (healed != null)
                            islandLoops.Add(healed);
                    }

                    if (islandLoops.Count > 0)
                        islands.Add(islandLoops);
                }

                return islands.Count > 0 ? islands : null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static CurveLoop HealCollinearCurveLoop(CurveLoop rawLoop, double targetZ)
        {
            if (rawLoop == null) return null;
            var rawCurves = rawLoop.ToList();
            if (rawCurves.Count == 0) return null;

            // Nếu toàn bộ là Line thì dùng bộ làm sạch đa giác để xóa sạch điểm gãy thừa
            if (rawCurves.All(c => c is Line))
            {
                var pts = rawCurves.Select(c => new Pt2D(c.GetEndPoint(0).X, c.GetEndPoint(0).Y)).ToList();
                var simplified = SimplifyCollinearPolygon(pts, CollinearDeviationFt, MinEdgeLengthFt);
                return BuildLineCurveLoop(simplified, targetZ);
            }

            // Nếu có cả Arc và Line: gộp các đoạn Line liên tiếp cùng phương
            var projected = new List<Curve>();
            foreach (Curve c in rawCurves)
            {
                XYZ p0 = new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, targetZ);
                XYZ p1 = new XYZ(c.GetEndPoint(1).X, c.GetEndPoint(1).Y, targetZ);
                if (c is Line)
                {
                    if (p0.DistanceTo(p1) >= 1e-4)
                        projected.Add(Line.CreateBound(p0, p1));
                }
                else if (c is Arc arc)
                {
                    XYZ mid = arc.Evaluate(0.5, true);
                    projected.Add(Arc.Create(p0, p1, new XYZ(mid.X, mid.Y, targetZ)));
                }
            }

            bool merged = true;
            while (merged && projected.Count > 1)
            {
                merged = false;
                for (int i = 0; i < projected.Count; i++)
                {
                    int nextIdx = (i + 1) % projected.Count;
                    if (projected[i] is Line l1 && projected[nextIdx] is Line l2)
                    {
                        if (l1.Direction.DotProduct(l2.Direction) > 0.9998)
                        {
                            XYZ start = l1.GetEndPoint(0);
                            XYZ end = l2.GetEndPoint(1);
                            if (start.DistanceTo(end) >= MinEdgeLengthFt)
                            {
                                projected[i] = Line.CreateBound(start, end);
                                projected.RemoveAt(nextIdx);
                                merged = true;
                                break;
                            }
                        }
                    }
                }
            }

            try
            {
                var loop = new CurveLoop();
                foreach (Curve c in projected)
                    loop.Append(c);
                return !loop.IsOpen() ? loop : null;
            }
            catch
            {
                return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  5. 2D Computational Geometry Primitives
        // ═════════════════════════════════════════════════════════════════

        private static List<Pt2D> CleanPolygonVertices(List<Pt2D> pts, double minDist)
        {
            var cleaned = new List<Pt2D>();
            if (pts == null || pts.Count == 0) return cleaned;

            foreach (Pt2D p in pts)
            {
                if (cleaned.Count == 0 || cleaned[cleaned.Count - 1].DistanceTo(p) > minDist)
                    cleaned.Add(p);
            }

            while (cleaned.Count > 1 && cleaned[cleaned.Count - 1].DistanceTo(cleaned[0]) <= minDist)
            {
                cleaned.RemoveAt(cleaned.Count - 1);
            }

            return cleaned;
        }

        private static double ComputeSignedArea(List<Pt2D> poly)
        {
            if (poly == null || poly.Count < 3) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < poly.Count; i++)
            {
                Pt2D a = poly[i];
                Pt2D b = poly[(i + 1) % poly.Count];
                sum += a.X * b.Y - b.X * a.Y;
            }
            return 0.5 * sum;
        }

        private static bool IsPointInPolygon2D(Pt2D pt, List<Pt2D> poly)
        {
            if (poly == null || poly.Count < 3) return false;
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Pt2D pi = poly[i];
                Pt2D pj = poly[j];

                if (((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                    (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static double DistancePointToSegment(Pt2D p, Pt2D a, Pt2D b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-12) return p.DistanceTo(a);

            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            if (t < 0.0) t = 0.0;
            else if (t > 1.0) t = 1.0;

            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        private static double SegmentDistance2D(Pt2D a0, Pt2D a1, Pt2D b0, Pt2D b1)
        {
            if (TryIntersectSegments(a0, a1, b0, b1, out _))
                return 0.0;

            double d1 = DistancePointToSegment(a0, b0, b1);
            double d2 = DistancePointToSegment(a1, b0, b1);
            double d3 = DistancePointToSegment(b0, a0, a1);
            double d4 = DistancePointToSegment(b1, a0, a1);
            return Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
        }

        private static bool TryIntersectSegments(Pt2D a0, Pt2D a1, Pt2D b0, Pt2D b1, out Pt2D intersection)
        {
            intersection = default;
            double rX = a1.X - a0.X;
            double rY = a1.Y - a0.Y;
            double sX = b1.X - b0.X;
            double sY = b1.Y - b0.Y;

            double denom = rX * sY - rY * sX;
            if (Math.Abs(denom) < 1e-10)
                return false;

            double qpX = b0.X - a0.X;
            double qpY = b0.Y - a0.Y;

            double t = (qpX * sY - qpY * sX) / denom;
            double u = (qpX * rY - qpY * rX) / denom;

            if (t >= -1e-5 && t <= 1.0 + 1e-5 && u >= -1e-5 && u <= 1.0 + 1e-5)
            {
                intersection = new Pt2D(a0.X + t * rX, a0.Y + t * rY);
                return true;
            }
            return false;
        }

        private static BoundingBoxUV ComputeBounds2D(List<CurveLoop> loops)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            bool any = false;

            foreach (CurveLoop cl in loops)
            {
                foreach (Curve c in cl)
                {
                    foreach (XYZ p in c.Tessellate())
                    {
                        if (p.X < minX) minX = p.X;
                        if (p.Y < minY) minY = p.Y;
                        if (p.X > maxX) maxX = p.X;
                        if (p.Y > maxY) maxY = p.Y;
                        any = true;
                    }
                }
            }

            return any ? new BoundingBoxUV(minX, minY, maxX, maxY) : null;
        }

        // ═════════════════════════════════════════════════════════════════
        //  6. Report UI & Helpers
        // ═════════════════════════════════════════════════════════════════

        private static void ShowReport(
            int totalScanned,
            List<MergeGroupResult> results,
            List<string> skippedReasons,
            MergeFloorScope scope)
        {
            int totalMergedSource = results.Sum(r => r.MergedSourceCount);
            int totalCreated = results.Sum(r => r.MergedFloorCount);
            int totalSingle = results.Sum(r => r.SkippedSingleCount) + results.Count(r => r.SourceFloorCount == 1);
            int totalErrors = results.Sum(r => r.Errors.Count);

            string scopeTitle = (scope == MergeFloorScope.ActiveView) ? "Active View" : "Selection";
            string scanLabel = (scope == MergeFloorScope.ActiveView)
                ? $"• Tổng số sàn quét được trong View: {totalScanned}"
                : $"• Số mảnh sàn đã chọn: {totalScanned}";

            var main = new StringBuilder();
            main.AppendLine(scanLabel);
            main.AppendLine($"• Số mảnh sàn đã gộp thành công: {totalMergedSource}  ➔  {totalCreated} sàn duy nhất");
            if (totalSingle > 0)
                main.AppendLine($"• Sàn đứng riêng lẻ (không chạm sàn cùng loại): {totalSingle}");
            if (skippedReasons.Count > 0)
                main.AppendLine($"• Sàn bỏ qua (dốc / lỗi biên): {skippedReasons.Count}");
            if (totalErrors > 0)
                main.AppendLine($"• Nhóm gặp lỗi khi tạo: {totalErrors}");

            var details = new StringBuilder();
            foreach (MergeGroupResult r in results)
            {
                if (r.MergedFloorCount > 0)
                {
                    string ids = string.Join(", ", r.CreatedIds.Select(id => $"#{GetIdValue(id)}"));
                    details.AppendLine($"✓ [{r.TypeName}]: Đã gộp {r.MergedSourceCount} mảnh ➔ {r.MergedFloorCount} sàn ({ids})");
                }
                else if (!string.IsNullOrEmpty(r.Note))
                {
                    details.AppendLine($"• [{r.TypeName}]: {r.Note}");
                }

                foreach (string err in r.Errors)
                {
                    details.AppendLine($"⚠ [{r.TypeName}]: {err}");
                }
            }

            foreach (string s in skippedReasons.Take(10))
                details.AppendLine($"• Bỏ qua {s}");

            var dlg = new TaskDialog($"Merge Floor ({scopeTitle})")
            {
                MainIcon = totalCreated > 0 ? TaskDialogIcon.TaskDialogIconInformation : TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = totalCreated > 0
                    ? $"Đã gộp {totalMergedSource} mảnh sàn thành {totalCreated} sàn duy nhất."
                    : "Không có mảnh sàn nào được gộp."
            };

            if (results.Count <= 4)
            {
                dlg.MainContent = main.ToString().TrimEnd() + "\n\n" + details.ToString().TrimEnd();
            }
            else
            {
                dlg.MainContent = main.ToString().TrimEnd();
                dlg.ExpandedContent = details.ToString().TrimEnd();
            }

            dlg.Show();
        }

        internal static long GetIdValue(ElementId id)
        {
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }

        // ═════════════════════════════════════════════════════════════════
        //  Internal Data Structures
        // ═════════════════════════════════════════════════════════════════

        private struct Pt2D
        {
            public readonly double X;
            public readonly double Y;

            public Pt2D(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double DistanceTo(Pt2D other)
            {
                double dx = X - other.X;
                double dy = Y - other.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            public double DistanceSqTo(Pt2D other)
            {
                double dx = X - other.X;
                double dy = Y - other.Y;
                return dx * dx + dy * dy;
            }
        }

        private sealed class Seg2D
        {
            public int U;
            public int V;
            public Pt2D A;
            public Pt2D B;
        }

        private sealed class DirectedEdge
        {
            public readonly int U;
            public readonly int V;
            public readonly Pt2D U_Pt;
            public readonly Pt2D V_Pt;

            public DirectedEdge(int u, int v, Pt2D uPt, Pt2D vPt)
            {
                U = u;
                V = v;
                U_Pt = uPt;
                V_Pt = vPt;
            }
        }

        private sealed class FloorPolyRegion
        {
            public List<Pt2D> Outer { get; set; }
            public List<List<Pt2D>> Holes { get; } = new List<List<Pt2D>>();
        }

        private sealed class JoinedElementRecord
        {
            public ElementId JoinedId { get; set; }
            public bool FloorWasCutting { get; set; }
        }

        private sealed class FloorInputInfo
        {
            public ElementId FloorId { get; set; }
            public ElementId FloorTypeId { get; set; }
            public string TypeName { get; set; }
            public ElementId LevelId { get; set; }
            public double LevelElevation { get; set; }
            public double HeightOffset { get; set; }
            public double TopZ { get; set; }
            public int IsStructural { get; set; }
            public string Comments { get; set; }
            public string Mark { get; set; }
            public List<CurveLoop> RawLoops { get; set; }
            public List<List<Pt2D>> PolyLoops { get; set; }
            public bool HasCurvedEdges { get; set; }
            public double AreaApprox { get; set; }
            public BoundingBoxUV Bounds2D { get; set; }
            public List<JoinedElementRecord> JoinedElements { get; set; }
        }

        private sealed class MergeGroupResult
        {
            public string TypeName { get; set; }
            public int SourceFloorCount { get; set; }
            public int MergedSourceCount { get; set; }
            public int MergedFloorCount { get; set; }
            public int SkippedSingleCount { get; set; }
            public string Note { get; set; }
            public List<ElementId> CreatedIds { get; } = new List<ElementId>();
            public List<string> Errors { get; } = new List<string>();
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Selection Filter & Failure Preprocessor
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Chỉ cho phép quét chọn Sàn (OST_Floors hoặc Sàn móng OST_StructuralFoundation là đối tượng Floor).</summary>
    internal sealed class FloorSelectionFilter : ISelectionFilter
    {
        private static readonly ElementId FloorCatId = new ElementId(BuiltInCategory.OST_Floors);
        private static readonly ElementId FoundationCatId = new ElementId(BuiltInCategory.OST_StructuralFoundation);

        public bool AllowElement(Element elem)
        {
            if (elem is Floor floor && floor.Category != null)
            {
                ElementId catId = floor.Category.Id;
                return catId == FloorCatId || catId == FoundationCatId;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    /// <summary>Tự động bỏ qua các cảnh báo (Warning) khi thay thế và gộp sàn.</summary>
    internal sealed class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (FailureMessageAccessor f in failuresAccessor.GetFailureMessages())
            {
                if (f.GetSeverity() == FailureSeverity.Warning)
                    failuresAccessor.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }
}
