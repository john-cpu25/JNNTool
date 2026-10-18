using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CreateView.Models;
using JNNTool.Tools.CreateView.ViewModels;

namespace JNNTool.Tools.CreateView.Handlers
{
    public class CreateViewHandler : IExternalEventHandler
    {
        private readonly ExternalEvent _externalEvent;

        public CreateViewViewModel? ViewModel { get; set; }
        public bool ProcessAllTabs { get; set; } = true;

        public CreateViewHandler()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public void Raise() => _externalEvent.Raise();

        public string GetName() => "JNN_CreateViewHandler";

        public void Execute(UIApplication app)
        {
            if (ViewModel == null) return;

            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            Document doc = uidoc.Document;

            try
            {
                var selectedLevels = ViewModel.Levels.Where(l => l.IsSelected).ToList();
                if (selectedLevels.Count == 0)
                {
                    ViewModel.StatusMessage = "Vui lòng chọn ít nhất một Level.";
                    return;
                }

                // Determine which tabs to process
                List<ViewTypeTabItem> tabsToProcess;
                if (ProcessAllTabs)
                {
                    tabsToProcess = ViewModel.Tabs.Where(t => t.IsEnabled).ToList();
                    if (tabsToProcess.Count == 0)
                    {
                        ViewModel.StatusMessage = "Không có Tab Type View nào được bật.";
                        return;
                    }
                }
                else
                {
                    if (ViewModel.SelectedTab == null)
                    {
                        ViewModel.StatusMessage = "Vui lòng chọn một Tab Type View.";
                        return;
                    }
                    tabsToProcess = new List<ViewTypeTabItem> { ViewModel.SelectedTab };
                }

                // Default base ViewFamilyType (Structural Plan fallback)
                var baseStructuralVft = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vt => vt.ViewFamily == ViewFamily.StructuralPlan)
                    ?? new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vt => vt.ViewFamily == ViewFamily.FloorPlan);

                if (baseStructuralVft == null)
                {
                    ViewModel.StatusMessage = "Không tìm thấy ViewFamilyType (Structural Plan/Floor Plan) trong dự án.";
                    return;
                }

                // Cache all existing view family types
                var vftMap = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .Where(vt => vt.ViewFamily == ViewFamily.StructuralPlan || vt.ViewFamily == ViewFamily.FloorPlan)
                    .ToDictionary(vt => vt.Name, vt => vt, StringComparer.OrdinalIgnoreCase);

                // Cache all existing views in document
                var existingViews = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(v => !v.IsTemplate)
                    .ToDictionary(v => v.Name, v => v, StringComparer.OrdinalIgnoreCase);

                int createdCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                using (TransactionGroup tg = new TransactionGroup(doc, "JNN - Create Views"))
                {
                    tg.Start();

                    using (Transaction trans = new Transaction(doc, "Batch Create Views"))
                    {
                        trans.Start();

                        foreach (var tab in tabsToProcess)
                        {
                            var activeViews = tab.Views.Where(v => v.IsSelected).ToList();
                            if (activeViews.Count == 0) continue;

                            // Resolve or create ViewFamilyType for this Tab
                            ElementId vftId = baseStructuralVft.Id;

                            if (tab.SelectedViewType != null && tab.SelectedViewType.Id != ElementId.InvalidElementId)
                            {
                                vftId = tab.SelectedViewType.Id;
                            }
                            else if (!string.IsNullOrWhiteSpace(tab.ViewFamilyTypeName))
                            {
                                if (vftMap.TryGetValue(tab.ViewFamilyTypeName, out ViewFamilyType? foundVft))
                                {
                                    vftId = foundVft.Id;
                                }
                                else if (tab.AutoCreateViewType && baseStructuralVft != null)
                                {
                                    try
                                    {
                                        var newVft = baseStructuralVft.Duplicate(tab.ViewFamilyTypeName) as ViewFamilyType;
                                        if (newVft != null)
                                        {
                                            vftId = newVft.Id;
                                            vftMap[newVft.Name] = newVft;
                                        }
                                    }
                                    catch
                                    {
                                        vftId = baseStructuralVft.Id;
                                    }
                                }
                            }

                            // Determine levels to process for this tab
                            List<LevelItem> levelsForTab;
                            if (tab.LevelScope == LevelScopeMode.SingleLevel && tab.TargetLevel != null)
                            {
                                levelsForTab = new List<LevelItem> { tab.TargetLevel };
                            }
                            else
                            {
                                levelsForTab = selectedLevels;
                            }

                            foreach (var level in levelsForTab)
                            {
                                foreach (var rule in activeViews)
                                {
                                    try
                                    {
                                        string targetName = rule.ResolveName(level.Name);
                                        if (string.IsNullOrWhiteSpace(targetName))
                                            targetName = $"{level.Name} Plan";

                                        if (existingViews.TryGetValue(targetName, out View? existingView))
                                        {
                                            if (ViewModel.DuplicateHandling == DuplicateHandling.SkipIfExists)
                                            {
                                                skippedCount++;
                                                continue;
                                            }

                                            if (ViewModel.DuplicateHandling == DuplicateHandling.UpdateExisting && existingView != null)
                                            {
                                                ApplyViewConfigurations(doc, existingView, rule, tab);
                                                updatedCount++;
                                                continue;
                                            }

                                            if (ViewModel.DuplicateHandling == DuplicateHandling.AddSuffixNumber)
                                            {
                                                targetName = GenerateUniqueViewName(existingViews, targetName);
                                            }
                                        }

                                        // Create new ViewPlan
                                        ViewPlan newView = ViewPlan.Create(doc, vftId, level.Id);
                                        if (newView == null)
                                        {
                                            errorCount++;
                                            continue;
                                        }

                                        // Set Name
                                        try
                                        {
                                            newView.Name = targetName;
                                        }
                                        catch
                                        {
                                            targetName = GenerateUniqueViewName(existingViews, targetName);
                                            try { newView.Name = targetName; } catch { }
                                        }

                                        // Apply configurations (Template, ScopeBox, SubDiscipline)
                                        ApplyViewConfigurations(doc, newView, rule, tab);

                                        // Update cache
                                        existingViews[newView.Name] = newView;
                                        createdCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        System.Diagnostics.Debug.WriteLine($"[JNN CreateView Error] {ex.Message}");
                                    }
                                }
                            }
                        }

                        trans.Commit();
                    }

                    tg.Assimilate();
                }

