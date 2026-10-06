using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.Compat;
using JNNTool.Core.ExternalEvents;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.UI.ViewModels
{
    public class PropertyItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Group { get; set; } = "Thông số";

        public PropertyItem(string name, string value, string group = "Thông số")
        {
            Name = name;
            Value = value;
            Group = group;
        }
    }

    public class PropertyViewerViewModel : ObservableObject
    {
        private string _elementName = "Chưa chọn cấu kiện";
        public string ElementName
        {
            get => _elementName;
            set => SetProperty(ref _elementName, value);
        }

        private string _elementCategory = "-";
        public string ElementCategory
        {
            get => _elementCategory;
            set => SetProperty(ref _elementCategory, value);
        }

        private string _source = "-";
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private string _sourceId = "-";
        public string SourceId
        {
            get => _sourceId;
            set => SetProperty(ref _sourceId, value);
        }

        private string _section = "-";
        public string Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        private string _material = "-";
        public string Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }

        private string _story = "-";
        public string Story
        {
            get => _story;
            set => SetProperty(ref _story, value);
        }

        private string _sourceHash = "-";
        public string SourceHash
        {
            get => _sourceHash;
            set => SetProperty(ref _sourceHash, value);
        }

        private string _status = "-";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ObservableCollection<PropertyItem> AllProperties { get; } = new();
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        public ICommand PickElementCommand { get; }

        public PropertyViewerViewModel()
        {
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            PickElementCommand = new RelayCommand(OnPickElement);
        }

        private void RunOnUi(Action action)
        {
            if (_dispatcher == null || _dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            {
                return;
            }

            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _dispatcher.BeginInvoke(action);
            }
        }

        public void LoadFromElement(Element element)
        {
            if (element == null) return;

            ElementName = $"{element.Name} (ID: {element.Id.GetIdValue()})";
            ElementCategory = element.Category?.Name ?? "-";

            Source = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSource);
            SourceId = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamEtabsId);
            Section = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSection);
            Material = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamMaterial);
            Story = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamStory);
            SourceHash = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamSourceHash);
            Status = SharedParameterManager.GetParameterValue(element, SharedParameterManager.ParamConversionStatus);

            if (string.IsNullOrWhiteSpace(Source)) Source = "Không rõ (Chưa gắn JNN)";
            if (string.IsNullOrWhiteSpace(SourceId)) SourceId = "-";
            if (string.IsNullOrWhiteSpace(Section)) Section = "-";
            if (string.IsNullOrWhiteSpace(Material)) Material = "-";
            if (string.IsNullOrWhiteSpace(Story)) Story = "-";

            AllProperties.Clear();
            foreach (Parameter p in element.Parameters)
            {
                if (p.HasValue && !string.IsNullOrWhiteSpace(p.Definition?.Name))
                {
                    string valStr = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString("F2");
                    AllProperties.Add(new PropertyItem(p.Definition.Name, valStr));
                }
            }
        }

        private void OnPickElement()
        {
            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var uidoc = app.ActiveUIDocument;
                    if (uidoc == null) return;

                    Reference picked = uidoc.Selection.PickObject(ObjectType.Element, "Chọn một cấu kiện để xem thông tin nguồn ETABS / IFC");
                    if (picked != null)
                    {
                        Element el = uidoc.Document.GetElement(picked);
                        RunOnUi(() =>
                        {
                            LoadFromElement(el);
                        });
                    }
                }
                catch { }
            });
        }
    }
}
