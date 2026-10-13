using System;
using System.Windows;
using System.Windows.Input;
using JNNTool.Tools.Login.ViewModels;

namespace JNNTool.Tools.Login.UI
{
    public partial class LicenseStatusWindow : Window
    {
        private static LicenseStatusWindow _instance;

        public LicenseStatusViewModel ViewModel { get; }

        public LicenseStatusWindow()
        {
            InitializeComponent();
            ViewModel = new LicenseStatusViewModel();
            DataContext = ViewModel;

            ViewModel.RequestClose += () =>
            {
                _instance = null;
                Close();
            };

            ViewModel.RequestReactivate += () =>
            {
                _instance = null;
                Close();
                LoginWindow.ShowWindow();
            };
        }

        public static void ShowWindow()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new LicenseStatusWindow();
                _instance.Show();
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
