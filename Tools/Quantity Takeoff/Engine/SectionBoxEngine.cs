using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;

namespace JNNTool.Tools.QuantityTakeoff.Engine
{
    public static class SectionBoxEngine
    {
        private const string QtoViewName = "{3D - JNN QTO}";
        private const double BoxOffsetFeet = 1.5; // ~450 mm context margin

        /// <summary>
        /// Tạo hoặc cập nhật Section Box 3D bao quanh các cấu kiện được chọn, chuyển view và zoom tới đối tượng.
        /// </summary>
        public static bool FocusSectionBox(UIDocument uidoc, ICollection<ElementId> elementIds)
        {
            if (uidoc == null) return false;
            Document doc = uidoc.Document;

            if (elementIds == null || elementIds.Count == 0)
            {
                return false;
            }

            try
            {
                // 1. Tính toán BoundingBoxXYZ bao phủ toàn bộ danh sách phần tử
                BoundingBoxXYZ? unionBox = null;
                var validIds = new List<ElementId>();

                foreach (var id in elementIds)
                {
                    if (id == null || id == ElementId.InvalidElementId) continue;
                    var elem = doc.GetElement(id);
                    if (elem == null) continue;

                    var bbox = elem.get_BoundingBox(null);
                    if (bbox == null) continue;

                    validIds.Add(id);

                    if (unionBox == null)
                    {
                        unionBox = new BoundingBoxXYZ
                        {
                            Min = new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z),
                            Max = new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Max.Z)
                        };
                    }
                    else
                    {
                        unionBox.Min = new XYZ(
                            Math.Min(unionBox.Min.X, bbox.Min.X),
                            Math.Min(unionBox.Min.Y, bbox.Min.Y),
                            Math.Min(unionBox.Min.Z, bbox.Min.Z)
                        );
                        unionBox.Max = new XYZ(
                            Math.Max(unionBox.Max.X, bbox.Max.X),
                            Math.Max(unionBox.Max.Y, bbox.Max.Y),
                            Math.Max(unionBox.Max.Z, bbox.Max.Z)
                        );
                    }
                }

                if (unionBox == null || validIds.Count == 0) return false;

                // Mở rộng thêm margin để nhìn rõ ngữ cảnh xung quanh
                unionBox.Min = new XYZ(
                    unionBox.Min.X - BoxOffsetFeet,
                    unionBox.Min.Y - BoxOffsetFeet,
                    unionBox.Min.Z - BoxOffsetFeet
                );
                unionBox.Max = new XYZ(
                    unionBox.Max.X + BoxOffsetFeet,
                    unionBox.Max.Y + BoxOffsetFeet,
                    unionBox.Max.Z + BoxOffsetFeet
                );

                // 2. Tìm hoặc kích hoạt View 3D phù hợp
                View3D? view3D = uidoc.ActiveView as View3D;

                // Nếu view hiện tại không phải 3D hoặc là phối cảnh / template bị khóa
                if (view3D == null || view3D.IsTemplate || view3D.IsPerspective)
                {
                    view3D = GetOrCreateQto3DView(doc);
                    if (view3D != null)
                    {
                        uidoc.ActiveView = view3D;
                    }
                }

                if (view3D == null) return false;

                // 3. Áp dụng Section Box vào View3D
                using (var t = new Transaction(doc, "JNN QTO — Box 3D"))
                {
                    t.Start();
                    view3D.IsSectionBoxActive = true;
                    view3D.SetSectionBox(unionBox);
                    t.Commit();
                }

                // 4. Highlight và Zoom tới cấu kiện
                try
                {
                    uidoc.Selection.SetElementIds(validIds);
                    uidoc.ShowElements(validIds);
                }
                catch
                {
                    // Một số bản Revit cũ có thể nuốt exception khi zoom
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi thực hiện Box 3D trong Quantity Takeoff", ex);
                return false;
            }
        }

        private static View3D? GetOrCreateQto3DView(Document doc)
        {
            // Tìm view đã có tên {3D - JNN QTO}
            var existingView = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => !v.IsTemplate && !v.IsPerspective &&
                                     v.Name.Equals(QtoViewName, StringComparison.OrdinalIgnoreCase));

            if (existingView != null) return existingView;

            // Tìm ViewFamilyType cho 3D
            var vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            if (vft == null) return null;

            using (var t = new Transaction(doc, "Tạo 3D View JNN QTO"))
            {
                t.Start();
                var newView = View3D.CreateIsometric(doc, vft.Id);
                newView.Name = QtoViewName;
                t.Commit();
                return newView;
            }
        }
    }
}
