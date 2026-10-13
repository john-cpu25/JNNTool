using System;
using System.Windows;
using JNNTool.Tools.Login.Models;
using JNNTool.Tools.Login.ViewModels;

namespace JNNTool.Tools.Login.UI
{
    public partial class LicenseExpiredWindow : Window
    {
        private static LicenseExpiredWindow _instance;

        public LicenseExpiredViewModel ViewModel { get; }

        public LicenseExpiredWindow(ClientLicenseInfo info = null)
        {
            InitializeComponent();
            ViewModel = new LicenseExpiredViewModel(info);
            DataContext = ViewModel;

            ViewModel.RequestReactivate += () =>
            {
                _instance = null;
                Close();
                LoginWindow.ShowWindow();
            };

            ViewModel.RequestClose += () =>
            {
                _instance = null;
                Close();
            };
        }

        public static void ShowExpiredWindow(ClientLicenseInfo info = null)
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new LicenseExpiredWindow(info);
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