                ViewModel.StatusMessage = $"✓ Đã tạo thành công {createdCount} view, cập nhật {updatedCount} view, bỏ qua {skippedCount} view. ({errorCount} lỗi)";

                TaskDialog.Show("Create Views",
                    $"Đã xử lý xong các View theo yêu cầu:\n\n" +
                    $"• Tạo mới thành công: {createdCount} view\n" +
                    $"• Cập nhật thông số: {updatedCount} view\n" +
                    $"• Bỏ qua do trùng tên: {skippedCount} view\n" +
                    (errorCount > 0 ? $"• Lỗi: {errorCount} view" : ""));
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Lỗi thực thi: {ex.Message}";
                TaskDialog.Show("Create Views - Error", ex.Message);
            }
        }

        private void ApplyViewConfigurations(Document doc, View view, ViewRuleItem rule, ViewTypeTabItem tab)
        {
            // 1. View Template (rule override, otherwise tab default)
            var template = (rule.SelectedViewTemplate != null && !rule.SelectedViewTemplate.IsNone)
                ? rule.SelectedViewTemplate
                : tab.SelectedViewTemplate;

            if (template != null && !template.IsNone)
            {
                try
                {
                    view.ViewTemplateId = template.Id;
                }
                catch { }
            }

            // 2. Scope Box (rule override, otherwise tab default)
            var scopeBox = (rule.SelectedScopeBox != null && !rule.SelectedScopeBox.IsNone)
                ? rule.SelectedScopeBox
                : tab.SelectedScopeBox;

            if (scopeBox != null && !scopeBox.IsNone)
            {
                try
                {
                    var sbParam = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);
                    if (sbParam != null && !sbParam.IsReadOnly)
                    {
                        sbParam.Set(scopeBox.Id);
                    }
                }
                catch { }
            }

            // 3. Browser Sub-Discipline / View Group Folder
            string subDiscipline = !string.IsNullOrWhiteSpace(rule.BrowserSubDiscipline)
                ? rule.BrowserSubDiscipline
                : tab.BrowserSubDiscipline;

            if (!string.IsNullOrWhiteSpace(subDiscipline))
            {
                ApplyBrowserParameter(view, subDiscipline);
            }
        }

        private void ApplyBrowserParameter(View view, string groupValue)
        {
            string[] candidateParamNames = new[]
            {
                ViewModel?.SelectedGroupingParameterName ?? "",
                "Sub-Discipline",
                "Sub Discipline",
                "Views $ Group",
                "View Group",
                "Phân loại",
                "Thư mục View",
                "Folder",
                "Discipline"
            };

            foreach (var paramName in candidateParamNames)
            {
                if (string.IsNullOrWhiteSpace(paramName)) continue;

                var param = view.LookupParameter(paramName);
                if (param != null && !param.IsReadOnly && param.StorageType == StorageType.String)
                {
                    try
                    {
                        param.Set(groupValue);
                        return;
                    }
                    catch { }
                }
            }
        }

        private string GenerateUniqueViewName(Dictionary<string, View> existingViews, string baseName)
        {
            int index = 1;
            string newName = $"{baseName}.{index}";
            while (existingViews.ContainsKey(newName))
            {
                index++;
                newName = $"{baseName}.{index}";
            }
            return newName;
        }
    }
}
