using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.CreateFilter.Models;

namespace JNNTool.Tools.CreateFilter.ViewModels
{
    public partial class FilterRowViewModel : ObservableObject
    {
        private readonly Document _doc;

        private string _filterName = "New Filter";
        public string FilterName
        {
            get => _filterName;
            set => SetProperty(ref _filterName, value);
        }

        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    LoadParametersForCategory(value);
                }
            }
        }

        private ObservableCollection<ParameterModel> _availableParameters = new ObservableCollection<ParameterModel>();
        public ObservableCollection<ParameterModel> AvailableParameters
        {
            get => _availableParameters;
            set => SetProperty(ref _availableParameters, value);
        }

        private ParameterModel _selectedParameter;
        public ParameterModel SelectedParameter
        {
            get => _selectedParameter;
            set => SetProperty(ref _selectedParameter, value);
        }

        public ObservableCollection<FilterCondition> Conditions { get; } = new ObservableCollection<FilterCondition>
        {
            FilterCondition.Equals,
            FilterCondition.Contains,
            FilterCondition.BeginsWith,
            FilterCondition.EndsWith,
            FilterCondition.DoesNotEqual,
            FilterCondition.DoesNotContain,
            FilterCondition.DoesNotBeginWith,
            FilterCondition.DoesNotEndWith
        };

        private FilterCondition _selectedCondition = FilterCondition.Contains;
        public FilterCondition SelectedCondition
        {
            get => _selectedCondition;
            set => SetProperty(ref _selectedCondition, value);
        }

        private string _filterValue = "";
        public string FilterValue
        {
            get => _filterValue;
            set => SetProperty(ref _filterValue, value);
        }

        private ObservableCollection<ColorModel> _colors = new ObservableCollection<ColorModel>();
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

        public FilterRowViewModel(Document doc, ObservableCollection<CategoryModel> availableCategories, ObservableCollection<ColorModel> sharedColors)
        {
            _doc = doc;
            AvailableCategories = availableCategories;
            
            // Clone colors so each row can add its own custom colors
            foreach (var color in sharedColors)
            {
                Colors.Add(color);
            }
            SelectedColor = Colors.FirstOrDefault();
        }

        public ObservableCollection<CategoryModel> AvailableCategories { get; }

        private void LoadParametersForCategory(CategoryModel category)
        {
            AvailableParameters.Clear();
            SelectedParameter = null;
            if (category == null) return;

            try
            {
                var categoryIds = new List<ElementId> { category.Id };
                var paramIds = ParameterFilterUtilities.GetFilterableParametersInCommon(_doc, categoryIds);
                
                var parameters = new List<ParameterModel>();
                foreach (var id in paramIds)
                {
                    try
                    {
                        var paramElement = _doc.GetElement(id) as ParameterElement;
#if NET48
                        string name = paramElement != null ? paramElement.GetDefinition().Name : LabelUtils.GetLabelFor((BuiltInParameter)id.IntegerValue);
#else
                        string name = paramElement != null ? paramElement.GetDefinition().Name : LabelUtils.GetLabelFor((BuiltInParameter)(int)id.Value);
#endif
                        parameters.Add(new ParameterModel { Name = name, Id = id });
                    }
                    catch { }
                }

                parameters = parameters.OrderBy(p => p.Name).ToList();

                foreach (var param in parameters)
                {
                    AvailableParameters.Add(param);
                }
                
                SelectedParameter = AvailableParameters.FirstOrDefault();
            }
            catch (Exception)
            {
                // Fallback or error handling
            }
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
    }
}
