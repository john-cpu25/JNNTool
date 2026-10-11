using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using JNN.Stair.Models;

namespace JNN.Stair.ViewModels
{
    /// <summary>
    /// ViewModel điều khiển toàn diện cửa sổ HT/JNN Stair Family Designer
    /// </summary>
    public class StairDesignerViewModel : INotifyPropertyChanged
    {
        private StairLayout _layout = new();

        public StairLayout Layout
        {
            get => _layout;
            set
            {
                _layout = value;
                OnPropertyChanged();
                UpdateCalculations();
            }
        }

        // 1. Phân loại & Preset
        public ObservableCollection<string> OutputObjectTypes { get; } = new()
        {
            "Family bê tông (Generic Model)"
        };
        public string SelectedOutputType { get; set; } = "Family bê tông (Generic Model)";

        public ObservableCollection<string> PresetOptions { get; } = new()
        {
            "1 vế thẳng (Straight)",
            "2 vế chữ L (L-Shape)",
            "2 vế chữ U (U-Shape)",
            "2 vế chữ L - Chẻ bậc (L-Winder)",
            "3 vế chữ U - 2 Góc chẻ (3-Flight U-Winder)",
            "3 vế chữ U - 2 Chiếu nghỉ (3-Flight U)"
        };

        private string _selectedPresetText = "3 vế chữ U - 2 Chiếu nghỉ (3-Flight U)";
        public string SelectedPresetText
        {
            get => _selectedPresetText;
            set
            {
                if (_selectedPresetText != value)
                {
                    _selectedPresetText = value;
                    OnPropertyChanged();
                    SyncPresetWithLayout();
                    UpdateCalculations();
                }
            }
        }

        public ObservableCollection<string> InputModeOptions { get; } = new()
        {
            "Theo cổ bậc giả định (mm)",
            "Theo số bậc từng vế (bậc)",
            "Theo chiều dài vế thang (mm)"
        };

        private string _selectedInputModeText = "Theo số bậc từng vế (bậc)";
        public string SelectedInputModeText
        {
            get => _selectedInputModeText;
            set
            {
                if (_selectedInputModeText != value)
                {
                    _selectedInputModeText = value;
                    OnPropertyChanged();
                    SyncInputModeWithLayout();
                    UpdateCalculations();
                }
            }
        }

        // Mặt cắt hiển thị cho Tab 2
        public ObservableCollection<string> AvailableSections { get; } = new()
        {
            "Mặt cắt 1-1 (Vế 1 & Chiếu nghỉ 1)",
            "Mặt cắt 2-2 (Vế 2 Giữa 2 Chiếu nghỉ)",
            "Mặt cắt 3-3 (Vế 3 & Chiếu nghỉ 2)"
        };

