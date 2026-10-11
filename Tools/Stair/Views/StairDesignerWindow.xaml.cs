using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JNN.Stair.Core;
using JNN.Stair.ViewModels;

namespace JNN.Stair.Views
{
    public partial class StairDesignerWindow : Window
    {
        public StairDesignerViewModel ViewModel { get; }

        public StairDesignerWindow(StairDesignerViewModel? viewModel = null)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new StairDesignerViewModel();
            DataContext = ViewModel;

            ViewModel.RequestRedraw += OnRequestRedraw;
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnUpdatePreview_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateCalculations();
            RedrawActiveCanvas();
        }

        private void BtnFitPlan_Click(object sender, RoutedEventArgs e)
        {
            StairPlan2DRenderer.RenderPlan(PlanCanvas, ViewModel.Layout);
        }

        private void BtnFitSection_Click(object sender, RoutedEventArgs e)
        {
            StairSection2DRenderer.RenderSection(SectionCanvas, ViewModel.Layout, ViewModel.SelectedSectionIndex);
        }

        private void PlanCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StairPlan2DRenderer.RenderPlan(PlanCanvas, ViewModel.Layout);
        }

        private void SectionCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            StairSection2DRenderer.RenderSection(SectionCanvas, ViewModel.Layout, ViewModel.SelectedSectionIndex);
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RedrawActiveCanvas();
        }

        private void OnRequestRedraw()
        {
            Dispatcher.InvokeAsync(RedrawActiveCanvas);
        }

        private void RedrawActiveCanvas()
        {
            if (MainTabControl.SelectedIndex == 0 && PlanCanvas != null)
            {
                StairPlan2DRenderer.RenderPlan(PlanCanvas, ViewModel.Layout);
            }
            else if (MainTabControl.SelectedIndex == 1 && SectionCanvas != null)
            {
                StairSection2DRenderer.RenderSection(SectionCanvas, ViewModel.Layout, ViewModel.SelectedSectionIndex);
            }
        }

        private void BtnCreateAndPlace_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnExportRfa_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng xuất Family (.rfa) sẵn sàng khi chạy trong môi trường Revit.", 
                            "JNN Stair", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
