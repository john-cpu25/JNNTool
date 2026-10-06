using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Mapping.Services;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tìm kiếm hoặc tự động nhân bản (Duplicate) FamilySymbol cho Dầm và Cột.
    /// Tự động gán kích thước b (Width) và h (Depth) theo tiết diện ETABS.
    /// </summary>
    public class TypeAutoCreator
    {
        private readonly Document _doc;
        private readonly Dictionary<string, FamilySymbol> _framingSymbols = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FamilySymbol> _columnSymbols = new(StringComparer.OrdinalIgnoreCase);

        public TypeAutoCreator(Document doc)
        {
            _doc = doc;
            RefreshSymbolCaches();
        }

        public void RefreshSymbolCaches()
        {
            _framingSymbols.Clear();
            _columnSymbols.Clear();

            var framing = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilySymbol>();

            foreach (var fs in framing)
            {
                _framingSymbols[fs.Name] = fs;
                _framingSymbols[$"{fs.FamilyName}:{fs.Name}"] = fs;
            }

            var columns = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .Cast<FamilySymbol>();

            foreach (var fs in columns)
            {
                _columnSymbols[fs.Name] = fs;
                _columnSymbols[$"{fs.FamilyName}:{fs.Name}"] = fs;
            }
        }

        /// <summary>
        /// Lấy hoặc tự động tạo FamilySymbol cho Dầm.
        /// </summary>
        public FamilySymbol? GetOrCreateBeamSymbol(string sectionName, double widthMm, double depthMm, bool autoCreate = true)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) sectionName = $"B{widthMm:F0}x{depthMm:F0}";

            // 1. Kiểm tra trong bảng ánh xạ MappingService
            var mapping = MappingService.Instance.GetSectionMapping(sectionName);
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.RevitTypeName))
            {
                if (_framingSymbols.TryGetValue(mapping.RevitTypeName, out var mappedSymbol))
                {
                    if (!mappedSymbol.IsActive) mappedSymbol.Activate();
                    return mappedSymbol;
                }
            }

            // 2. Tìm theo tên trực tiếp
            if (_framingSymbols.TryGetValue(sectionName, out var directSymbol))
            {
                if (!directSymbol.IsActive) directSymbol.Activate();
                return directSymbol;
            }

            // Tìm theo dạng biến thể: B400x600 -> 400x600
            string cleanName = sectionName.TrimStart('B', 'b', 'D', 'd', '_', '-');
            if (_framingSymbols.TryGetValue(cleanName, out var cleanSymbol))
            {
                if (!cleanSymbol.IsActive) cleanSymbol.Activate();
                return cleanSymbol;
            }

            if (!autoCreate) return null;

            // 3. Tự động Duplicate từ một Family chữ nhật mẫu
            var template = _framingSymbols.Values.FirstOrDefault();
            if (template == null) return null;

            try
            {
                FamilySymbol newSymbol = (FamilySymbol)template.Duplicate(sectionName);
                SetDimensionParameters(newSymbol, widthMm, depthMm);

                if (!newSymbol.IsActive) newSymbol.Activate();
                _framingSymbols[newSymbol.Name] = newSymbol;
                _framingSymbols[$"{newSymbol.FamilyName}:{newSymbol.Name}"] = newSymbol;

                // Lưu vào mapping cache
                MappingService.Instance.SetSectionMapping(sectionName, newSymbol.FamilyName, newSymbol.Name, widthMm, depthMm);
                return newSymbol;
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Lấy hoặc tự động tạo FamilySymbol cho Cột.
        /// </summary>
        public FamilySymbol? GetOrCreateColumnSymbol(string sectionName, double widthMm, double depthMm, bool autoCreate = true)
        {
            if (string.IsNullOrWhiteSpace(sectionName)) sectionName = $"C{widthMm:F0}x{depthMm:F0}";

            // 1. Kiểm tra MappingService
            var mapping = MappingService.Instance.GetSectionMapping(sectionName);
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.RevitTypeName))
            {
                if (_columnSymbols.TryGetValue(mapping.RevitTypeName, out var mappedSymbol))
                {
                    if (!mappedSymbol.IsActive) mappedSymbol.Activate();
                    return mappedSymbol;
                }
            }

            // 2. Tìm theo tên trực tiếp
            if (_columnSymbols.TryGetValue(sectionName, out var directSymbol))
            {
                if (!directSymbol.IsActive) directSymbol.Activate();
                return directSymbol;
            }

            string cleanName = sectionName.TrimStart('C', 'c', '_', '-');
            if (_columnSymbols.TryGetValue(cleanName, out var cleanSymbol))
            {
                if (!cleanSymbol.IsActive) cleanSymbol.Activate();
                return cleanSymbol;
            }

            if (!autoCreate) return null;

            // 3. Tự động Duplicate
            var template = _columnSymbols.Values.FirstOrDefault();
            if (template == null) return null;

            try
            {
                FamilySymbol newSymbol = (FamilySymbol)template.Duplicate(sectionName);
                SetDimensionParameters(newSymbol, widthMm, depthMm);

                if (!newSymbol.IsActive) newSymbol.Activate();
                _columnSymbols[newSymbol.Name] = newSymbol;
                _columnSymbols[$"{newSymbol.FamilyName}:{newSymbol.Name}"] = newSymbol;

                MappingService.Instance.SetSectionMapping(sectionName, newSymbol.FamilyName, newSymbol.Name, widthMm, depthMm);
                return newSymbol;
            }
            catch
            {
                return template;
            }
        }

        private static void SetDimensionParameters(FamilySymbol symbol, double widthMm, double depthMm)
        {
            double widthFt = UnitConverter.MmToFeet(widthMm);
            double depthFt = UnitConverter.MmToFeet(depthMm);

            // Các tên thông số chiều rộng phổ biến trong thư viện Revit
            string[] widthNames = { "b", "Width", "b (Rộng)", "Chiêu rộng", "d1", "B" };
            string[] depthNames = { "h", "Height", "Depth", "h (Cao)", "Chiều cao", "d2", "H" };

            SetParameterByNames(symbol, widthNames, widthFt);
            SetParameterByNames(symbol, depthNames, depthFt);
        }

        private static void SetParameterByNames(FamilySymbol symbol, string[] names, double valueFeet)
        {
            foreach (var name in names)
            {
                Parameter p = symbol.LookupParameter(name);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(valueFeet);
                    return;
                }
            }
        }
    }
}
