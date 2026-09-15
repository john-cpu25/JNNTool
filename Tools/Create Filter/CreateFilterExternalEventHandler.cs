using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CreateFilter.Models;
using JNNTool.Tools.CreateFilter.ViewModels;

namespace JNNTool.Tools.CreateFilter
{
    public class CreateFilterExternalEventHandler : IExternalEventHandler
    {
        public List<FilterRowViewModel> FilterRows { get; set; }
        public System.Action CloseWindowAction { get; set; }

        public void Execute(UIApplication app)
        {
            if (app.ActiveUIDocument == null) return;
            Document doc = app.ActiveUIDocument.Document;
            if (doc == null || doc.IsReadOnly) return;
            
            if (FilterRows == null || FilterRows.Count == 0) return;

            using (Transaction t = new Transaction(doc, "Create and Apply Filters"))
            {
                t.Start();
                int successCount = 0;

                foreach (var row in FilterRows)
                {
                    if (string.IsNullOrWhiteSpace(row.FilterName) || row.SelectedCategory == null || row.SelectedParameter == null)
                        continue;

                    try
                    {
                        var categoryIds = new List<ElementId> { row.SelectedCategory.Id };

                        // 1. Create Filter Rule
                        FilterRule filterRule = CreateRule(doc, row.SelectedParameter.Id, row.SelectedCondition, row.FilterValue);
                        if (filterRule == null) continue;
                        ElementFilter elementFilter = new ElementParameterFilter(filterRule);

                        // 2. Check if filter already exists
                        var existingFilter = new FilteredElementCollector(doc)
                            .OfClass(typeof(FilterElement))
                            .FirstOrDefault(f => f.Name.Equals(row.FilterName, StringComparison.OrdinalIgnoreCase));

                        ParameterFilterElement filterElement = existingFilter as ParameterFilterElement;

                        if (filterElement == null)
                        {
                            filterElement = ParameterFilterElement.Create(doc, row.FilterName, categoryIds);
                            filterElement.SetElementFilter(elementFilter);
                        }
                        else
                        {
                            // Update existing filter categories and rules
                            var currentCategories = filterElement.GetCategories();
                            if (!currentCategories.Contains(row.SelectedCategory.Id))
                            {
                                currentCategories.Add(row.SelectedCategory.Id);
                                filterElement.SetCategories(currentCategories);
                            }
                            filterElement.SetElementFilter(elementFilter);
                        }

                        // 3. Apply to Active View
                        View activeView = doc.ActiveView;
                        if (activeView == null || !activeView.AreGraphicsOverridesAllowed())
                        {
                            successCount++;
                            continue;
                        }

                        if (!activeView.GetFilters().Contains(filterElement.Id))
                        {
                            activeView.AddFilter(filterElement.Id);
                        }

                        // 4. Set Override Graphic Settings
                        OverrideGraphicSettings overrideSettings = activeView.GetFilterOverrides(filterElement.Id);
                        
                        // Apply Color to Line and Pattern (Projection and Cut)
                        if (row.SelectedColor != null)
                        {
                            overrideSettings.SetProjectionLineColor(row.SelectedColor.RevitColor);
                            overrideSettings.SetSurfaceForegroundPatternColor(row.SelectedColor.RevitColor);
                            
                            overrideSettings.SetCutLineColor(row.SelectedColor.RevitColor);
                            overrideSettings.SetCutForegroundPatternColor(row.SelectedColor.RevitColor);
                            
                            // Try to get solid fill pattern
                            FillPatternElement solidPattern = new FilteredElementCollector(doc)
                                .OfClass(typeof(FillPatternElement))
                                .Cast<FillPatternElement>()
                                .FirstOrDefault(f => {
                                    try { return f.GetFillPattern()?.IsSolidFill == true; }
                                    catch { return false; }
                                });

                            if (solidPattern != null)
                            {
                                overrideSettings.SetSurfaceForegroundPatternId(solidPattern.Id);
                                overrideSettings.SetCutForegroundPatternId(solidPattern.Id);
                            }
                        }

                        activeView.SetFilterOverrides(filterElement.Id, overrideSettings);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        // Handle error for individual row
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }

                // Chỉ commit khi có ít nhất 1 filter được xử lý thành công
                if (successCount > 0)
                    t.Commit();
                else
                    t.RollBack();
            }

            // Đóng window sau khi execute xong
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                CloseWindowAction?.Invoke());
        }

        /// <summary>
        /// Tạo FilterRule phù hợp với kiểu dữ liệu của parameter.
        /// net48 (Revit 2022-2024): dùng overload có caseSensitive.
        /// net8.0 (Revit 2025+): dùng overload mới không có caseSensitive.
        /// </summary>
        private FilterRule CreateRule(Document doc, ElementId paramId, FilterCondition condition, string value)
        {
            try
            {
                switch (condition)
                {
#if NET48
                    case FilterCondition.Equals:
                        return ParameterFilterRuleFactory.CreateEqualsRule(paramId, value, true);
                    case FilterCondition.Contains:
                        return ParameterFilterRuleFactory.CreateContainsRule(paramId, value, true);
                    case FilterCondition.BeginsWith:
                        return ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, value, true);
                    case FilterCondition.EndsWith:
                        return ParameterFilterRuleFactory.CreateEndsWithRule(paramId, value, true);
                    case FilterCondition.DoesNotEqual:
                        return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value, true);
                    case FilterCondition.DoesNotContain:
                        return ParameterFilterRuleFactory.CreateNotContainsRule(paramId, value, true);
                    case FilterCondition.DoesNotBeginWith:
                        return ParameterFilterRuleFactory.CreateNotBeginsWithRule(paramId, value, true);
                    case FilterCondition.DoesNotEndWith:
                        return ParameterFilterRuleFactory.CreateNotEndsWithRule(paramId, value, true);
                    default:
                        return ParameterFilterRuleFactory.CreateContainsRule(paramId, value, true);
#else
                    case FilterCondition.Equals:
                        return ParameterFilterRuleFactory.CreateEqualsRule(paramId, value);
                    case FilterCondition.Contains:
                        return ParameterFilterRuleFactory.CreateContainsRule(paramId, value);
                    case FilterCondition.BeginsWith:
                        return ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, value);
                    case FilterCondition.EndsWith:
                        return ParameterFilterRuleFactory.CreateEndsWithRule(paramId, value);
                    case FilterCondition.DoesNotEqual:
                        return ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value);
                    case FilterCondition.DoesNotContain:
                        return ParameterFilterRuleFactory.CreateNotContainsRule(paramId, value);
                    case FilterCondition.DoesNotBeginWith:
                        return ParameterFilterRuleFactory.CreateNotBeginsWithRule(paramId, value);
                    case FilterCondition.DoesNotEndWith:
                        return ParameterFilterRuleFactory.CreateNotEndsWithRule(paramId, value);
                    default:
                        return ParameterFilterRuleFactory.CreateContainsRule(paramId, value);
#endif
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateRule failed for param {paramId}: {ex.Message}");
                return null;
            }
        }

        public string GetName() => "CreateFilterExternalEventHandler";
    }
}
