using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB;
using JNNTool.Tools.StairDetail.ViewModels;

namespace JNNTool.Tools.StairDetail.UI
{
    public partial class StairDetailWindow : Window
    {
        public StairDetailWindow(List<Element> rebarTypes)
        {
            JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();

            InitializeComponent();
            this.DataContext = new StairDetailViewModel(rebarTypes);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as StairDetailViewModel;
            vm?.SaveSettings();
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

