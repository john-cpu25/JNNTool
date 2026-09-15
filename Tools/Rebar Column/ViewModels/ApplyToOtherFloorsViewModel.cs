using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JNNTool.Tools.RebarColumn.ViewModels
{
    public class ApplyStoreyItem : ObservableObject
    {
        public int No { get; set; }
        public string Section { get; set; }
        public string LevelName { get; set; }
        public double Elevation { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ApplyToOtherFloorsViewModel ParentViewModel { get; set; }

        public ApplyStoreyItem()
        {
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsSelected))
                    ParentViewModel?.UpdateSelectedCount();
            };
        }
    }

    public class ApplyToOtherFloorsViewModel : ObservableObject
    {
        public ObservableCollection<ApplyStoreyItem> StoreyItems { get; } = new ObservableCollection<ApplyStoreyItem>();
        private int _selectedCount = 0;
        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        public ApplyToOtherFloorsViewModel()
        {
            // Populate mock/representative storey stack matching the user's photo
            StoreyItems.Add(new ApplyStoreyItem { No = 1, Section = "Rec: (S=400x300)", LevelName = "TẦNG 5", Elevation = 14650, IsSelected = false, ParentViewModel = this });
            StoreyItems.Add(new ApplyStoreyItem { No = 2, Section = "Rec: (S=400x300)", LevelName = "TẦNG 4", Elevation = 11050, IsSelected = false, ParentViewModel = this });
            StoreyItems.Add(new ApplyStoreyItem { No = 3, Section = "Rec: (S=400x300)", LevelName = "TẦNG 3", Elevation = 7450, IsSelected = true, ParentViewModel = this });
            StoreyItems.Add(new ApplyStoreyItem { No = 4, Section = "Rec: (S=400x300)", LevelName = "TẦNG 2", Elevation = 3850, IsSelected = true, ParentViewModel = this });
            StoreyItems.Add(new ApplyStoreyItem { No = 5, Section = "Rec: (S=400x300)", LevelName = "TẦNG 1", Elevation = -50, IsSelected = true, ParentViewModel = this });
            StoreyItems.Add(new ApplyStoreyItem { No = 6, Section = "Rec: (S=400x300)", LevelName = "MÓNG", Elevation = -1100, IsSelected = true, ParentViewModel = this });

            UpdateSelectedCount();
        }

        public void UpdateSelectedCount()
        {
            SelectedCount = StoreyItems.Count(item => item.IsSelected);
        }
        private void SelectAll()
        {
            foreach (var item in StoreyItems)
            {
                item.IsSelected = true;
            }
        }
        private void UnselectAll()
        {
            foreach (var item in StoreyItems)
            {
                item.IsSelected = false;
            }
        }
    }
}

