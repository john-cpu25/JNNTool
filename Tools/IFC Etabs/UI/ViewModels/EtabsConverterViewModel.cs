using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.ExternalEvents;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.ETABS;
using JNNTool.Tools.IFCEtabs.ETABS.Interfaces;
using JNNTool.Tools.IFCEtabs.Revit.Services;

namespace JNNTool.Tools.IFCEtabs.UI.ViewModels
{
    public class EtabsConverterViewModel : ObservableObject
    {
        private readonly IEtabsService _etabsService;
        private StructuralModel? _currentModel;

        private bool _isEtabsMode = true;
        public bool IsEtabsMode
        {
            get => _isEtabsMode;
            set => SetProperty(ref _isEtabsMode, value);
        }

        private bool _isIfcMode = false;
        public bool IsIfcMode
        {
            get => _isIfcMode;
            set => SetProperty(ref _isIfcMode, value);
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        private string _statusText = "Sẵn sàng kết nối ETABS";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _modelName = "Chưa nạp";
        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        private string _units = "N_mm_C";
        public string Units
        {
            get => _units;
            set => SetProperty(ref _units, value);
        }

        private int _levelCount = 0;
        public int LevelCount
        {
            get => _levelCount;
            set => SetProperty(ref _levelCount, value);
        }

        private int _gridCount = 0;
        public int GridCount
        {
            get => _gridCount;
            set => SetProperty(ref _gridCount, value);
        }

        private int _columnCount = 0;
        public int ColumnCount
        {
            get => _columnCount;
            set => SetProperty(ref _columnCount, value);
        }

        private int _beamCount = 0;
        public int BeamCount
        {
            get => _beamCount;
            set => SetProperty(ref _beamCount, value);
        }

        private int _wallCount = 0;
        public int WallCount
        {
            get => _wallCount;
            set => SetProperty(ref _wallCount, value);
        }

        private int _slabCount = 0;
        public int SlabCount
        {
            get => _slabCount;
            set => SetProperty(ref _slabCount, value);
        }

        // Tùy chọn cấu kiện
        private bool _importColumns = true;
        public bool ImportColumns
        {
            get => _importColumns;
            set => SetProperty(ref _importColumns, value);
        }

        private bool _importBeams = true;
        public bool ImportBeams
        {
            get => _importBeams;
            set => SetProperty(ref _importBeams, value);
        }

        private bool _importWalls = true;
        public bool ImportWalls
        {
            get => _importWalls;
            set => SetProperty(ref _importWalls, value);
        }

        private bool _importSlabs = true;
        public bool ImportSlabs
        {
            get => _importSlabs;
            set => SetProperty(ref _importSlabs, value);
        }

        private bool _importLevels = true;
        public bool ImportLevels
        {
            get => _importLevels;
            set => SetProperty(ref _importLevels, value);
        }

        private bool _importGrids = true;
        public bool ImportGrids
        {
            get => _importGrids;
            set => SetProperty(ref _importGrids, value);
        }

        // Tùy chọn chuyển đổi
        private bool _updateExisting = true;
        public bool UpdateExisting
        {
            get => _updateExisting;
            set => SetProperty(ref _updateExisting, value);
        }

        private bool _autoCreateTypes = true;
        public bool AutoCreateTypes
        {
            get => _autoCreateTypes;
            set => SetProperty(ref _autoCreateTypes, value);
        }

        private bool _preserveIds = true;
        public bool PreserveIds
        {
            get => _preserveIds;
            set => SetProperty(ref _preserveIds, value);
        }

        // Tiến trình xử lý
        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private double _progressValue = 0;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _progressText = "";
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        private string _ifcFilePath = string.Empty;
        public string IfcFilePath
        {
            get => _ifcFilePath;
            set => SetProperty(ref _ifcFilePath, value);
        }

        private ConversionReportResult? _lastResult;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        public ICommand ConnectCommand { get; }
        public ICommand ExtractModelCommand { get; }
        public ICommand SelectIfcFileCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public EtabsConverterViewModel()
        {
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            _etabsService = new EtabsApiService();

            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            ExtractModelCommand = new AsyncRelayCommand(ExtractModelAsync);
            SelectIfcFileCommand = new AsyncRelayCommand(SelectIfcFileAsync);
            ImportCommand = new RelayCommand(OnImport);
            GenerateReportCommand = new RelayCommand(OnGenerateReport);
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

        private async Task SelectIfcFileAsync()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn tệp mô hình IFC từ ETABS",
                    Filter = "Tệp IFC (*.ifc)|*.ifc|Toàn bộ tệp (*.*)|*.*"
                };

                if (dlg.ShowDialog() == true)
                {
                    IfcFilePath = dlg.FileName;
                    IsBusy = true;
                    ProgressText = "Đang đọc cấu trúc tệp tin IFC...";

                    try
                    {
                        await Task.Run(() =>
                        {
                            _currentModel = JNNTool.Tools.IFCEtabs.IFC.IfcReader.Instance.ReadIfcFile(IfcFilePath, (msg, prog) =>
                            {
                                RunOnUi(() =>
                                {
                                    ProgressText = msg;
                                    ProgressValue = prog;
                                });
                            });
                        });

                        if (_currentModel != null)
                        {
                            RunOnUi(() =>
                            {
                                ModelName = _currentModel.ModelName;
                                Units = _currentModel.SourceUnits;
                                LevelCount = _currentModel.Levels.Count;
                                GridCount = _currentModel.Grids.Count;
                                ColumnCount = _currentModel.Columns.Count;
                                BeamCount = _currentModel.Beams.Count;
                                WallCount = _currentModel.Walls.Count;
                                SlabCount = _currentModel.Slabs.Count;

                                StatusText = $"Đã nạp IFC: {ColumnCount} Cột, {BeamCount} Dầm, {WallCount} Vách, {SlabCount} Sàn, {LevelCount} Tầng.";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        RunOnUi(() =>
                        {
                            StatusText = $"Lỗi đọc tệp IFC: {ex.Message}";
                        });
                    }
                    finally
                    {
                        RunOnUi(() => IsBusy = false);
                    }
                }
            }
            catch (Exception ex)
            {
                RunOnUi(() =>
                {
                    StatusText = $"Lỗi mở tệp: {ex.Message}";
                });
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                IsBusy = true;
                ProgressText = "Đang tìm kiếm tiến trình ETABS...";

                await Task.Run(() =>
                {
                    bool success = _etabsService.Connect(out string msg);
                    RunOnUi(() =>
                    {
                        IsConnected = success;
                        StatusText = msg;
                    });
                });

                if (IsConnected)
                {
                    await ExtractModelAsync();
                }
            }
            catch (Exception ex)
            {
                RunOnUi(() =>
                {
                    StatusText = $"Lỗi kết nối ETABS: {ex.Message}";
                });
            }
            finally
            {
                RunOnUi(() => IsBusy = false);
            }
        }

        private async Task ExtractModelAsync()
        {
            if (!IsConnected)
            {
                StatusText = "Vui lòng kết nối ETABS trước khi đọc dữ liệu.";
                return;
            }

            try
            {
                IsBusy = true;
                await Task.Run(() =>
                {
                    _currentModel = _etabsService.ExtractModel((msg, prog) =>
                    {
                        RunOnUi(() =>
                        {
                            ProgressText = msg;
                            ProgressValue = prog;
                        });
                    });
                });

                if (_currentModel != null)
                {
                    RunOnUi(() =>
                    {
                        ModelName = _currentModel.ModelName;
                        Units = _currentModel.SourceUnits;
                        LevelCount = _currentModel.Levels.Count;
                        GridCount = _currentModel.Grids.Count;
                        ColumnCount = _currentModel.Columns.Count;
                        BeamCount = _currentModel.Beams.Count;
                        WallCount = _currentModel.Walls.Count;
                        SlabCount = _currentModel.Slabs.Count;

                        StatusText = $"Đã nạp: {ColumnCount} Cột, {BeamCount} Dầm, {LevelCount} Tầng từ ETABS.";
                    });
                }
            }
            catch (Exception ex)
            {
                RunOnUi(() =>
                {
                    StatusText = $"Lỗi đọc mô hình: {ex.Message}";
                });
            }
            finally
            {
                RunOnUi(() => IsBusy = false);
            }
        }

        private void OnImport()
        {
            if (_currentModel == null || _currentModel.TotalElements == 0)
            {
                MessageBox.Show("Chưa có dữ liệu mô hình để chuyển đổi. Vui lòng kết nối và nạp từ ETABS hoặc chọn tệp IFC.",
                    "JNN ETABS Converter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var options = new ConversionOptions
            {
                ImportLevels = ImportLevels,
                ImportGrids = ImportGrids,
                ImportColumns = ImportColumns,
                ImportBeams = ImportBeams,
                ImportWalls = ImportWalls,
                ImportSlabs = ImportSlabs,
                UpdateExisting = UpdateExisting,
                AutoCreateTypes = AutoCreateTypes
            };

            IsBusy = true;
            ProgressText = "Đang bắt đầu giao dịch chuyển đổi trong Revit...";

            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var doc = app.ActiveUIDocument?.Document;
                    if (doc == null) return;

                    var service = new RevitConversionService(doc);
                    var result = service.ConvertModel(_currentModel, options, (msg, prog) =>
                    {
                        RunOnUi(() =>
                        {
                            ProgressText = msg;
                            ProgressValue = prog;
                        });
                    });

                    _lastResult = result;

                    RunOnUi(() =>
                    {
                        IsBusy = false;
                        StatusText = $"Hoàn tất! {result}";
                        MessageBox.Show($"Chuyển đổi hoàn tất thành công!\n\n{result}",
                            "JNN ETABS Converter", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    RunOnUi(() =>
                    {
                        IsBusy = false;
                        StatusText = $"Lỗi chuyển đổi: {ex.Message}";
                        MessageBox.Show($"Quá trình chuyển đổi thất bại:\n{ex.Message}",
                            "Lỗi Chuyển Đổi", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void OnGenerateReport()
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Chưa có mô hình để xuất báo cáo. Vui lòng nạp mô hình từ ETABS hoặc IFC trước.",
                    "JNN ETABS Converter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var doc = app.ActiveUIDocument?.Document;
                    if (doc == null) return;

                    var tracker = new JNNTool.Tools.IFCEtabs.Revit.Tracking.ElementTracker(doc);
                    var validator = new JNNTool.Tools.IFCEtabs.Services.ValidationEngine();
                    var validation = validator.Validate(doc, _currentModel, tracker);

                    string reportDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "JNN_Reports");
                    if (!System.IO.Directory.Exists(reportDir)) System.IO.Directory.CreateDirectory(reportDir);

                    string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string htmlPath = System.IO.Path.Combine(reportDir, $"JNN_ETABS_Conversion_Report_{timeStamp}.html");
                    string csvPath = System.IO.Path.Combine(reportDir, $"JNN_ETABS_Conversion_Report_{timeStamp}.csv");

                    var result = _lastResult ?? new ConversionReportResult();
                    JNNTool.Tools.IFCEtabs.Services.ReportGenerator.GenerateHtmlReport(doc, _currentModel, tracker, validation, result, htmlPath);
                    JNNTool.Tools.IFCEtabs.Services.ReportGenerator.GenerateCsvReport(doc, _currentModel, tracker, result, csvPath);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(htmlPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    RunOnUi(() =>
                    {
                        MessageBox.Show($"Lỗi tạo báo cáo: {ex.Message}", "Lỗi Báo Cáo", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }
    }
}
