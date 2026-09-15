using System;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.CreateFilter.Models;

namespace JNNTool.Tools.CreateFilter.ViewModels
{
    public partial class CreateFilterViewModel : ObservableObject
    {
        private readonly Document _doc;
        private readonly CreateFilterExternalEventHandler _handler;
        private readonly Autodesk.Revit.UI.ExternalEvent _exEvent;

        private ObservableCollection<FilterRowViewModel> _filterRows = new ObservableCollection<FilterRowViewModel>();
        public ObservableCollection<FilterRowViewModel> FilterRows
        {
            get => _filterRows;
            set => SetProperty(ref _filterRows, value);
        }

        private ObservableCollection<CategoryModel> _availableCategories = new ObservableCollection<CategoryModel>();
        public ObservableCollection<CategoryModel> AvailableCategories
        {
            get => _availableCategories;
            set => SetProperty(ref _availableCategories, value);
        }

        private ObservableCollection<ColorModel> _sharedColors;

        public CreateFilterViewModel(Document doc, CreateFilterExternalEventHandler handler, Autodesk.Revit.UI.ExternalEvent exEvent)
        {
            _doc = doc;
            _handler = handler;
            _exEvent = exEvent;

            LoadCategories();
            InitializeSharedColors();
            AddRow(); // Add one default row
        }

        private bool _isCategoryModeCurrentView = true;
        public bool IsCategoryModeCurrentView
        {
            get => _isCategoryModeCurrentView;
            set
            {
                if (SetProperty(ref _isCategoryModeCurrentView, value))
                {
                    LoadCategories();
                }
            }
        }

        private bool _isCategoryModeProject = false;
        public bool IsCategoryModeProject
        {
            get => _isCategoryModeProject;
            set
            {
                if (SetProperty(ref _isCategoryModeProject, value))
                {
                    LoadCategories();
                }
            }
        }

        private void LoadCategories()
        {
            AvailableCategories.Clear();
            var categories = _doc.Settings.Categories;

            System.Collections.Generic.HashSet<ElementId> currentViewCategoryIds = null;
            if (IsCategoryModeCurrentView && _doc.ActiveView != null)
            {
                try
                {
                    currentViewCategoryIds = new System.Collections.Generic.HashSet<ElementId>(
                        new FilteredElementCollector(_doc, _doc.ActiveView.Id)
                            .WhereElementIsNotElementType()
                            .Select(e => e.Category?.Id)
                            .Where(id => id != null)
                    );
                }
                catch { }
            }

            var tempCategories = new System.Collections.Generic.List<CategoryModel>();
            var filterableCategoryIds = ParameterFilterUtilities.GetAllFilterableCategories();

            foreach (Category cat in categories)
            {
                if (cat.CategoryType == CategoryType.Model || cat.CategoryType == CategoryType.Annotation)
                {
                    if (IsCategoryModeCurrentView && currentViewCategoryIds != null && !currentViewCategoryIds.Contains(cat.Id))
                    {
                        continue; // Skip if not in current view
                    }

                    if (!filterableCategoryIds.Contains(cat.Id))
                    {
                        continue; // Skip if not filterable
                    }

                    try
                    {
                        if (ParameterFilterUtilities.GetFilterableParametersInCommon(_doc, new[] { cat.Id }).Count > 0)
                        {
                            tempCategories.Add(new CategoryModel { Name = cat.Name, Id = cat.Id });
                        }
                    }
                    catch { }
                }
            }
            
            foreach (var cat in tempCategories.OrderBy(c => c.Name))
            {
                AvailableCategories.Add(cat);
            }
        }

        private void InitializeSharedColors()
        {
            _sharedColors = new ObservableCollection<ColorModel>
            {
                new ColorModel("Red", 255, 0, 0),
                new ColorModel("Green", 0, 255, 0),
                new ColorModel("Blue", 0, 0, 255),
                new ColorModel("Yellow", 255, 255, 0),
                new ColorModel("Cyan", 0, 255, 255),
                new ColorModel("Magenta", 255, 0, 255),
                new ColorModel("Orange", 255, 165, 0),
                new ColorModel("Black", 0, 0, 0)
            };
        }

        [RelayCommand]
        private void AddRow()
        {
            var newRow = new FilterRowViewModel(_doc, AvailableCategories, _sharedColors);
            FilterRows.Add(newRow);
        }

        [RelayCommand]
        private void RemoveRow(FilterRowViewModel row)
        {
            if (row != null && FilterRows.Contains(row))
            {
                FilterRows.Remove(row);
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            if (FilterRows.Count == 0) return;

            _handler.FilterRows = FilterRows.ToList();
            _exEvent.Raise();
            // Window sẽ được đóng bởi handler sau khi execute xong
        }
    }
}
