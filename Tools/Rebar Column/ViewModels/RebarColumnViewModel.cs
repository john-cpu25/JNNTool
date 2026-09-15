using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Tools.RebarColumn.Models;
using Autodesk.Revit.UI;

namespace JNNTool.Tools.RebarColumn.ViewModels
{
    public class RebarColumnViewModel : ObservableObject
    {
        private ExternalEvent _externalEvent;

        public RebarColumnViewModel(ExternalEvent externalEvent = null)
        {
            _externalEvent = externalEvent;

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ColumnWidth):
                    case nameof(ColumnDepth):
                        UpdatePreviewSize();
                        UpdateCalculations();
                        break;
                    case nameof(CountX):
                    case nameof(CountY):
                        UpdateCalculations();
                        UpdatePreview();
                        break;
                    case nameof(SelectedPattern):
                        OnPropertyChanged(nameof(IsStandardPattern));
                        OnPropertyChanged(nameof(IsInternalLinksPattern));
                        UpdateCalculations();
                        UpdatePreview();
                        break;
                    case nameof(Cover):
                        UpdatePreview();
                        break;
                    case nameof(MainDiameter):
                        UpdateCalculations();
                        UpdatePreview();
                        break;
                    case nameof(StirrupDiameter):
                        UpdatePreview();
                        break;
                    case nameof(IsSeismic):
                        OnPropertyChanged(nameof(IsUniform));
                        UpdatePreview();
                        break;
                    case nameof(ApplyAllAdditionalStirrup):
                        OnPropertyChanged(nameof(IsNotApplyAllAdditionalStirrup));
                        break;
                }
            };

            UpdateCalculations();
            UpdatePreview();
        }

        // Column Geometry (read from Revit)
        private string _columnName = "";
        public string ColumnName
        {
            get => _columnName;
            set => SetProperty(ref _columnName, value);
        }
        private double _columnWidth = 0;
        public double ColumnWidth
        {
            get => _columnWidth;
            set => SetProperty(ref _columnWidth, value);
        } // mm
        private double _columnDepth = 0;
        public double ColumnDepth
        {
            get => _columnDepth;
            set => SetProperty(ref _columnDepth, value);
        } // mm
        private double _columnHeight = 0;
        public double ColumnHeight
        {
            get => _columnHeight;
            set => SetProperty(ref _columnHeight, value);
        } // mm
        private double _cover = 30;
        public double Cover
        {
            get => _cover;
            set => SetProperty(ref _cover, value);
        } // mm
        private int _countX = 3;
        public int CountX
        {
            get => _countX;
            set => SetProperty(ref _countX, value);
        }
        private int _countY = 3;
        public int CountY
        {
            get => _countY;
            set => SetProperty(ref _countY, value);
        }
        private double _mainDiameter = 20;
        public double MainDiameter
        {
            get => _mainDiameter;
            set => SetProperty(ref _mainDiameter, value);
        }
        private double _stirrupDiameter = 10;
        public double StirrupDiameter
        {
            get => _stirrupDiameter;
            set => SetProperty(ref _stirrupDiameter, value);
        }
        private double _lapFactor = 40;
        public double LapFactor
        {
            get => _lapFactor;
            set => SetProperty(ref _lapFactor, value);
        }
        private bool _isSeismic = false;
        public bool IsSeismic
        {
            get => _isSeismic;
            set => SetProperty(ref _isSeismic, value);
        }
        private double _spacing1 = 100;
        public double Spacing1
        {
            get => _spacing1;
            set => SetProperty(ref _spacing1, value);
        }
        private double _spacing2 = 200;
        public double Spacing2
        {
            get => _spacing2;
            set => SetProperty(ref _spacing2, value);
        }
        private double _spacing3 = 100;
        public double Spacing3
        {
            get => _spacing3;
            set => SetProperty(ref _spacing3, value);
        }
        private StirrupPattern _selectedPattern = StirrupPattern.Standard;
        public StirrupPattern SelectedPattern
        {
            get => _selectedPattern;
            set => SetProperty(ref _selectedPattern, value);
        }
        private double _slabHeight = 300;
        public double SlabHeight
        {
            get => _slabHeight;
            set => SetProperty(ref _slabHeight, value);
        } // mm
        private double _topCover = 25;
        public double TopCover
        {
            get => _topCover;
            set => SetProperty(ref _topCover, value);
        } // mm
        private double _otherCover = 25;
        public double OtherCover
        {
            get => _otherCover;
            set => SetProperty(ref _otherCover, value);
        } // mm

        public ObservableCollection<StirrupPattern> Patterns { get; } = new ObservableCollection<StirrupPattern>(
            Enum.GetValues(typeof(StirrupPattern)).Cast<StirrupPattern>()
        );

        public ObservableCollection<double> MainDiameters { get; } = new ObservableCollection<double> { 16, 18, 20, 22, 25 };
        public ObservableCollection<double> StirrupDiameters { get; } = new ObservableCollection<double> { 8, 10, 12 };
        private ObservableCollection<string> _availableShapes = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableShapes
        {
            get => _availableShapes;
            set => SetProperty(ref _availableShapes, value);
        }
        private string _selectedStirrupShapeName = "";
        public string SelectedStirrupShapeName
        {
            get => _selectedStirrupShapeName;
            set => SetProperty(ref _selectedStirrupShapeName, value);
        }

        // Properties for UI Preview
        private ObservableCollection<Point> _barPositions = new ObservableCollection<Point>();
        public ObservableCollection<Point> BarPositions
        {
            get => _barPositions;
            set => SetProperty(ref _barPositions, value);
        }
        private ObservableCollection<PointCollection> _stirrupLines = new ObservableCollection<PointCollection>();
        public ObservableCollection<PointCollection> StirrupLines
        {
            get => _stirrupLines;
            set => SetProperty(ref _stirrupLines, value);
        }
        private double _previewWidth = 200;
        public double PreviewWidth
        {
            get => _previewWidth;
            set => SetProperty(ref _previewWidth, value);
        }
        private double _previewHeight = 300;
        public double PreviewHeight
        {
            get => _previewHeight;
            set => SetProperty(ref _previewHeight, value);
        }

        // Tab 2: Thép móng (Foundation Rebar)
        private bool _enableFoundationRebar = true;
        public bool EnableFoundationRebar
        {
            get => _enableFoundationRebar;
            set => SetProperty(ref _enableFoundationRebar, value);
        }
        private bool _splitRebarAtFooting = false;
        public bool SplitRebarAtFooting
        {
            get => _splitRebarAtFooting;
            set => SetProperty(ref _splitRebarAtFooting, value);
        }
        private bool _useStirrupSpacing = true;
        public bool UseStirrupSpacing
        {
            get => _useStirrupSpacing;
            set => SetProperty(ref _useStirrupSpacing, value);
        }
        private double _footingStirrupSpacing = 200;
        public double FootingStirrupSpacing
        {
            get => _footingStirrupSpacing;
            set => SetProperty(ref _footingStirrupSpacing, value);
        }
        private double _footingDepthHm = 580;
        public double FootingDepthHm
        {
            get => _footingDepthHm;
            set => SetProperty(ref _footingDepthHm, value);
        }
        private double _footingBendLb = 300;
        public double FootingBendLb
        {
            get => _footingBendLb;
            set => SetProperty(ref _footingBendLb, value);
        }
        private void DirectionUp()
        {
            MessageBox.Show("Hướng thép móng: Lên trên", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void DirectionDown()
        {
            MessageBox.Show("Hướng thép móng: Xuống dưới", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void DirectionLeft()
        {
            MessageBox.Show("Hướng thép móng: Sang trái", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void DirectionRight()
        {
            MessageBox.Show("Hướng thép móng: Sang phải", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Tab 3: Thép đai (Stirrups)
        private bool _spacingToBeamBottom = true;
        public bool SpacingToBeamBottom
        {
            get => _spacingToBeamBottom;
            set => SetProperty(ref _spacingToBeamBottom, value);
        }
        private bool _spacingToMaxTop = false;
        public bool SpacingToMaxTop
        {
            get => _spacingToMaxTop;
            set => SetProperty(ref _spacingToMaxTop, value);
        }
        private bool _seismicSpacingByDist = true;
        public bool SeismicSpacingByDist
        {
            get => _seismicSpacingByDist;
            set => SetProperty(ref _seismicSpacingByDist, value);
        }
        private double _seismicSpacingVal = 200;
        public double SeismicSpacingVal
        {
            get => _seismicSpacingVal;
            set => SetProperty(ref _seismicSpacingVal, value);
        }
        private bool _seismicSpacingByCount = false;
        public bool SeismicSpacingByCount
        {
            get => _seismicSpacingByCount;
            set => SetProperty(ref _seismicSpacingByCount, value);
        }
        private int _seismicCountVal = 3;
        public int SeismicCountVal
        {
            get => _seismicCountVal;
            set => SetProperty(ref _seismicCountVal, value);
        }

        public ObservableCollection<int> SeismicCounts { get; } = new ObservableCollection<int> { 2, 3, 4, 5 };
        private double _stirrupStartOffset = 50;
        public double StirrupStartOffset
        {
            get => _stirrupStartOffset;
            set => SetProperty(ref _stirrupStartOffset, value);
        }
        private bool _spacingUniform = false;
        public bool SpacingUniform
        {
            get => _spacingUniform;
            set => SetProperty(ref _spacingUniform, value);
        }
        private bool _spacingVariable = true;
        public bool SpacingVariable
        {
            get => _spacingVariable;
            set => SetProperty(ref _spacingVariable, value);
        }
        private double _spacingValA1 = 100;
        public double SpacingValA1
        {
            get => _spacingValA1;
            set => SetProperty(ref _spacingValA1, value);
        }
        private double _spacingValA2 = 200;
        public double SpacingValA2
        {
            get => _spacingValA2;
            set => SetProperty(ref _spacingValA2, value);
        }
        private bool _l1UseMinVal = true;
        public bool L1UseMinVal
        {
            get => _l1UseMinVal;
            set => SetProperty(ref _l1UseMinVal, value);
        }
        private double _l1MinVal = 450;
        public double L1MinVal
        {
            get => _l1MinVal;
            set => SetProperty(ref _l1MinVal, value);
        }
        private bool _l1UseHeightDiv = true;
        public bool L1UseHeightDiv
        {
            get => _l1UseHeightDiv;
            set => SetProperty(ref _l1UseHeightDiv, value);
        }
        private double _l1HeightDivVal = 5;
        public double L1HeightDivVal
        {
            get => _l1HeightDivVal;
            set => SetProperty(ref _l1HeightDivVal, value);
        }
        private bool _l1UseColSection = true;
        public bool L1UseColSection
        {
            get => _l1UseColSection;
            set => SetProperty(ref _l1UseColSection, value);
        }
        private bool _applyAllAdditionalStirrup = true;
        public bool ApplyAllAdditionalStirrup
        {
            get => _applyAllAdditionalStirrup;
            set => SetProperty(ref _applyAllAdditionalStirrup, value);
        }


        public bool IsNotApplyAllAdditionalStirrup => !ApplyAllAdditionalStirrup;
        private double _additionalStirrupDiameter = 6;
        public double AdditionalStirrupDiameter
        {
            get => _additionalStirrupDiameter;
            set => SetProperty(ref _additionalStirrupDiameter, value);
        }
        private double _additionalStirrupSpacing = 300;
        public double AdditionalStirrupSpacing
        {
            get => _additionalStirrupSpacing;
            set => SetProperty(ref _additionalStirrupSpacing, value);
        }

        // Tab 4: Triển khai (Detailing / Sheets Layout)
        private bool _enableDetailingLayout = true;
        public bool EnableDetailingLayout
        {
            get => _enableDetailingLayout;
            set => SetProperty(ref _enableDetailingLayout, value);
        }

        public ObservableCollection<string> SheetNames { get; } = new ObservableCollection<string> { "CHI TIẾT CỘT", "CHI TIẾT CỘT 1", "CHI TIẾT CỘT 2" };
        private string _selectedSheetName = "CHI TIẾT CỘT";
        public string SelectedSheetName
        {
            get => _selectedSheetName;
            set => SetProperty(ref _selectedSheetName, value);
        }

        public ObservableCollection<string> SheetNumbers { get; } = new ObservableCollection<string> { "S4-00.05", "S4-00.06", "S4-00.07" };
        private string _selectedSheetNumber = "S4-00.05";
        public string SelectedSheetNumber
        {
            get => _selectedSheetNumber;
            set => SetProperty(ref _selectedSheetNumber, value);
        }

        public ObservableCollection<string> TitleBlocks { get; } = new ObservableCollection<string> { "A1 metric", "A0 metric", "A2 metric", "A3 metric" };
        private string _selectedTitleBlock = "A1 metric";
        public string SelectedTitleBlock
        {
            get => _selectedTitleBlock;
            set => SetProperty(ref _selectedTitleBlock, value);
        }
        private double _crossSectionScale = 25;
        public double CrossSectionScale
        {
            get => _crossSectionScale;
            set => SetProperty(ref _crossSectionScale, value);
        }

        public ObservableCollection<string> ViewTemplates { get; } = new ObservableCollection<string> { "@BS-MCN COT", "@BS-MCD COT", "None" };
        private string _crossSectionTemplate = "@BS-MCN COT";
        public string CrossSectionTemplate
        {
            get => _crossSectionTemplate;
            set => SetProperty(ref _crossSectionTemplate, value);
        }

        public ObservableCollection<string> ViewFamilyTypes { get; } = new ObservableCollection<string> { "@RVS-MCN Cột", "@RVS_MCD-Cột", "Section" };
        private string _crossSectionFamilyType = "@RVS-MCN Cột";
        public string CrossSectionFamilyType
        {
            get => _crossSectionFamilyType;
            set => SetProperty(ref _crossSectionFamilyType, value);
        }

        public ObservableCollection<string> DimensionTypes { get; } = new ObservableCollection<string> { "@BS-Dim A3", "@BS-Dim A1", "Linear" };
        private string _crossSectionDimType = "@BS-Dim A3";
        public string CrossSectionDimType
        {
            get => _crossSectionDimType;
            set => SetProperty(ref _crossSectionDimType, value);
        }
        private bool _stretchCrossSectionRebar = true;
        public bool StretchCrossSectionRebar
        {
            get => _stretchCrossSectionRebar;
            set => SetProperty(ref _stretchCrossSectionRebar, value);
        }

        public ObservableCollection<double> TextSizes { get; } = new ObservableCollection<double> { 1.5, 1.8, 2.0, 2.5, 3.0 };
        private double _crossSectionTextSize = 1.8;
        public double CrossSectionTextSize
        {
            get => _crossSectionTextSize;
            set => SetProperty(ref _crossSectionTextSize, value);
        }

        public ObservableCollection<string> StirrupTags { get; } = new ObservableCollection<string> { "A3_P_MRA_SL&DK_BOT", "None" };
        private string _crossSectionStirrupTag = "A3_P_MRA_SL&DK_BOT";
        public string CrossSectionStirrupTag
        {
            get => _crossSectionStirrupTag;
            set => SetProperty(ref _crossSectionStirrupTag, value);
        }
        private bool _crossSectionShowTag = false;
        public bool CrossSectionShowTag
        {
            get => _crossSectionShowTag;
            set => SetProperty(ref _crossSectionShowTag, value);
        }
        private bool _crossSectionShowVal = true;
        public bool CrossSectionShowVal
        {
            get => _crossSectionShowVal;
            set => SetProperty(ref _crossSectionShowVal, value);
        }

        public ObservableCollection<string> TypeTags { get; } = new ObservableCollection<string> { "A3_T_RT_DK&KC_MID", "None" };
        private string _longSectionTypeTag = "A3_T_RT_DK&KC_MID";
        public string LongSectionTypeTag
        {
            get => _longSectionTypeTag;
            set => SetProperty(ref _longSectionTypeTag, value);
        }
        private double _longSectionScale = 50;
        public double LongSectionScale
        {
            get => _longSectionScale;
            set => SetProperty(ref _longSectionScale, value);
        }

        public ObservableCollection<string> BreakLines { get; } = new ObservableCollection<string> { "1-25", "1-50", "None" };
        private string _longSectionBreakLine = "1-25";
        public string LongSectionBreakLine
        {
            get => _longSectionBreakLine;
            set => SetProperty(ref _longSectionBreakLine, value);
        }
        private string _longSectionTemplate = "@BS-MCD COT";
        public string LongSectionTemplate
        {
            get => _longSectionTemplate;
            set => SetProperty(ref _longSectionTemplate, value);
        }
        private string _longSectionFamilyType = "@RVS_MCD-Cột";
        public string LongSectionFamilyType
        {
            get => _longSectionFamilyType;
            set => SetProperty(ref _longSectionFamilyType, value);
        }
        private string _longSectionDimType = "@BS-Dim A3";
        public string LongSectionDimType
        {
            get => _longSectionDimType;
            set => SetProperty(ref _longSectionDimType, value);
        }
        private double _longSectionTextSize = 1.8;
        public double LongSectionTextSize
        {
            get => _longSectionTextSize;
            set => SetProperty(ref _longSectionTextSize, value);
        }

        public ObservableCollection<string> StandardTags { get; } = new ObservableCollection<string> { "A3_T_RT_NhomThepBS", "None" };
        private string _longSectionStandardTag = "A3_T_RT_NhomThepBS";
        public string LongSectionStandardTag
        {
            get => _longSectionStandardTag;
            set => SetProperty(ref _longSectionStandardTag, value);
        }
        private bool _longSectionShowTag = true;
        public bool LongSectionShowTag
        {
            get => _longSectionShowTag;
            set => SetProperty(ref _longSectionShowTag, value);
        }
        private bool _longSectionShowVal = true;
        public bool LongSectionShowVal
        {
            get => _longSectionShowVal;
            set => SetProperty(ref _longSectionShowVal, value);
        }



        private void UpdatePreviewSize()
        {
            if (ColumnWidth > 0 && ColumnDepth > 0)
            {
                // Keep the larger dimension at 200, scale the other proportionally
                double ratio = ColumnWidth / ColumnDepth;
                if (ratio >= 1)
                {
                    PreviewWidth = 200;
                    PreviewHeight = 200 / ratio;
                }
                else
                {
                    PreviewHeight = 200;
                    PreviewWidth = 200 * ratio;
                }
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            BarPositions.Clear();
            StirrupLines.Clear();

            if (CountX < 2) CountX = 2;
            if (CountY < 2) CountY = 2;
            
            double dx = 1.0 / (CountX - 1);
            double dy = 1.0 / (CountY - 1);

            // 1. Calculate Bar Positions (Perimeter only)
            for (int i = 0; i < CountX; i++)
            {
                for (int j = 0; j < CountY; j++)
                {
                    // Only add if it's on the boundary (i is first/last OR j is first/last)
                    if (i == 0 || i == CountX - 1 || j == 0 || j == CountY - 1)
                    {
                        BarPositions.Add(new Point(i * dx, j * dy));
                    }
                }
            }

            // 2. Calculate Stirrup Lines
            // Outer loop
            var outerLoop = new PointCollection
            {
                new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1), new Point(0, 0)
            };
            StirrupLines.Add(outerLoop);

            // Internal Patterns
            if (SelectedPattern == StirrupPattern.WithInternalLinks)
            {
                // Connect internal bars to opposite sides
                for (int i = 1; i < CountX - 1; i++)
                {
                    var link = new PointCollection { new Point(i * dx, 0), new Point(i * dx, 1) };
                    StirrupLines.Add(link);
                }
            }
            else if (SelectedPattern == StirrupPattern.CrossTies)
            {
                for (int i = 1; i < CountX - 1; i++)
                {
                    StirrupLines.Add(new PointCollection { new Point(i * dx, 0), new Point(i * dx, 1) });
                }
                for (int j = 1; j < CountY - 1; j++)
                {
                    StirrupLines.Add(new PointCollection { new Point(0, j * dy), new Point(1, j * dy) });
                }
            }
        }
        private void Generate()
        {
            _externalEvent?.Raise();
        }

        // Calculated properties
        public int TotalMainBars => (2 * CountX + 2 * CountY - 4) >= 4 ? (2 * CountX + 2 * CountY - 4) : 4;

        public string TotalRebarText => $"{TotalMainBars}Ø{MainDiameter:0}";

        public double TotalRebarArea => TotalMainBars * Math.PI * Math.Pow(MainDiameter / 2.0, 2) / 100.0; // cm2

        public string ColumnDimensionsText => $"S = {ColumnWidth:0}x{ColumnDepth:0}";

        public double RebarRatio
        {
            get
            {
                if (ColumnWidth <= 0 || ColumnDepth <= 0) return 0;
                double totalAreaMm2 = TotalMainBars * Math.PI * Math.Pow(MainDiameter / 2.0, 2);
                double colAreaMm2 = ColumnWidth * ColumnDepth;
                return (totalAreaMm2 / colAreaMm2) * 100.0; // %
            }
        }

        public void UpdateCalculations()
        {
            OnPropertyChanged(nameof(TotalMainBars));
            OnPropertyChanged(nameof(TotalRebarText));
            OnPropertyChanged(nameof(TotalRebarArea));
            OnPropertyChanged(nameof(RebarRatio));
            OnPropertyChanged(nameof(ColumnDimensionsText));
        }

        // Radio button binders
        public bool IsUniform
        {
            get => !IsSeismic;
            set
            {
                if (value) IsSeismic = false;
            }
        }

        public bool IsStandardPattern
        {
            get => SelectedPattern == StirrupPattern.Standard;
            set { if (value) SelectedPattern = StirrupPattern.Standard; }
        }

        public bool IsInternalLinksPattern
        {
            get => SelectedPattern == StirrupPattern.WithInternalLinks;
            set { if (value) SelectedPattern = StirrupPattern.WithInternalLinks; }
        }
    }
}

