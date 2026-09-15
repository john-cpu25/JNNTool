using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool
{
    // ── Bộ lọc: chỉ cho phép chọn Room ─────────────────────────────────────
    public class RoomSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
            => elem is Room;

        public bool AllowReference(Reference reference, XYZ position)
            => false;
    }

    [Transaction(TransactionMode.Manual)]
    public class FloorByRoomCmd : IExternalCommand
    {
        // Danh sách Room đã chọn (giữ xuyên suốt vòng lặp dialog)
        private readonly List<Room> _pickedRooms = new List<Room>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc   = uidoc.Document;

            // Lấy tất cả FloorType trong dự án
            var allFloorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();

            var typeNames = allFloorTypes
                .Select(x => x.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Mở cửa sổ dialog — tạo mới mỗi vòng lặp để tránh lỗi ShowDialog on visible window
            // State được lưu lại qua các biến dưới đây:
            string savedTypeName    = typeNames.Count > 0 ? typeNames[0] : string.Empty;
            string savedThickness   = "200";
            string savedOffset      = "0";
            var    savedRoomNames   = new List<string>();

            while (true)
            {
                // Tạo dialog mới và khôi phục state từ lần trước
                var dialog = new FloorByRoomWindow(typeNames);
                dialog.PresetValues(savedTypeName, savedThickness, savedOffset, savedRoomNames);

                bool? result = dialog.ShowDialog();

                // Lưu lại giá trị người dùng vừa nhập (trước khi dialog bị đóng)
                savedTypeName  = dialog.SelectedFloorTypeName;
                savedThickness = dialog.ThicknessMm.ToString();
                savedOffset    = dialog.OffsetMm.ToString();

                // Người dùng nhấn "Chọn Room" → dialog đã Close(), result == null
                if (dialog.PickRoomsRequested)
                {
                    _pickedRooms.Clear();

                    try
                    {
                        var filter = new RoomSelectionFilter();
                        var refs   = uidoc.Selection.PickObjects(
                            ObjectType.Element,
                            filter,
                            "Chọn các Room cần vé sàn (ESC để kết thúc)");

                        foreach (var r in refs)
                        {
                            if (doc.GetElement(r.ElementId) is Room room)
                                _pickedRooms.Add(room);
                        }
                    }
                    catch
                    {
                        // Người dùng nhấn ESC → giữ nguyên danh sách cũ
                    }

                    // Cập nhật danh sách Room để hiện lại ở vòng tiếp theo
                    savedRoomNames = _pickedRooms
                        .Select(r => $"{r.Number}  –  {r.Name}  ({GetRoomLevel(r)})")
                        .ToList();

                    continue; // Tạo dialog mới ở vòng lặp kế tiếp
                }

                // Người dùng nhấn "Huỷ" hoặc đóng cửa sổ (X)
                if (result != true || !dialog.CreateRequested)
                    return Result.Cancelled;

                // Người dùng nhấn "Tạo Sàn" → tiếp tục
                break;
            }


            // ── Thu thập tham số (dùng biến đã lưu, dialog nằm trong scope while) ──
            string typeName    = savedTypeName;
            double thicknessFt = double.TryParse(savedThickness, out double th) ? th / 304.8 : 200.0 / 304.8;
            double offsetFt    = double.TryParse(savedOffset,    out double of) ? of / 304.8 : 0.0;

            // Tìm / tạo FloorType
            var floorType = allFloorTypes.FirstOrDefault(
                t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (floorType == null)
            {
                // Duplicate từ type đầu tiên tìm được
                var baseType = allFloorTypes.FirstOrDefault(t => !t.IsFoundationSlab)
                            ?? allFloorTypes.FirstOrDefault();

                if (baseType == null)
                {
                    TaskDialog.Show("Lỗi", "Không tìm thấy FloorType nào trong dự án.");
                    return Result.Failed;
                }

                using (var prepTx = new Transaction(doc, "Tạo FloorType Mới"))
                {
                    prepTx.Start();
                    floorType = baseType.Duplicate(typeName) as FloorType;

                    // Gán chiều dày cho lớp core
                    try
                    {
                        var cs = floorType.GetCompoundStructure();
                        if (cs != null)
                        {
                            int idx = cs.GetFirstCoreLayerIndex();
                            cs.SetLayerWidth(idx, thicknessFt);
                            floorType.SetCompoundStructure(cs);
                        }
                    }
                    catch { }

                    prepTx.Commit();
                }
            }

            // ── Tạo sàn trong transaction chính ─────────────────────────────
            int createdCount = 0;
            int failedCount  = 0;

            using (var tx = new Transaction(doc, "Vé Sàn Theo Room"))
            {
                tx.Start();

                foreach (var room in _pickedRooms)
                {
                    try
                    {
                        // Lấy boundary của Room
                        var boundaryOptions = new SpatialElementBoundaryOptions
                        {
                            SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
                        };

                        IList<IList<BoundarySegment>> segments =
                            room.GetBoundarySegments(boundaryOptions);

                        if (segments == null || segments.Count == 0)
                        {
                            failedCount++;
                            continue;
                        }

                        // Xây dựng CurveLoop từ boundary segment đầu tiên (outer)
                        var curveLoops = new List<CurveLoop>();
                        foreach (var loop in segments)
                        {
                            var cl = new CurveLoop();
                            foreach (var seg in loop)
                                cl.Append(seg.GetCurve());
                            curveLoops.Add(cl);
                        }

                        // Lấy Level của Room
                        Level level = doc.GetElement(room.LevelId) as Level;
                        if (level == null)
                        {
                            level = new FilteredElementCollector(doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .OrderBy(l => l.Elevation)
                                .FirstOrDefault();
                        }
                        if (level == null) { failedCount++; continue; }

                        // Tạo Floor
                        Floor newFloor = Floor.Create(doc, curveLoops, floorType.Id, level.Id);

                        // Gán Offset (chiều cao so với Level)
                        if (newFloor != null && Math.Abs(offsetFt) > 1e-6)
                        {
                            var offsetParam = newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                            if (offsetParam != null && !offsetParam.IsReadOnly)
                                offsetParam.Set(offsetFt);
                        }

                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[FloorByRoom] Room {room.Name}: {ex.Message}");
                        failedCount++;
                    }
                }

                tx.Commit();
            }

            // ── Thông báo kết quả ────────────────────────────────────────────
            string summary = $"✅  Đã tạo: {createdCount} sàn.";
            if (failedCount > 0)
                summary += $"\n⚠️  Lỗi: {failedCount} room (xem Output Window để chi tiết).";

            TaskDialog.Show("Vé Sàn Hoàn Tất", summary);
            return Result.Succeeded;
        }

        private static string GetRoomLevel(Room room)
        {
            var doc   = room.Document;
            var level = doc.GetElement(room.LevelId) as Level;
            return level?.Name ?? "—";
        }
    }
}