        private string _selectedSection = "Mặt cắt 1-1 (Vế 1 & Chiếu nghỉ 1)";
        public string SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection = value;
                    OnPropertyChanged();
                    RequestRedraw?.Invoke();
                }
            }
        }

        public int SelectedSectionIndex => SelectedSection.Contains("2-2") ? 2 : (SelectedSection.Contains("3-3") ? 3 : 1);

        // Hướng đặt mặt bằng
        public double PlanRotationDeg { get; set; } = 0.0;
        public bool PlanMirrored { get; set; } = false;
        public bool ReverseDirection { get; set; } = false;

        // Hoàn thiện
        public double TileFinishMm { get; set; } = 0.0;
        public double RiserFinishMm { get; set; } = 0.0;
        public double NosingMm { get; set; } = 0.0;

        // Tab 4: Tạo View Thang
        public string PlanNamePattern { get; set; } = "MẶT BẰNG THANG {Level}";
        public string SectionNamePattern { get; set; } = "THANG {Level} MC{Index}";
        public int PlanScale { get; set; } = 50;
        public int SectionScale { get; set; } = 25;
        public double CropOffsetMm { get; set; } = 500.0;
        public double FarClipOffsetMm { get; set; } = 1500.0;
        public double TextOffsetMm { get; set; } = 250.0;
        public bool IncludeTextNotes { get; set; } = true;
        public bool IncludeDirectionArrow { get; set; } = true;

        // Bottom right controls
        public string TypeNameToLoad { get; set; } = "Thang U Ba Ve Chieu N";

        // Thống kê nhanh (Summary Box)
        private string _summaryText = string.Empty;
        public string SummaryText
        {
            get => _summaryText;
            private set { _summaryText = value; OnPropertyChanged(); }
        }

        public event Action? RequestRedraw;

        public StairDesignerViewModel()
        {
            Layout.Preset = StairPreset.ThreeFlightU;
            Layout.InputMode = StairInputMode.ByStepCount;
            Layout.CustomRisersFlight1 = 11;
            Layout.CustomRisersFlight2 = 11;
            Layout.CustomRisersFlight3 = 4;
            Layout.TreadMm = 300.0;
            Layout.WaistThicknessMm = 150.0;
            Layout.LandingDepthMm = 1200.0;
            Layout.LandingBeamWidthMm = 200.0;
            Layout.LandingBeamHeightMm = 300.0;

            UpdateCalculations();
        }

        public void SyncPresetWithLayout()
        {
            Layout.Preset = SelectedPresetText switch
            {
                var s when s.Contains("1 vế") => StairPreset.Straight,
                var s when s.Contains("L-Shape") => StairPreset.LShape,
                var s when s.Contains("U-Shape") => StairPreset.UShape,
                var s when s.Contains("L-Winder") => StairPreset.LShapeWinder,
                var s when s.Contains("3-Flight U-Winder") => StairPreset.ThreeFlightUWinder,
                _ => StairPreset.ThreeFlightU
            };

            Layout.FlightCount = Layout.Preset switch
            {
                StairPreset.Straight => 1,
                StairPreset.ThreeFlightU or StairPreset.ThreeFlightUWinder => 3,
                _ => 2
            };

            TypeNameToLoad = Layout.Preset switch
            {
                StairPreset.Straight => $"Thang Thang {Layout.FloorToFloorMm:0}x{Layout.StairWidthMm:0}",
                StairPreset.LShape => $"Thang L {Layout.FloorToFloorMm:0}x{Layout.StairWidthMm:0}",
                StairPreset.UShape => $"Thang U {Layout.FloorToFloorMm:0}x{Layout.StairWidthMm:0}",
                StairPreset.LShapeWinder => "Thang L Re Quat",
                StairPreset.ThreeFlightUWinder => "Thang U Ba Ve Re Quat",
                _ => "Thang U Ba Ve Chieu N"
            };
            OnPropertyChanged(nameof(TypeNameToLoad));
        }

        public void SyncInputModeWithLayout()
        {
            Layout.InputMode = SelectedInputModeText switch
            {
                var s when s.Contains("cổ bậc giả định") => StairInputMode.ByPreferredRiser,
                var s when s.Contains("chiều dài") => StairInputMode.ByFlightLength,
                _ => StairInputMode.ByStepCount
            };
        }

        public void UpdateCalculations()
        {
            var risers = Layout.GetRisersPerFlight();
            int total = Layout.TotalRisers;
            double h = Layout.ActualRiserMm;
            double b = Layout.TreadMm;
            double blondel = 2 * h + b;

            var sb = new StringBuilder();
            sb.AppendLine($"• Tổng số cổ bậc: {total}");
            sb.AppendLine($"• Cổ bậc thực h = {h:F1} mm");
            sb.AppendLine($"• Mặt bậc b = {b:F0} mm");

            if (risers.Count == 1)
                sb.AppendLine($"• Phân bổ cổ bậc: {risers[0]}");
            else if (risers.Count == 2)
                sb.AppendLine($"• Phân bổ cổ bậc: {risers[0]} / {risers[1]}");
            else
                sb.AppendLine($"• Phân bổ cổ bậc: {risers[0]} / {risers[1]} / {risers[2]}");

            int landingCount = Layout.Preset switch
            {
                StairPreset.Straight => 0,
                StairPreset.ThreeFlightU or StairPreset.ThreeFlightUWinder => 2,
                _ => 1
            };
            sb.AppendLine($"• Chiếu nghỉ / rẻ quạt: {landingCount} bậc");
            sb.AppendLine($"• Công thức 2h + b = {blondel:F0} mm ({(blondel >= 590 && blondel <= 650 ? "Đạt" : "Xem lại")})");

            double spanX = (risers.Count > 1 ? risers[1] * b : 0) + 2 * Layout.LandingDepthMm;
            double spanY = risers[0] * b + Layout.LandingDepthMm;
            sb.AppendLine($"• Khẩu độ mặt bằng: {spanX:N0} x {spanY:N0} mm");

            SummaryText = sb.ToString();
            RequestRedraw?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
