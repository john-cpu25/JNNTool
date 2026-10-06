using System;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.IFCEtabs.Revit.Parameters
{
    /// <summary>
    /// Quản lý việc tự động tạo và gắn (Bind) các tham số JNN_* vào cấu kiện Revit.
    /// Đảm bảo tính minh bạch, truy xuất nguồn gốc (Traceability) từ ETABS/IFC.
    /// </summary>
    public static class SharedParameterManager
    {
        public const string ParamSource = "JNN_Source";
        public const string ParamEtabsId = "JNN_ETABS_ID";
        public const string ParamIfcGuid = "JNN_IFC_GUID";
        public const string ParamStory = "JNN_ETABS_Story";
        public const string ParamSection = "JNN_ETABS_Section";
        public const string ParamMaterial = "JNN_ETABS_Material";
        public const string ParamSourceHash = "JNN_Source_Hash";
        public const string ParamConversionStatus = "JNN_Conversion_Status";
        public const string ParamConversionDate = "JNN_Conversion_Date";

        private static readonly string[] ParameterNames = new[]
        {
            ParamSource,
            ParamEtabsId,
            ParamIfcGuid,
            ParamStory,
            ParamSection,
            ParamMaterial,
            ParamSourceHash,
            ParamConversionStatus,
            ParamConversionDate
        };

        /// <summary>
        /// Kiểm tra và tự động khởi tạo các tham số chia sẻ JNN trong dự án Revit nếu chưa tồn tại.
        /// </summary>
        public static void EnsureSharedParameters(Document doc)
        {
            if (doc == null || doc.IsReadOnly) return;

            Application app = doc.Application;
            string originalSharedParamFile = app.SharedParametersFilename;
            string tempParamFile = Path.Combine(Path.GetTempPath(), "JNN_ETABS_SharedParams.txt");

            try
            {
                // 1. Tạo file shared parameter tạm nếu cần
                if (!File.Exists(tempParamFile))
                {
                    File.WriteAllText(tempParamFile,
                        "# This is a Revit shared parameter file.\r\n" +
                        "*META\tVERSION\tMINVERSION\r\n" +
                        "META\t2\t1\r\n" +
                        "*GROUP\tID\tNAME\r\n" +
                        "GROUP\t1\tJNN_ETABS_Converter\r\n" +
                        "*PARAM\tGUID\tNAME\tDATATYPE\tDATACAT\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e801\tJNN_Source\tTEXT\t\t1\t1\tNguồn dữ liệu\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e802\tJNN_ETABS_ID\tTEXT\t\t1\t1\tID cấu kiện trong ETABS\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e803\tJNN_IFC_GUID\tTEXT\t\t1\t1\tGUID IFC\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e804\tJNN_ETABS_Story\tTEXT\t\t1\t1\tTên tầng ETABS\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e805\tJNN_ETABS_Section\tTEXT\t\t1\t1\tTiết diện ETABS\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e806\tJNN_ETABS_Material\tTEXT\t\t1\t1\tVật liệu ETABS\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e807\tJNN_Source_Hash\tTEXT\t\t1\t1\tMã băm kiểm tra thay đổi\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e808\tJNN_Conversion_Status\tTEXT\t\t1\t1\tTrạng thái convert\t1\t0\r\n" +
                        "PARAM\t31c9a62b-656b-4e89-897d-60a0f9b6e809\tJNN_Conversion_Date\tTEXT\t\t1\t1\tNgày import gần nhất\t1\t0\r\n"
                    );
                }

                app.SharedParametersFilename = tempParamFile;
                DefinitionFile defFile = app.OpenSharedParameterFile();
                if (defFile == null) return;

                DefinitionGroup? defGroup = defFile.Groups.get_Item("JNN_ETABS_Converter");
                if (defGroup == null) return;

                // 2. Thiết lập CategorySet cần gắn tham số
                CategorySet catSet = app.Create.NewCategorySet();
                AddCategoryIfValid(doc, catSet, BuiltInCategory.OST_StructuralFraming);
                AddCategoryIfValid(doc, catSet, BuiltInCategory.OST_StructuralColumns);
                AddCategoryIfValid(doc, catSet, BuiltInCategory.OST_Walls);
                AddCategoryIfValid(doc, catSet, BuiltInCategory.OST_Floors);
                AddCategoryIfValid(doc, catSet, BuiltInCategory.OST_StructuralFoundation);

                BindingMap bindingMap = doc.ParameterBindings;

                // 3. Bind từng parameter dạng Instance Binding
                foreach (string pName in ParameterNames)
                {
                    Definition? definition = defGroup.Definitions.get_Item(pName);
                    if (definition != null && !bindingMap.Contains(definition))
                    {
                        InstanceBinding newBinding = app.Create.NewInstanceBinding(catSet);
#if REVIT2024 || REVIT2025_OR_GREATER
                        bindingMap.Insert(definition, newBinding, GroupTypeId.Data);
#else
                        bindingMap.Insert(definition, newBinding, BuiltInParameterGroup.PG_DATA);
#endif
                    }
                }
            }
            catch { }
            finally
            {
                // Khôi phục lại đường dẫn shared parameter cũ của người dùng
                if (!string.IsNullOrEmpty(originalSharedParamFile) && File.Exists(originalSharedParamFile))
                {
                    try { app.SharedParametersFilename = originalSharedParamFile; } catch { }
                }
            }
        }

        private static void AddCategoryIfValid(Document doc, CategorySet catSet, BuiltInCategory bic)
        {
            Category cat = doc.Settings.Categories.get_Item(bic);
            if (cat != null && cat.AllowsBoundParameters)
            {
                catSet.Insert(cat);
            }
        }

        /// <summary>
        /// Gán giá trị chuỗi an toàn cho tham số của Element.
        /// </summary>
        public static void SetParameterValue(Element element, string paramName, string value)
        {
            if (element == null) return;
            Parameter p = element.LookupParameter(paramName);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(value ?? string.Empty);
            }
        }

        /// <summary>
        /// Đọc giá trị chuỗi an toàn từ tham số của Element.
        /// </summary>
        public static string GetParameterValue(Element element, string paramName)
        {
            if (element == null) return string.Empty;
            Parameter p = element.LookupParameter(paramName);
            if (p != null && p.HasValue)
            {
                return p.AsString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
