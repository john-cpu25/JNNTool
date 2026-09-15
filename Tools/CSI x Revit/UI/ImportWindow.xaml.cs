using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CSIxRevit.UI
{
    public partial class ImportWindow : Window
    {
        public ObservableCollection<SectionMappingItem> MappingItems { get; set; }
        
        public ImportWindow(
            Document doc,
            List<SectionMappingItem> items,
            List<Level> levels)
        {
            InitializeComponent();
            
            CmbLevels.ItemsSource = levels;
            if (levels.Count > 0) CmbLevels.SelectedIndex = 0;

            MappingItems = new ObservableCollection<SectionMappingItem>(items);
            DgMapping.ItemsSource = MappingItems;
            AutoMatch();
        }

        private void AutoMatch()
        {
            foreach (var item in MappingItems)
            {
                if (item.AvailableTypes == null || item.AvailableTypes.Count == 0) continue;

                string etabsName = item.EtabsSectionName.Replace(" ", "").Replace("_", "").Replace("-", "").ToLower();

                var exactMatch = item.AvailableTypes.FirstOrDefault(s => s.Name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLower() == etabsName);
                if (exactMatch != null)
                {
                    item.SelectedType = exactMatch;
                    continue;
                }

                var partialMatch = item.AvailableTypes.FirstOrDefault(s => s.Name.ToLower().Contains(etabsName) || etabsName.Contains(s.Name.ToLower()));
                if (partialMatch != null)
                {
                    item.SelectedType = partialMatch;
                    continue;
                }

                item.SelectedType = item.AvailableTypes.FirstOrDefault();
            }
        }

        private void BtnAutoMatch_Click(object sender, RoutedEventArgs e)
        {
            AutoMatch();
            DgMapping.Items.Refresh();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Dictionary<string, ElementType> GetBeamMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Dầm (Beam)").ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetColumnMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Cột (Column)").ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetFloorMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Sàn (Floor)").ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetWallMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Vách (Wall)").ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public ElementId GetSelectedLevelId()
        {
            if (CmbLevels.SelectedItem is Level level) return level.Id;
            return ElementId.InvalidElementId;
        }
    }

    public class SectionMappingItem
    {
        public string EtabsSectionName { get; set; }
        public string SectionDetails { get; set; }
        public string ElementType { get; set; }
        public int ObjectCount { get; set; }
        public ElementType SelectedType { get; set; }
        public List<ElementType> AvailableTypes { get; set; }
    }
}
