using System;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JNNTool.Tools.PileLocation.ViewModels
{
    public partial class PileLocationViewModel : ObservableObject
    {
        // ── Internal refs ─────────────────────────────────────────────────────
        public Document   Doc       { get; }
        public ElementId  RefPileId { get; }

        private readonly ExternalEvent _externalEvent;
        private readonly Action        _closeAction;

        // ── Tab 1 – Info ──────────────────────────────────────────────────────
        private string _refFamilyName = "—";
        public string RefFamilyName
        {
            get => _refFamilyName;
            set => SetProperty(ref _refFamilyName, value);
        }

        private string _refTypeName = "—";
        public string RefTypeName
        {
            get => _refTypeName;
            set => SetProperty(ref _refTypeName, value);
        }

        // Preview: toạ độ tính từ model (để so sánh)
        private string _surveyX = "—";
        public string SurveyX
        {
            get => _surveyX;
            set => SetProperty(ref _surveyX, value);
        }

        private string _surveyY = "—";
        public string SurveyY
        {
            get => _surveyY;
            set => SetProperty(ref _surveyY, value);
        }

        // Giá trị hiện tại trong parameter
        private string _refParamX = "—";
        public string RefParamX
        {
            get => _refParamX;
            set => SetProperty(ref _refParamX, value);
        }

        private string _refParamY = "—";
        public string RefParamY
        {
            get => _refParamY;
            set => SetProperty(ref _refParamY, value);
        }

        // ── Tab 2 – Results ───────────────────────────────────────────────────
        private int _updatedCount;
        public int UpdatedCount
        {
            get => _updatedCount;
            set => SetProperty(ref _updatedCount, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        // ── Shared ───────────────────────────────────────────────────────────
        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private SolidColorBrush _statusColor = new SolidColorBrush(Colors.White);
        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public PileAction PendingAction { get; set; } = PileAction.None;

        // ── Commands ─────────────────────────────────────────────────────────
        public ICommand WriteReferencePileCommand { get; }
        public ICommand UpdateAllPilesCommand     { get; }
        public ICommand CloseCommand              { get; }

        // ─────────────────────────────────────────────────────────────────────
        public PileLocationViewModel(
            Document      doc,
            Element       refPile,
            ExternalEvent externalEvent,
            Action        closeAction)
        {
            Doc            = doc;
            RefPileId      = refPile.Id;
            _externalEvent = externalEvent;
            _closeAction   = closeAction;

            WriteReferencePileCommand = new RelayCommand(WriteReferencePile);
            UpdateAllPilesCommand     = new RelayCommand(UpdateAllPiles);
            CloseCommand              = new RelayCommand(() => _closeAction?.Invoke());

            var fi = refPile as FamilyInstance;
            RefFamilyName = fi?.Symbol?.Family?.Name ?? "—";
            RefTypeName   = fi?.Symbol?.Name          ?? "—";

            // Đọc giá trị hiện tại của parameter
            RefParamX = ReadParamString(refPile, "X Coordinate");
            RefParamY = ReadParamString(refPile, "Y Coordinate");

            // Tính toạ độ khảo sát thực tế để preview
            if (refPile.Location is LocationPoint lp)
            {
                try
                {
                    var pos = doc.ActiveProjectLocation.GetProjectPosition(lp.Point);
                    SurveyX = (pos.NorthSouth * 0.3048).ToString("F2");
                    SurveyY = (pos.EastWest   * 0.3048).ToString("F2");
                }
                catch { SurveyX = SurveyY = "—"; }
            }
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private void WriteReferencePile()
        {
            PendingAction = PileAction.WriteReferencePile;
            IsBusy        = true;
            StatusMessage = "Đang ghi toạ độ vào cọc mẫu…";
            _externalEvent.Raise();
        }

        private void UpdateAllPiles()
        {
            PendingAction = PileAction.UpdateAllPiles;
            IsBusy        = true;
            StatusMessage = "Đang cập nhật toạ độ…";
            _externalEvent.Raise();
        }

        // ── Callbacks từ handler ─────────────────────────────────────────────

        public void SetStatus(string msg, bool isError = false)
        {
            IsBusy        = false;
            StatusMessage = msg;
            StatusColor   = isError
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 139, 168))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(166, 227, 161));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string ReadParamString(Element elem, string name)
        {
            var p = elem.LookupParameter(name);
            if (p == null) return "—";
            return p.StorageType switch
            {
                StorageType.Double => p.AsDouble().ToString("F2"),
                StorageType.String => p.AsString() ?? "—",
                _                  => "—"
            };
        }
    }
}
