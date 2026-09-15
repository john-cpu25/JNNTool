using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.ClashDetection.Models;

namespace JNNTool.Tools.ClashDetection.ViewModels
{
    public class ClashDetectionViewModel : ObservableObject
    {
        private ClashDetectionExternalEventHandler _handler;

        private ObservableCollection<RevitLinkInstance> _links;
        public ObservableCollection<RevitLinkInstance> Links
        {
            get => _links;
            set => SetProperty(ref _links, value);
        }

        private RevitLinkInstance _selectedLink;
        public RevitLinkInstance SelectedLink
        {
            get => _selectedLink;
            set => SetProperty(ref _selectedLink, value);
        }

        private bool _isLinkSelected = true;
        public bool IsLinkSelected
        {
            get => _isLinkSelected;
            set => SetProperty(ref _isLinkSelected, value);
        }

        private ObservableCollection<Category> _linkCategories;
        public ObservableCollection<Category> LinkCategories
        {
            get => _linkCategories;
            set => SetProperty(ref _linkCategories, value);
        }

        private Category _selectedLinkCategory;
        public Category SelectedLinkCategory
        {
            get => _selectedLinkCategory;
            set => SetProperty(ref _selectedLinkCategory, value);
        }

        private ObservableCollection<ClashResult> _results = new ObservableCollection<ClashResult>();
        public ObservableCollection<ClashResult> Results
        {
            get => _results;
            set => SetProperty(ref _results, value);
        }

        private string _statusMessage = "Ready. Select a Link and Categories.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RunCheckCommand { get; }
        public ICommand ShowClashCommand { get; }

        public ClashDetectionViewModel(ClashDetectionExternalEventHandler handler, Document doc)
        {
            _handler = handler;
            _handler.ViewModel = this;

            RunCheckCommand = new RelayCommand(RunCheck);
            ShowClashCommand = new RelayCommand<ClashResult>(ShowClash);

            // Load Links
            var linkInstances = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();
            Links = new ObservableCollection<RevitLinkInstance>(linkInstances);
            if (Links.Count > 0) SelectedLink = Links[0];

            // Load Categories
            LoadCategories(doc);
        }

        private void LoadCategories(Document doc)
        {
            var categories = doc.Settings.Categories
                .Cast<Category>()
                .Where(c => c.CategoryType == CategoryType.Model)
                .OrderBy(c => c.Name)
                .ToList();
            
            LinkCategories = new ObservableCollection<Category>(categories);
        }

        public List<Element> GetHostElements(Document doc)
        {
            return new FilteredElementCollector(doc, doc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .ToElements()
                .ToList();
        }

        public void UpdateResults(List<ClashResult> newResults)
        {
            Results.Clear();
            foreach (var res in newResults) Results.Add(res);
        }

        private void RunCheck()
        {
            StatusMessage = "Checking...";
            _handler.RequestAction = "RUN_CHECK";
            _handler.Raise();
        }

        private void ShowClash(ClashResult result)
        {
            if (result == null) return;
            _handler.RequestAction = "SHOW_CLASH";
            _handler.SelectedClash = result;
            _handler.Raise();
        }
    }
}
