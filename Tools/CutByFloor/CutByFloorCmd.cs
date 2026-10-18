using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JNNTool.Tools.CutByFloor.Engine;
using JNNTool.Tools.CutByFloor.Models;

namespace JNNTool.Tools.CutByFloor
{
    /// <summary>
    /// Chạy 1-click tự động rà soát và khoét toàn bộ sàn to có chứa sàn nhỏ trong View hiện hành.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CutByFloorActiveViewCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CutByFloorCmd.RunCut(commandData, CutFloorScope.ActiveView, ref message);
        }
    }

    /// <summary>
    /// Quét chọn sàn thủ công (hoặc quét chọn trước) để cắt/khoét sàn to theo sàn nhỏ.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CutByFloorSelectCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CutByFloorCmd.RunCut(commandData, CutFloorScope.Selection, ref message);
        }
    }

    /// <summary>
    /// Cut Floor By Floor (Cắt sàn to theo sàn nhỏ):
    /// - Quét chọn trước: Sàn to nhất tự động là Sàn Bị Cắt, các sàn nhỏ lọt trong/giao cắt là Sàn Cắt.
    /// - Chọn theo bước: Bước 1 chọn Sàn To -> Bước 2 chọn các Sàn Nhỏ.
    /// - Hỗ trợ Active View tự động tìm tất cả sàn to chứa sàn nhỏ trong view hiện hành.
    /// - Giữ nguyên Sàn Nhỏ sau khi cắt (cho sàn vệ sinh/ban công hạ cốt) và bảo toàn 100% tham số, Join Geometry của sàn to.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CutByFloorCmd : IExternalCommand
    {
        private const double ElevationTolFt = 5.0 / 304.8; // 5 mm
        private const double MinEdgeLengthFt = 3.0 / 304.8; // 3 mm

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                return Result.Cancelled;

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
                return Result.Cancelled;

            Document doc = uidoc.Document;

            // Kiểm tra xem người dùng đã quét chọn sẵn sàn chưa
            var filter = new FloorSelectionFilter();
            List<Floor> preSelected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(filter.AllowElement)
                .OfType<Floor>()
                .ToList();

            // Chạy trực tiếp chế độ Quét chọn (Selection):
            // - Nếu đã chọn sẵn: tự nhận diện sàn to nhất và các sàn nhỏ để cắt ngay lập tức
            // - Nếu chưa chọn: hướng dẫn chọn sàn to (Bước 1) và quét chọn các sàn nhỏ (Bước 2)
            return RunCut(commandData, CutFloorScope.Selection, ref message, preSelected);
        }

        public static Result RunCut(
            ExternalCommandData commandData,
            CutFloorScope scope,
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
                var pairs = new List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)>();

                if (scope == CutFloorScope.ActiveView)
                {
                    pairs = ResolvePairsInActiveView(doc, out string viewErr);
                    if (pairs == null)
                    {
                        TaskDialog.Show("Cut Floor (Active View)", viewErr ?? "View hiện hành không hỗ trợ.");
                        return Result.Cancelled;
                    }

                    if (pairs.Count == 0)
                    {
                        TaskDialog.Show("Cut Floor (Active View)",
                            "Đã quét các sàn trong View hiện hành nhưng không tìm thấy sàn to nào chứa sàn nhỏ bên trong / giao cắt.");
                        return Result.Cancelled;
                    }
                }
                else
                {
                    pairs = ResolvePairsBySelection(uidoc, doc, preSelected, out string selErr);
                    if (pairs == null || pairs.Count == 0)
                    {
                        if (!string.IsNullOrEmpty(selErr))
                            TaskDialog.Show("Cut Floor", selErr);
                        return Result.Cancelled;
                    }
                }

                // Thực hiện cắt trong Transaction
                var createdFloorIds = new List<ElementId>();
                var cutResults = new List<CutFloorResult>();

                string txName = scope == CutFloorScope.ActiveView
                    ? "JNN - Cut Floor (Active View)"
                    : "JNN - Cut Floor (Selection)";

                using (Transaction tx = new Transaction(doc, txName))
                {
                    tx.Start();

                    FailureHandlingOptions fho = tx.GetFailureHandlingOptions();
                    fho.SetFailuresPreprocessor(new WarningSwallower());
                    tx.SetFailureHandlingOptions(fho);

                    foreach (var pair in pairs)
                    {
                        var res = ProcessCut(doc, pair.Target, pair.Cutters, createdFloorIds);
                        cutResults.Add(res);
                    }

                    tx.Commit();
                }

                // Highlight các sàn to mới vừa được cắt trên màn hình
                if (createdFloorIds.Count > 0)
                {
                    try { uidoc.Selection.SetElementIds(createdFloorIds); } catch { }
                }

                ShowReport(cutResults, scope);
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                JNNTool.Core.Logging.Logger.Error("Lỗi Cut Floor", ex);
                return Result.Failed;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  Selection & Pairing Logic
        // ═════════════════════════════════════════════════════════════════

        private static List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)> ResolvePairsBySelection(
            UIDocument uidoc,
            Document doc,
            List<Floor> preSelected,
            out string error)
        {
            error = null;
            var filter = new FloorSelectionFilter();

            List<Floor> floors = preSelected;
            if (floors == null || floors.Count < 2)
            {
                // Cho người dùng chọn theo 2 bước:
                // Bước 1: Chọn sàn to cần cắt
                Floor targetFloor = null;
                if (floors != null && floors.Count == 1)
                {
                    targetFloor = floors[0];
                }
                else
                {
                    try
                    {
                        Reference rTarget = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            filter,
                            "Bước 1/2: Chọn SÀN TO cần cắt (Target Floor)");
                        if (rTarget != null)
                            targetFloor = doc.GetElement(rTarget.ElementId) as Floor;
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return null;
                    }
                }

                if (targetFloor == null)
                {
                    error = "Chưa chọn sàn to hợp lệ.";
                    return null;
                }

                // Bước 2: Quét chọn các sàn nhỏ dùng làm dao cắt
                IList<Reference> rCutters;
                try
                {
                    rCutters = uidoc.Selection.PickObjects(
                        ObjectType.Element,
                        filter,
                        "Bước 2/2: Quét chọn các SÀN NHỎ dùng làm dao cắt rồi nhấn Finish trên thanh Options Bar");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return null;
                }

                if (rCutters == null || rCutters.Count == 0)
                {
                    error = "Chưa chọn sàn nhỏ để cắt.";
                    return null;
                }

                var cutterFloors = rCutters
                    .Select(r => doc.GetElement(r.ElementId))
                    .OfType<Floor>()
                    .Where(f => GetIdValue(f.Id) != GetIdValue(targetFloor.Id))
                    .GroupBy(f => GetIdValue(f.Id))
                    .Select(g => g.First())
                    .ToList();

                if (cutterFloors.Count == 0)
                {
                    error = "Sàn cắt không được trùng với sàn bị cắt.";
                    return null;
                }

                if (!TryExtractFloorInfo(doc, targetFloor, out CutFloorInputInfo targetInfo, out string tErr))
                {
                    error = "Không đọc được hình học sàn to: " + tErr;
                    return null;
                }

                var cutterInfos = new List<CutFloorInputInfo>();
                foreach (var cf in cutterFloors)
                {
                    if (TryExtractFloorInfo(doc, cf, out CutFloorInputInfo cInfo, out _))
                        cutterInfos.Add(cInfo);
                }

                if (cutterInfos.Count == 0)
                {
                    error = "Không đọc được hình học các sàn nhỏ.";
                    return null;
                }

                return new List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)>
                {
                    (targetInfo, cutterInfos)
                };
            }

            // Người dùng đã quét chọn sẵn >= 2 sàn: tự động xác định Sàn to nhất là Target, sàn nhỏ hơn là Cutters
            var extracted = new List<CutFloorInputInfo>();
            foreach (var f in floors)
            {
                if (TryExtractFloorInfo(doc, f, out CutFloorInputInfo info, out _))
                    extracted.Add(info);
            }

            if (extracted.Count < 2)
            {
                error = "Không đủ sàn phẳng hợp lệ để cắt.";
                return null;
            }

            // Sàn có diện tích lớn nhất là Target
            extracted = extracted.OrderByDescending(x => x.AreaApprox).ToList();
            var primaryTarget = extracted[0];
            var primaryCutters = extracted.Skip(1).ToList();

            return new List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)>
            {
                (primaryTarget, primaryCutters)
            };
        }

        private static List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)> ResolvePairsInActiveView(
            Document doc,
            out string error)
        {
            error = null;
            View activeView = doc.ActiveView;
            if (activeView == null || (!(activeView is ViewPlan) && !(activeView is View3D)))
            {
                error = "Chức năng Active View chỉ áp dụng trên Mặt bằng hoặc 3D View.";
                return null;
            }

            var filter = new FloorSelectionFilter();
            var allFloors = new FilteredElementCollector(doc, activeView.Id)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType()
                .Cast<Floor>()
                .Where(f => !f.IsHidden(activeView) && filter.AllowElement(f))
                .ToList();

            var extracted = new List<CutFloorInputInfo>();
            foreach (var f in allFloors)
            {
                if (TryExtractFloorInfo(doc, f, out CutFloorInputInfo info, out _))
                    extracted.Add(info);
            }

            if (extracted.Count < 2)
            {
                error = "Trong view không đủ 2 sàn để thực hiện cắt.";
                return null;
            }

            // Gom nhóm theo Level / Cao độ Z (chỉ cắt sàn cùng tầng hoặc cao độ gần nhau)
            var groups = extracted.GroupBy(f => Math.Round(f.TopZ * 304.8 / 50.0)).ToList();
            var pairs = new List<(CutFloorInputInfo Target, List<CutFloorInputInfo> Cutters)>();

            foreach (var grp in groups)
            {
                var list = grp.OrderByDescending(f => f.AreaApprox).ToList();
                if (list.Count < 2) continue;

                var usedAsCutter = new HashSet<long>();

                for (int i = 0; i < list.Count; i++)
                {
                    var target = list[i];
                    if (usedAsCutter.Contains(GetIdValue(target.FloorId))) continue;

                    var matchingCutters = new List<CutFloorInputInfo>();
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var candidate = list[j];
                        if (usedAsCutter.Contains(GetIdValue(candidate.FloorId))) continue;

                        // Kiểm tra candidate có nằm trong hoặc giao cắt với target không
                        if (FloorDifferenceEngine.IsPointInRegion(candidate.PolyRegion.Outer[0], target.PolyRegion) ||
                            candidate.PolyRegion.Outer.Any(p => FloorDifferenceEngine.IsPointInRegion(p, target.PolyRegion)))
                        {
                            matchingCutters.Add(candidate);
                            usedAsCutter.Add(GetIdValue(candidate.FloorId));
                        }
                    }

                    if (matchingCutters.Count > 0)
                    {
                        pairs.Add((target, matchingCutters));
                    }
                }
            }

            return pairs;
        }

        // ═════════════════════════════════════════════════════════════════
        //  Core Execution & Floor Replacement
        // ═════════════════════════════════════════════════════════════════

        private static CutFloorResult ProcessCut(
            Document doc,
            CutFloorInputInfo target,
            List<CutFloorInputInfo> cutters,
            List<ElementId> createdFloorIds)
        {
            var result = new CutFloorResult
            {
                TargetFloorId = target.FloorId,
                TargetTypeName = target.TypeName,
                OriginalArea = target.AreaApprox,
                CuttersCount = cutters.Count
            };

            try
            {
                List<List<CurveLoop>> islandsLoops = null;

                bool anyCurved = target.HasCurvedEdges || cutters.Any(c => c.HasCurvedEdges);
                if (!anyCurved)
                {
                    // Chạy Engine 1: Exact 2D Planar Difference
                    var islands = FloorDifferenceEngine.Difference(
                        target.PolyRegion,
                        cutters.Select(c => c.PolyRegion).ToList());

                    if (islands != null && islands.Count > 0)
                    {
                        islandsLoops = new List<List<CurveLoop>>();
                        foreach (var island in islands)
                        {
                            var islandList = new List<CurveLoop>();
                            CurveLoop outerCl = BuildLineCurveLoop(island.OuterLoop, target.TopZ);
                            if (outerCl != null)
                            {
                                islandList.Add(outerCl);
                                foreach (var hole in island.HoleLoops)
                                {
                                    CurveLoop holeCl = BuildLineCurveLoop(hole, target.TopZ);
                                    if (holeCl != null)
                                        islandList.Add(holeCl);
                                }
                                islandsLoops.Add(islandList);
                            }
                        }
                    }
                }

                // Fallback Engine 2 (Solid Boolean) nếu có cạnh cong hoặc Engine 1 không xuất được loop
                if (islandsLoops == null || islandsLoops.Count == 0)
                {
                    islandsLoops = TryCutBySolidBoolean(target, cutters, target.TopZ);
                }

                if (islandsLoops == null || islandsLoops.Count == 0)
                {
                    result.Note = "Không thể tính toán hình học cắt.";
                    return result;
                }

                // Thực hiện thay thế sàn to trong SubTransaction
                using (SubTransaction subTx = new SubTransaction(doc))
                {
                    subTx.Start();

                    // Xóa sàn to cũ
                    doc.Delete(target.FloorId);

                    var newlyCreated = new List<Floor>();
                    int totalHoles = 0;

                    foreach (var islandLoop in islandsLoops)
                    {
                        Floor newFloor = Floor.Create(doc, islandLoop, target.FloorTypeId, target.LevelId);
                        if (newFloor == null)
                            throw new InvalidOperationException("Revit Floor.Create trả về null.");

                        // Khôi phục cao độ Offset
                        double newOffset = target.TopZ - target.LevelElevation;
                        var pOffset = newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                        if (pOffset != null && !pOffset.IsReadOnly) pOffset.Set(newOffset);

                        var pStruct = newFloor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                        if (pStruct != null && !pStruct.IsReadOnly) pStruct.Set(target.IsStructural);

                        if (!string.IsNullOrEmpty(target.Comments))
                        {
                            var pCmt = newFloor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (pCmt != null && !pCmt.IsReadOnly) pCmt.Set(target.Comments);
                        }

                        if (!string.IsNullOrEmpty(target.Mark))
                        {
                            var pMark = newFloor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                            if (pMark != null && !pMark.IsReadOnly) pMark.Set(target.Mark);
                        }

                        newlyCreated.Add(newFloor);
                        if (islandLoop.Count > 1)
                            totalHoles += (islandLoop.Count - 1);
                    }

                    doc.Regenerate();

                    // Khôi phục Join Geometry với các cấu kiện xung quanh
                    foreach (Floor nf in newlyCreated)
                    {
                        foreach (var jr in target.JoinedElements)
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
                                    bool isCutting = JoinGeometryUtils.IsCuttingElementInJoin(doc, nf, other);
                                    if (isCutting != jr.FloorWasCutting)
                                        JoinGeometryUtils.SwitchJoinOrder(doc, nf, other);
                                }
                            }
                            catch { }
                        }
                    }

                    subTx.Commit();

                    foreach (Floor nf in newlyCreated)
                    {
                        createdFloorIds.Add(nf.Id);
                        result.CreatedFloorIds.Add(nf.Id);
                    }

                    result.Success = true;
                    result.IslandsCreatedCount = newlyCreated.Count;
                    result.HolesCreatedCount = totalHoles;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Note = ex.Message;
            }

            return result;
        }

        private static List<List<CurveLoop>> TryCutBySolidBoolean(
            CutFloorInputInfo target,
            List<CutFloorInputInfo> cutters,
            double targetZ)
        {
            try
            {
                Solid targetSolid = GeometryCreationUtilities.CreateExtrusionGeometry(target.RawLoops, XYZ.BasisZ, 1.0);
                if (targetSolid == null) return null;

                Solid cutSolid = targetSolid;
                foreach (var c in cutters)
                {
                    if (c.RawLoops == null || c.RawLoops.Count == 0) continue;
                    try
                    {
                        Solid cutterSolid = GeometryCreationUtilities.CreateExtrusionGeometry(c.RawLoops, XYZ.BasisZ, 1.0);
                        if (cutterSolid != null && cutterSolid.Volume > 1e-6)
                        {
                            cutSolid = BooleanOperationsUtils.ExecuteBooleanOperation(
                                cutSolid,
                                cutterSolid,
                                BooleanOperationsType.Difference);
                        }
                    }
                    catch { }
                }

                if (cutSolid == null || cutSolid.Volume <= 1e-6) return null;

                IList<Solid> subSolids = SolidUtils.SplitVolumes(cutSolid);
                if (subSolids == null || subSolids.Count == 0) subSolids = new List<Solid> { cutSolid };

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

                    if (topFace != null)
                    {
                        var faceLoops = new List<CurveLoop>();
                        foreach (CurveLoop cl in topFace.GetEdgesAsCurveLoops())
                        {
                            var projLoop = new CurveLoop();
                            foreach (Curve c in cl)
                            {
                                XYZ p0 = new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, targetZ);
                                XYZ p1 = new XYZ(c.GetEndPoint(1).X, c.GetEndPoint(1).Y, targetZ);
                                if (p0.DistanceTo(p1) > MinEdgeLengthFt)
                                    projLoop.Append(Line.CreateBound(p0, p1));
                            }
                            if (!projLoop.IsOpen()) faceLoops.Add(projLoop);
                        }
                        if (faceLoops.Count > 0) islands.Add(faceLoops);
                    }
                }

                return islands.Count > 0 ? islands : null;
            }
            catch
            {
                return null;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  Geometry Extraction Helpers
        // ═════════════════════════════════════════════════════════════════

        private static bool TryExtractFloorInfo(Document doc, Floor floor, out CutFloorInputInfo info, out string skipReason)
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

            double heightOffset = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;
            double topZ = level.Elevation + heightOffset;

            bool hasCurved = false;
            List<CurveLoop> rawLoops = ExtractSketchLoops(doc, floor, topZ, out hasCurved);

            if (rawLoops.Count == 0)
            {
                rawLoops = ExtractTopFaceLoops(floor, topZ, out hasCurved);
                if (rawLoops.Count == 0)
                {
                    skipReason = "Không đọc được biên dạng sàn.";
                    return false;
                }
            }

            var polyRegion = new FloorPolyRegion();
            for (int i = 0; i < rawLoops.Count; i++)
            {
                var pts = new List<Pt2D>();
                foreach (Curve c in rawLoops[i])
                    pts.Add(new Pt2D(c.GetEndPoint(0).X, c.GetEndPoint(0).Y));

                if (i == 0)
                    polyRegion.Outer = pts;
                else
                    polyRegion.Holes.Add(pts);
            }

            var joinedItems = new List<JoinedElementRecord>();
            try
            {
                var joinedIds = JoinGeometryUtils.GetJoinedElements(doc, floor);
                if (joinedIds != null)
                {
                    foreach (var jId in joinedIds)
                    {
                        var jElem = doc.GetElement(jId);
                        if (jElem != null)
                        {
                            bool isCutting = false;
                            try { isCutting = JoinGeometryUtils.IsCuttingElementInJoin(doc, floor, jElem); } catch { }
                            joinedItems.Add(new JoinedElementRecord { JoinedId = jId, FloorWasCutting = isCutting });
                        }
                    }
                }
            }
            catch { }

            double area = Math.Abs(FloorDifferenceEngine.ComputeSignedArea(polyRegion.Outer));
            var pArea = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
            if (pArea != null && pArea.HasValue) area = pArea.AsDouble();

            info = new CutFloorInputInfo
            {
                FloorId = floor.Id,
                FloorTypeId = typeId,
                TypeName = floorType.Name,
                LevelId = levelId,
                LevelElevation = level.Elevation,
                HeightOffset = heightOffset,
                TopZ = topZ,
                IsStructural = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL)?.AsInteger() ?? 1,
                Comments = floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString(),
                Mark = floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
                RawLoops = rawLoops,
                PolyRegion = polyRegion,
                HasCurvedEdges = hasCurved,
                AreaApprox = area,
                JoinedElements = joinedItems
            };

            return true;
        }

        private static List<CurveLoop> ExtractSketchLoops(Document doc, Floor floor, double targetZ, out bool hasCurved)
        {
            hasCurved = false;
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
                    var depSketches = floor.GetDependentElements(new ElementClassFilter(typeof(Sketch)));
                    if (depSketches != null && depSketches.Count > 0)
                        sketch = doc.GetElement(depSketches[0]) as Sketch;
                }

                if (sketch?.Profile != null && sketch.Profile.Size > 0)
                {
                    foreach (CurveArray ca in sketch.Profile)
                    {
                        if (ca == null || ca.Size == 0) continue;
                        var cl = new CurveLoop();
                        foreach (Curve c in ca)
                        {
                            if (!(c is Line)) hasCurved = true;
                            XYZ p0 = new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, targetZ);
                            XYZ p1 = new XYZ(c.GetEndPoint(1).X, c.GetEndPoint(1).Y, targetZ);
                            if (p0.DistanceTo(p1) > MinEdgeLengthFt)
                                cl.Append(Line.CreateBound(p0, p1));
                        }
                        if (!cl.IsOpen()) result.Add(cl);
                    }
                }
            }
            catch { }
            return result;
        }

        private static List<CurveLoop> ExtractTopFaceLoops(Floor floor, double targetZ, out bool hasCurved)
        {
            hasCurved = false;
            var result = new List<CurveLoop>();
            try
            {
                var opt = new Options { DetailLevel = ViewDetailLevel.Fine };
                var geom = floor.get_Geometry(opt);
                if (geom == null) return result;

                foreach (var obj in geom)
                {
                    if (obj is Solid s && s.Volume > 1e-6)
                    {
                        PlanarFace topFace = null;
                        double maxZ = double.MinValue;
                        foreach (Face f in s.Faces)
                        {
                            if (f is PlanarFace pf && pf.FaceNormal.Z > 0.999)
                            {
                                if (pf.Origin.Z > maxZ)
                                {
                                    maxZ = pf.Origin.Z;
                                    topFace = pf;
                                }
                            }
                        }

                        if (topFace != null)
                        {
                            foreach (CurveLoop cl in topFace.GetEdgesAsCurveLoops())
                            {
                                var proj = new CurveLoop();
                                foreach (Curve c in cl)
                                {
                                    if (!(c is Line)) hasCurved = true;
                                    XYZ p0 = new XYZ(c.GetEndPoint(0).X, c.GetEndPoint(0).Y, targetZ);
                                    XYZ p1 = new XYZ(c.GetEndPoint(1).X, c.GetEndPoint(1).Y, targetZ);
                                    if (p0.DistanceTo(p1) > MinEdgeLengthFt)
                                        proj.Append(Line.CreateBound(p0, p1));
                                }
                                if (!proj.IsOpen()) result.Add(proj);
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        private static CurveLoop BuildLineCurveLoop(List<Pt2D> poly, double z)
        {
            if (poly == null || poly.Count < 3) return null;
            try
            {
                var cl = new CurveLoop();
                int n = poly.Count;
                for (int i = 0; i < n; i++)
                {
                    Pt2D a = poly[i];
                    Pt2D b = poly[(i + 1) % n];
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

        private static void ShowReport(List<CutFloorResult> results, CutFloorScope scope)
        {
            int successCount = results.Count(r => r.Success);
            int totalHoles = results.Sum(r => r.HolesCreatedCount);
            int totalIslands = results.Sum(r => r.IslandsCreatedCount);

            string title = scope == CutFloorScope.ActiveView ? "Cut Floor (Active View)" : "Cut Floor (Selection)";
            string msg = $"✅ Hoàn thành cắt {successCount}/{results.Count} sàn to:\n\n" +
                         $"• Số lỗ mở khoét mới: {totalHoles}\n" +
                         $"• Số mảng sàn độc lập tạo mới: {totalIslands}\n" +
                         $"• Sàn nhỏ cắt (dao cắt) vẫn được giữ nguyên vẹn.\n\n" +
                         $"💡 Các sàn mới đã được tự động chọn (highlight) trên màn hình.";

            TaskDialog.Show(title, msg);
        }

        private static long GetIdValue(ElementId id)
        {
#if REVIT2025_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        private sealed class FloorSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is Floor;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }

        private sealed class WarningSwallower : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                var fmaList = failuresAccessor.GetFailureMessages();
                foreach (var fma in fmaList)
                {
                    if (fma.GetSeverity() == FailureSeverity.Warning)
                    {
                        failuresAccessor.DeleteWarning(fma);
                    }
                }
                return FailureProcessingResult.Continue;
            }
        }
    }
}
