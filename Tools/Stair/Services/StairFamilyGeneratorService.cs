using System;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using JNN.Stair.Models;

namespace JNN.Stair.Services
{
    /// <summary>
    /// Dịch vụ tự động sinh file Family cầu thang (.rfa) dạng Generic Model và nạp vào Project
    /// </summary>
    public class StairFamilyGeneratorService
    {
        private readonly Application _app;

        public StairFamilyGeneratorService(Application app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        /// <summary>
        /// Tạo Family cầu thang và nạp thẳng vào Revit Project
        /// </summary>
        public bool CreateAndLoadIntoProject(
            Document projectDoc,
            StairLayout layout,
            string templateRftPath,
            string outputDirectory,
            out FamilySymbol? loadedSymbol,
            out string errorMessage)
        {
            loadedSymbol = null;
            errorMessage = string.Empty;

            if (!File.Exists(templateRftPath))
            {
                errorMessage = $"Không tìm thấy file Template Generic Model: {templateRftPath}";
                return false;
            }

            string familyName = string.IsNullOrWhiteSpace(layout.FamilyName) ? "JNN_Thang_BTCT" : layout.FamilyName;
            string rfaPath = Path.Combine(outputDirectory, $"{familyName}.rfa");

            try
            {
                // 1. Mở Family Document từ template
                Document familyDoc = _app.NewFamilyDocument(templateRftPath);
                if (familyDoc == null)
                {
                    errorMessage = "Không thể khởi tạo Family Document từ template.";
                    return false;
                }

                using (Transaction trans = new Transaction(familyDoc, "Tạo hình học Cầu Thang"))
                {
                    trans.Start();

                    // Dựng khối Solid hoặc FreeFormElement từ các đỉnh và mặt đã tính toán
                    // Dùng TessellatedShapeBuilder để ghép các mặt thành một Solid hoàn chỉnh
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(true);

                    // Chuyển đổi mm sang Feet nội bộ của Revit
                    double mmToFt = 1.0 / 304.8;
                    var components = Core.StairGeometryEngine.BuildUShapeStair(layout);

                    // Ghép các mặt khối hộp của vế và chiếu nghỉ
                    foreach (var comp in components)
                    {
                        if (comp.Vertices.Count >= 8)
                        {
                            // Đơn giản hóa thành khối hộp bao cảnh
                            var p0 = ToXyz(comp.Vertices[0], mmToFt);
                            var p1 = ToXyz(comp.Vertices[1], mmToFt);
                            var p2 = ToXyz(comp.Vertices[2], mmToFt);
                            var p3 = ToXyz(comp.Vertices[3], mmToFt);
                            var p4 = ToXyz(comp.Vertices[4], mmToFt);
                            var p5 = ToXyz(comp.Vertices[5], mmToFt);
                            var p6 = ToXyz(comp.Vertices[6], mmToFt);
                            var p7 = ToXyz(comp.Vertices[7], mmToFt);

                            // Mặt đáy
                            builder.AddFace(new TessellatedFace(new[] { p0, p3, p2, p1 }, ElementId.InvalidElementId));
                            // Mặt đỉnh
                            builder.AddFace(new TessellatedFace(new[] { p4, p5, p6, p7 }, ElementId.InvalidElementId));
                            // 4 mặt bên
                            builder.AddFace(new TessellatedFace(new[] { p0, p1, p5, p4 }, ElementId.InvalidElementId));
                            builder.AddFace(new TessellatedFace(new[] { p1, p2, p6, p5 }, ElementId.InvalidElementId));
                            builder.AddFace(new TessellatedFace(new[] { p2, p3, p7, p6 }, ElementId.InvalidElementId));
                            builder.AddFace(new TessellatedFace(new[] { p3, p0, p4, p7 }, ElementId.InvalidElementId));
                        }
                    }

                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.Solid;
                    builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
                    builder.Build();

                    var result = builder.GetBuildResult();
                    if (result.Outcome == TessellatedShapeBuilderOutcome.Solid)
                    {
                        foreach (GeometryObject geom in result.GetGeometricalObjects())
                        {
                            if (geom is Solid solid)
                            {
                                FreeFormElement.Create(familyDoc, solid);
                            }
                        }
                    }

                    trans.Commit();
                }

                // 2. Lưu Family ra đĩa
                SaveAsOptions saveOptions = new SaveAsOptions { OverwriteExistingFile = true };
                familyDoc.SaveAs(rfaPath, saveOptions);

                // 3. Nạp vào dự án chính
                using (Transaction loadTrans = new Transaction(projectDoc, "Nạp Family Cầu Thang"))
                {
                    loadTrans.Start();

                    Family loadedFamily = familyDoc.LoadFamily(projectDoc, new StairFamilyLoadOption());
                    familyDoc.Close(false);

                    if (loadedFamily != null)
                    {
                        foreach (ElementId symbolId in loadedFamily.GetFamilySymbolIds())
                        {
                            loadedSymbol = projectDoc.GetElement(symbolId) as FamilySymbol;
                            if (loadedSymbol != null && !loadedSymbol.IsActive)
                            {
                                loadedSymbol.Activate();
                            }
                            break;
                        }
                    }

                    loadTrans.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi khi tạo Family: {ex.Message}";
                return false;
            }
        }

        private static XYZ ToXyz(StairPoint3D pt, double scale) => new XYZ(pt.X * scale, pt.Y * scale, pt.Z * scale);

        private class StairFamilyLoadOption : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
