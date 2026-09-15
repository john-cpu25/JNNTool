using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.SpeedOverriderElement.Models;

namespace JNNTool.Tools.SpeedOverriderElement.ViewModels
{
    public partial class SpeedOverriderViewModel : ObservableObject
    {
        private readonly Document _doc;
        private readonly ExternalEvent _externalEvent;

        private ObservableCollection<CategoryModel> _categories;
        public ObservableCollection<CategoryModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    LoadTypesForCategory(value);
                }
            }
        }

        private ObservableCollection<TypeModel> _types;
        public ObservableCollection<TypeModel> Types
        {
            get => _types;
            set => SetProperty(ref _types, value);
        }

        private TypeModel _selectedType;
        public TypeModel SelectedType
        {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }

        private ObservableCollection<ColorModel> _colors;
        public ObservableCollection<ColorModel> Colors
        {
            get => _colors;
            set => SetProperty(ref _colors, value);
        }

        private ColorModel _selectedColor;
        public ColorModel SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        public bool IsResetting { get; set; } = false;

        public SpeedOverriderViewModel(Document doc, ExternalEvent externalEvent)
        {
            _doc = doc;
            _externalEvent = externalEvent;

            Categories = new ObservableCollection<CategoryModel>();
            Types = new ObservableCollection<TypeModel>();
            Colors = new ObservableCollection<ColorModel>
            {
                new ColorModel("Red", 255, 0, 0),
                new ColorModel("Green", 0, 255, 0),
                new ColorModel("Blue", 0, 0, 255),
                new ColorModel("Yellow", 255, 255, 0),
                new ColorModel("Cyan", 0, 255, 255),
                new ColorModel("Magenta", 255, 0, 255),
                new ColorModel("Orange", 255, 165, 0),
                new ColorModel("Purple", 128, 0, 128),
                new ColorModel("Black", 0, 0, 0),
                new ColorModel("Gray", 128, 128, 128)
            };
            
            SelectedColor = Colors.FirstOrDefault();

            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            if (_doc.ActiveView == null) return;
            
            try
            {
                // Get categories present in the active view
                var collector = new FilteredElementCollector(_doc, _doc.ActiveView.Id)
                    .WhereElementIsNotElementType();

                var categoryIds = collector.Select(e => e.Category?.Id)
                                           .Where(id => id != null && id != ElementId.InvalidElementId)
                                           .Distinct();

                var sortedCategories = categoryIds.Select(id => Category.GetCategory(_doc, id))
                                                  .Where(c => c != null)
                                                  .OrderBy(c => c.Name)
                                                  .Select(c => new CategoryModel { Name = c.Name, Id = c.Id })
                                                  .ToList();

                foreach (var cat in sortedCategories)
                {
                    Categories.Add(cat);
                }
            }
            catch { }
        }

        private void LoadTypesForCategory(CategoryModel category)
        {
            Types.Clear();
            if (category == null || _doc.ActiveView == null) return;

            try
            {
                // Get types for elements of the selected category in the active view
                var elements = new FilteredElementCollector(_doc, _doc.ActiveView.Id)
                    .OfCategoryId(category.Id)
                    .WhereElementIsNotElementType();

                var typeIds = elements.Select(e => e.GetTypeId())
                                      .Where(id => id != null && id != ElementId.InvalidElementId)
                                      .Distinct();

                var sortedTypes = typeIds.Select(id => _doc.GetElement(id) as ElementType)
                                         .Where(t => t != null)
                                         .OrderBy(t => t.Name)
                                         .Select(t => new TypeModel { Name = t.Name, Id = t.Id })
                                         .ToList();

                foreach (var type in sortedTypes)
                {
                    Types.Add(type);
                }
            }
            catch { }
            
            SelectedType = Types.FirstOrDefault();
        }

        [RelayCommand]
        private void Apply()
        {
            if (SelectedCategory == null || SelectedType == null || SelectedColor == null)
            {
                TaskDialog.Show("Warning", "Please select Category, Type, and Color.");
                return;
            }

            IsResetting = false;
            _externalEvent.Raise();
            // Window sẽ được đóng bởi handler sau khi execute xong
        }

        [RelayCommand]
        private void SelectCustomColor()
        {
            var colorDialog = new ColorSelectionDialog();
            if (colorDialog.Show() == ItemSelectionDialogResult.Confirmed)
            {
                var rColor = colorDialog.SelectedColor;
                var newColorModel = new ColorModel("Custom", rColor.Red, rColor.Green, rColor.Blue);
                Colors.Add(newColorModel);
                SelectedColor = newColorModel;
            }
        }

        [RelayCommand]
        private void Reset()
        {
            if (SelectedCategory == null || SelectedType == null)
            {
                TaskDialog.Show("Warning", "Please select Category and Type to reset.");
                return;
            }

            IsResetting = true;
            _externalEvent.Raise();
            // Window sẽ được đóng bởi handler sau khi execute xong
        }
    }
}
