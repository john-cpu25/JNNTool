using System.Windows;
using JNNTool.Tools.StairModel.ViewModels;

namespace JNNTool.Tools.StairModel.Views
{
    public partial class StairModelWindow : Window
    {
        private readonly StairModelViewModel _viewModel;

        public StairModelWindow(StairModelViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.RequestPreviewUpdate += OnRequestPreviewUpdate;
            _viewModel.RequestClose += Close;

            Loaded += (s, e) =>
            {
                PreviewCanvas.RenderPreview(_viewModel.GeometryResult);
            };
        }

        private void OnRequestPreviewUpdate()
        {
            Dispatcher.Invoke(() =>
            {
                PreviewCanvas.RenderPreview(_viewModel.GeometryResult);
            });
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.RequestPreviewUpdate -= OnRequestPreviewUpdate;
            base.OnClosed(e);
        }
    }
}
