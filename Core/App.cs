using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace JNNTool
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "JNNTool";
            string modelingPanelName = "Modeling";

            // Khởi tạo Ribbon Tab
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                // Bỏ qua nếu Tab đã tồn tại
            }


            // Tạo Ribbon Panel Modeling
            RibbonPanel modelingPanel = application.CreateRibbonPanel(tabName, modelingPanelName);

            // Lấy đường dẫn thực thi của tệp DLL
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            
            // Hướng tới thư mục Resources
            string resourcesPath = Path.Combine(Directory.GetParent(assemblyDir).FullName, "Resources");


            // ------------------- Floor By Room -------------------
            string iconPathFloorByRoom = Path.Combine(resourcesPath, "FloorByRoom.png");
            PushButtonData btnFloorByRoomData = new PushButtonData(
                "cmdFloorByRoom",
                "Floor By\nRoom",
                assemblyPath,
                "JNNTool.FloorByRoomCmd"
            );
            btnFloorByRoomData.ToolTip = "Vé sàn tự động theo Boundary của Room. Cho phép chọn nhiều Room, nhập chiều dày và loại sàn.";

            PushButton btnFloorByRoom = modelingPanel.AddItem(btnFloorByRoomData) as PushButton;
            if (File.Exists(iconPathFloorByRoom))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathFloorByRoom, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnFloorByRoom.LargeImage = img;
                btnFloorByRoom.Image = img;
            }

            // ------------------- Split Beam By Column -------------------
            string iconPathSplitBeam = Path.Combine(resourcesPath, "SplitBeam.png");
            PushButtonData btnSplitBeamData = new PushButtonData(
                "cmdSplitBeamByColumn",
                "Split\nBeam",
                assemblyPath,
                "JNNTool.SplitBeamByColumnCmd"
            );
            btnSplitBeamData.ToolTip = "Ngắt dầm thành từng đoạn tại vị trí các cột hoặc dầm cắt ngang được chọn.";

            PushButton btnSplitBeam = modelingPanel.AddItem(btnSplitBeamData) as PushButton;
            if (File.Exists(iconPathSplitBeam))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathSplitBeam, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnSplitBeam.LargeImage = img;
                btnSplitBeam.Image = img;
            }

            // ------------------- Rebar Beam -------------------
            string iconPathRebarBeam = Path.Combine(resourcesPath, "RebarBeam.png");
            PushButtonData btnRebarBeamData = new PushButtonData(
                "cmdRebarBeam",
                "Rebar\nBeam",
                assemblyPath,
                "JNNTool.RebarBeamCmd"
            );
            btnRebarBeamData.ToolTip = "Bố trí thép dầm liên tục (Lớp trên, dưới, và đai theo L0/4).";

            PushButton btnRebarBeam = modelingPanel.AddItem(btnRebarBeamData) as PushButton;
            if (File.Exists(iconPathRebarBeam))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarBeam, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarBeam.LargeImage = img;
                btnRebarBeam.Image = img;
            }

            // ------------------- Create Section -------------------
            string iconPathCreateSection = Path.Combine(resourcesPath, "CreateSection.png");
            PushButtonData btnCreateSectionData = new PushButtonData(
                "cmdCreateSection",
                "Create\nSection",
                assemblyPath,
                "JNNTool.CreateSectionCmd"
            );
            btnCreateSectionData.ToolTip = "Duplicate hàng loạt type dầm/cột với kích thước b×h mới.";

            PushButton btnCreateSection = modelingPanel.AddItem(btnCreateSectionData) as PushButton;
            if (File.Exists(iconPathCreateSection))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathCreateSection, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnCreateSection.LargeImage = img;
                btnCreateSection.Image = img;
            }

            // ═══════════════════════════════════════════════════════════════
            //  Panel: CSI x Revit
            // ═══════════════════════════════════════════════════════════════
            RibbonPanel csiPanel = application.CreateRibbonPanel(tabName, "CSI x Revit");

            // ------------------- Import E2K -------------------
            string iconPathImportE2K = Path.Combine(resourcesPath, "ImportE2K.png");
            PushButtonData btnImportE2KData = new PushButtonData(
                "cmdImportE2K",
                "Import\nE2K",
                assemblyPath,
                "JNNTool.Tools.CSIxRevit.Commands.ImportE2KCommand"
            );
            btnImportE2KData.ToolTip = "Import mô hình từ file E2K (ETABS/SAP2000) vào Revit.";

            PushButton btnImportE2K = csiPanel.AddItem(btnImportE2KData) as PushButton;
            if (File.Exists(iconPathImportE2K))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathImportE2K, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnImportE2K.LargeImage = img;
                btnImportE2K.Image = img;
            }

            // ------------------- Connection -------------------
            string iconPathConnection = Path.Combine(resourcesPath, "Connection.png");
            PushButtonData btnConnectionData = new PushButtonData(
                "cmdConnection",
                "Steel\nConnection",
                assemblyPath,
                "JNNTool.Tools.Connection.Command"
            );
            btnConnectionData.ToolTip = "Tạo liên kết thép (Steel Connection) cho dầm và cột.";

            PushButton btnConnection = modelingPanel.AddItem(btnConnectionData) as PushButton;
            if (File.Exists(iconPathConnection))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathConnection, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnConnection.LargeImage = img;
                btnConnection.Image = img;
            }

            // ------------------- Clash Detection -------------------
            string iconPathClash = Path.Combine(resourcesPath, "ClashDetection.png");
            PushButtonData btnClashData = new PushButtonData(
                "cmdClashDetection",
                "Clash\nDetection",
                assemblyPath,
                "JNNTool.Tools.ClashDetection.Command"
            );
            btnClashData.ToolTip = "Kiểm tra va chạm giữa model và Revit Link.";

            PushButton btnClash = modelingPanel.AddItem(btnClashData) as PushButton;
            if (File.Exists(iconPathClash))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathClash, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnClash.LargeImage = img;
                btnClash.Image = img;
            }

            // ------------------- Rebar Column -------------------
            string iconPathRebarCol = Path.Combine(resourcesPath, "RebarColumn.png");
            PushButtonData btnRebarColData = new PushButtonData(
                "cmdRebarColumn",
                "Rebar\nColumn",
                assemblyPath,
                "JNNTool.Tools.RebarColumn.Command"
            );
            btnRebarColData.ToolTip = "Bố trí thép cột (thép dọc, đai, nối thép).";

            PushButton btnRebarCol = modelingPanel.AddItem(btnRebarColData) as PushButton;
            if (File.Exists(iconPathRebarCol))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarCol, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarCol.LargeImage = img;
                btnRebarCol.Image = img;
            }

            // ------------------- Stair Detail -------------------
            string iconPathStair = Path.Combine(resourcesPath, "StairDetail.png");
            PushButtonData btnStairData = new PushButtonData(
                "cmdStairDetail",
                "Stair\nDetail",
                assemblyPath,
                "JNNTool.Tools.StairDetail.StairDetail"
            );
            btnStairData.ToolTip = "Tạo chi tiết thép cầu thang (thép chủ, thép phụ, kích thước).";

            PushButton btnStair = modelingPanel.AddItem(btnStairData) as PushButton;
            if (File.Exists(iconPathStair))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathStair, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnStair.LargeImage = img;
                btnStair.Image = img;
            }
            // ------------------- Disallow Join Beam -------------------
            string iconPathDisallow = Path.Combine(resourcesPath, "DisallowJoinBeam.png");
            PushButtonData btnDisallowData = new PushButtonData(
                "cmdDisallowJoinBeam",
                "Disallow\nJoin Beam",
                assemblyPath,
                "JNNTool.Tools.DisallowJoinBeam.DisallowJoinBeamCmd"
            );
            btnDisallowData.ToolTip = "Quét chọn nhiều dầm và thực hiện Disallow Join ở 2 đầu.";

            PushButton btnDisallow = modelingPanel.AddItem(btnDisallowData) as PushButton;
            if (File.Exists(iconPathDisallow))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathDisallow, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnDisallow.LargeImage = img;
                btnDisallow.Image = img;
            }
            // ------------------- Create Filter -------------------
            string iconPathCreateFilter = Path.Combine(resourcesPath, "CreateFilter.png");
            PushButtonData btnCreateFilterData = new PushButtonData(
                "cmdCreateFilter",
                "Create\nFilter",
                assemblyPath,
                "JNNTool.Tools.CreateFilter.Command"
            );
            btnCreateFilterData.ToolTip = "Tạo bộ lọc tự động từ danh sách phần tử (Create Filter).";

            PushButton btnCreateFilter = modelingPanel.AddItem(btnCreateFilterData) as PushButton;
            if (File.Exists(iconPathCreateFilter))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathCreateFilter, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnCreateFilter.LargeImage = img;
                btnCreateFilter.Image = img;
            }

            // ------------------- Speed Overrider Element -------------------
            string iconPathSpeedOverrider = Path.Combine(resourcesPath, "SpeedOverrider.png");
            PushButtonData btnSpeedOverriderData = new PushButtonData(
                "cmdSpeedOverriderElement",
                "Speed\nOverrider",
                assemblyPath,
                "JNNTool.Tools.SpeedOverriderElement.Command"
            );
            btnSpeedOverriderData.ToolTip = "Override hiển thị phần tử nhanh (Speed Overrider).";

            PushButton btnSpeedOverrider = modelingPanel.AddItem(btnSpeedOverriderData) as PushButton;
            if (File.Exists(iconPathSpeedOverrider))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathSpeedOverrider, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnSpeedOverrider.LargeImage = img;
                btnSpeedOverrider.Image = img;
            }

            // ------------------- Pile Location -------------------
            string iconPathPileLocation = Path.Combine(resourcesPath, "PileLocation.png");
            PushButtonData btnPileLocationData = new PushButtonData(
                "cmdPileLocation",
                "Pile\nLocation",
                assemblyPath,
                "JNNTool.Tools.PileLocation.Command"
            );
            btnPileLocationData.ToolTip = "Chọn 1 cọc gốc → tool tự tìm tất cả cọc (Pile) trong view → ghi toạ độ thực tế (mm) vào X Coordinate / Y Coordinate.";

            PushButton btnPileLocation = modelingPanel.AddItem(btnPileLocationData) as PushButton;
            if (File.Exists(iconPathPileLocation))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathPileLocation, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnPileLocation.LargeImage = img;
                btnPileLocation.Image = img;
            }

            // ─── Auto-update check (background, non-blocking) ───────────────
