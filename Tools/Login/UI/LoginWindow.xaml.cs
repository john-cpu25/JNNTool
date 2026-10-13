using System;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.UI;
using JNNTool.Core.UI;
using JNNTool.Tools.Login.ViewModels;

namespace JNNTool.Tools.Login.UI
{
    public partial class LoginWindow : Window
    {
        private static LoginWindow _instance;

        public LoginViewModel ViewModel { get; }

        public LoginWindow()
        {
            InitializeComponent();
            ViewModel = new LoginViewModel();
            DataContext = ViewModel;

            ViewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose(bool isSuccess)
        {
            _instance = null;
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void LanguagePill_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.ToggleLanguageCommand?.Execute(null);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowWindow(UIApplication uiApp = null)
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new LoginWindow();
                WpfWindowHelper.ShowModeless(_instance, uiApp);
            }
            else
            {
                if (_instance.WindowState == WindowState.Minimized)
                    _instance.WindowState = WindowState.Normal;
                _instance.Activate();
                _instance.Focus();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _instance = null;
        }
    }
}
