using System.Windows;
using System.Windows.Input;
using JNNTool.Tools.QuantityTakeoff.Models;
using JNNTool.Tools.QuantityTakeoff.ViewModels;

namespace JNNTool.Tools.QuantityTakeoff.Views
{
    public partial class QuantityTakeoffWindow : Window
    {
        public QuantityTakeoffWindow()
        {
            InitializeComponent();
        }

        private void OnLevelGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is QuantityTakeoffViewModel vm && vm.SelectedLevelSummary != null)
            {
                vm.Box3DCommand.Execute(vm.SelectedLevelSummary);
            }
        }

        private void OnCategoryBreakdownDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CategorySummaryItem item)
            {
                if (DataContext is QuantityTakeoffViewModel vm)
                {
                    vm.Box3DCommand.Execute(item);
                }
            }
        }

        private void OnCategoryGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is QuantityTakeoffViewModel vm && vm.SelectedCategorySummary != null)
            {
                vm.Box3DCommand.Execute(vm.SelectedCategorySummary);
            }
        }

        private void OnElementGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is QuantityTakeoffViewModel vm && vm.SelectedElement != null)
            {
                vm.Box3DCommand.Execute(vm.SelectedElement);
            }
        }
    }
}