#if !NET48
            _ = Task.Run(async () =>
            {
                try
                {
                    // Đọc version đang cài từ bundle
                    string bundleDir     = Path.GetDirectoryName(assemblyPath) is { } d
                                          ? Path.GetFullPath(Path.Combine(d, "..", ".."))
                                          : "";
                    string installedJson = Path.Combine(bundleDir, "version.json");
                    if (!File.Exists(installedJson)) return;

                    var installedText = await File.ReadAllTextAsync(installedJson);
                    var installed     = JsonSerializer.Deserialize<VersionInfo>(installedText);
                    if (installed == null) return;

                    // Nếu có manifestUrl thì check online
                    if (string.IsNullOrWhiteSpace(installed.ManifestUrl)) return;

                    using var http   = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var latestJson   = await http.GetStringAsync(installed.ManifestUrl);
                    var latest       = JsonSerializer.Deserialize<VersionInfo>(latestJson);
                    if (latest == null) return;

                    if (Version.TryParse(latest.Version, out var lv) &&
                        Version.TryParse(installed.Version, out var iv) &&
                        lv > iv)
                    {
                        // Hiện thông báo trên UI thread của Revit
                        application.ControlledApplication.ApplicationInitialized += (s, e) =>
                        {
                            TaskDialog dlg = new TaskDialog("JNNTool — Có phiên bản mới!");
                            dlg.MainIcon        = TaskDialogIcon.TaskDialogIconInformation;
                            dlg.MainInstruction = $"🔔 JNNTool v{latest.Version} đã sẵn sàng!";
                            dlg.MainContent     = $"Bạn đang dùng v{installed.Version}.\n" +
                                                  $"Vui lòng chạy JNNToolInstaller.exe để cập nhật.";
                            dlg.Show();
                        };
                    }
                }
                catch { /* Không làm gì nếu lỗi network */ }
            });
#endif

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    /// <summary>Model nhẹ để đọc version.json (tránh dependency nặng).</summary>
    internal sealed class VersionInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string Version { get; set; } = "0.0.0";

        [System.Text.Json.Serialization.JsonPropertyName("manifestUrl")]
        public string ManifestUrl { get; set; } = "";
    }
}
