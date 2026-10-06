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

            // Khởi tạo ActionEventHandler cho các Modeless UI của JNN Rebar Suite
            try
            {
                JNNTool.Core.ExternalEvents.ActionEventHandler.Instance.Initialize();
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi khởi tạo ActionEventHandler", ex);
            }


            // Tạo Ribbon Panel Modeling
            RibbonPanel modelingPanel = application.CreateRibbonPanel(tabName, modelingPanelName);

            // Lấy đường dẫn thực thi của tệp DLL
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            
            // Hướng tới thư mục Resources
            string resourcesPath = Path.Combine(Directory.GetParent(assemblyDir).FullName, "Resources");

            // ═══════════════════════════════════════════════════════════════
            //  Panel: JNN Rebar Suite
            // ═══════════════════════════════════════════════════════════════
            RibbonPanel rebarPanel = application.CreateRibbonPanel(tabName, "JNN Rebar");

            // ------------------- Rebar Slab -------------------
            string iconPathRebarSlab = Path.Combine(resourcesPath, "RebarSlab.png");
            PushButtonData btnRebarSlabData = new PushButtonData(
                "cmdSlabRebar",
                "Rebar\nSlab",
                assemblyPath,
                "JNNTool.RebarSuite.Slab.SlabRebarCommand"
            );
            btnRebarSlabData.ToolTip = "Bố trí thép sàn tự động: Lớp dưới, lớp trên và thép mũ gối L/4 theo TCVN 5574:2018.";

            PushButton btnRebarSlab = rebarPanel.AddItem(btnRebarSlabData) as PushButton;
            if (File.Exists(iconPathRebarSlab))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarSlab, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarSlab.LargeImage = img;
                btnRebarSlab.Image = img;
            }

            // ------------------- Rebar Beam (Suite) -------------------
            string iconPathRebarBeamSuite = Path.Combine(resourcesPath, "RebarBeam.png");
            PushButtonData btnRebarBeamSuiteData = new PushButtonData(
                "cmdBeamRebarSuite",
                "Rebar\nBeam",
                assemblyPath,
                "JNNTool.RebarSuite.Beam.BeamRebarCommand"
            );
            btnRebarBeamSuiteData.ToolTip = "Bố trí thép dầm liên tục: Thép chủ, gia cường gối L/4, đai dày/thưa và tạo mặt cắt tự động.";

            PushButton btnRebarBeamSuite = rebarPanel.AddItem(btnRebarBeamSuiteData) as PushButton;
            if (File.Exists(iconPathRebarBeamSuite))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarBeamSuite, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarBeamSuite.LargeImage = img;
                btnRebarBeamSuite.Image = img;
            }

            // ------------------- Rebar Column -------------------
            string iconPathRebarColSuite = Path.Combine(resourcesPath, "RebarColumn.png");
            PushButtonData btnRebarColSuiteData = new PushButtonData(
                "cmdColumnRebarSuite",
                "Rebar\nColumn",
                assemblyPath,
                "JNNTool.RebarSuite.Column.ColumnRebarCommand"
            );
            btnRebarColSuiteData.ToolTip = "Bố trí thép chuỗi cột theo tầng: Bẻ cổ chai 1:6, đai dày/thưa, đai AB/CN và tạo MCD.";

            PushButton btnRebarColSuite = rebarPanel.AddItem(btnRebarColSuiteData) as PushButton;
            if (File.Exists(iconPathRebarColSuite))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarColSuite, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarColSuite.LargeImage = img;
                btnRebarColSuite.Image = img;
            }

            // ------------------- Rebar Wall -------------------
            string iconPathRebarWallSuite = Path.Combine(resourcesPath, "DisallowWallJoins.png");
            PushButtonData btnRebarWallSuiteData = new PushButtonData(
                "cmdWallRebarSuite",
                "Rebar\nWall",
                assemblyPath,
                "JNNTool.RebarSuite.Wall.WallRebarCommand"
            );
            btnRebarWallSuiteData.ToolTip = "Bố trí thép vách 2 lớp: Thép đứng, thép ngang, đai C ghim biên, chống thấm và tạo MCD.";

            PushButton btnRebarWallSuite = rebarPanel.AddItem(btnRebarWallSuiteData) as PushButton;
            if (File.Exists(iconPathRebarWallSuite))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarWallSuite, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarWallSuite.LargeImage = img;
                btnRebarWallSuite.Image = img;
            }

            // ------------------- Rebar Foundation -------------------
            string iconPathRebarFdn = Path.Combine(resourcesPath, "Connection.png");
            PushButtonData btnRebarFdnData = new PushButtonData(
                "cmdFoundationRebarSuite",
                "Rebar\nFooting",
                assemblyPath,
                "JNNTool.RebarSuite.Foundation.FoundationRebarCommand"
            );
            btnRebarFdnData.ToolTip = "Bố trí thép móng & đài cọc: Lưới đáy móc uốn 90°, lưới trên, thép chờ cột chân vịt.";

            PushButton btnRebarFdn = rebarPanel.AddItem(btnRebarFdnData) as PushButton;
            if (File.Exists(iconPathRebarFdn))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarFdn, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarFdn.LargeImage = img;
                btnRebarFdn.Image = img;
            }

            // ------------------- Rebar Pile -------------------
            string iconPathRebarPile = Path.Combine(resourcesPath, "PileLocation.png");
            PushButtonData btnRebarPileData = new PushButtonData(
                "cmdPileRebarSuite",
                "Rebar\nPile",
                assemblyPath,
                "JNNTool.RebarSuite.Pile.PileRebarCommand"
            );
            btnRebarPileData.ToolTip = "Bố trí thép cọc khoan nhồi / cọc vuông: Lồng thép, đai phân vùng, vành gia cường và xuất tọa độ CSV.";

            PushButton btnRebarPile = rebarPanel.AddItem(btnRebarPileData) as PushButton;
            if (File.Exists(iconPathRebarPile))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarPile, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarPile.LargeImage = img;
                btnRebarPile.Image = img;
            }

            // ------------------- Rebar Stair -------------------
            string iconPathRebarStairSuite = Path.Combine(resourcesPath, "StairDetail.png");
            PushButtonData btnRebarStairSuiteData = new PushButtonData(
                "cmdStairRebarSuite",
                "Rebar\nStair",
                assemblyPath,
                "JNNTool.RebarSuite.Stair.StairRebarCommand"
            );
            btnRebarStairSuiteData.ToolTip = "Bố trí thép bản thang: Thép lớp dưới chịu nhịp, thép mũ gối trên neo dầm và thép phân bố ngang.";

            PushButton btnRebarStairSuite = rebarPanel.AddItem(btnRebarStairSuiteData) as PushButton;
            if (File.Exists(iconPathRebarStairSuite))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathRebarStairSuite, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnRebarStairSuite.LargeImage = img;
                btnRebarStairSuite.Image = img;
            }

            // ------------------- Pulldown: Rebar Utils -------------------
            PulldownButtonData pulldownUtilsData = new PulldownButtonData(
                "cmdRebarUtilsSuite",
                "Rebar\nUtils"
            );
            pulldownUtilsData.ToolTip = "Bộ tiện ích cốt thép: Thống kê khối lượng, đánh số hiệu Mark, Unobscured, Isolate và chuẩn hóa RebarType.";

            PulldownButton pullUtils = rebarPanel.AddItem(pulldownUtilsData) as PulldownButton;
            string iconPathUtils = Path.Combine(resourcesPath, "SpeedOverrider.png");
            if (File.Exists(iconPathUtils))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathUtils, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                pullUtils.LargeImage = img;
                pullUtils.Image = img;
            }

            pullUtils.AddPushButton(new PushButtonData(
                "cmdRebarVolume",
                "Thống Kê Khối Lượng Thép (CSV)",
                assemblyPath,
                "JNNTool.RebarSuite.Utilities.Commands.RebarVolumeCommand"
            ));

            pullUtils.AddPushButton(new PushButtonData(
                "cmdRebarNumber",
                "Đánh Số Hiệu Thép (Auto Mark)",
                assemblyPath,
                "JNNTool.RebarSuite.Utilities.Commands.RebarNumberCommand"
            ));

            pullUtils.AddPushButton(new PushButtonData(
                "cmdRebarUnobscured",
                "Bật Nhìn Xuyên (View Unobscured)",
                assemblyPath,
                "JNNTool.RebarSuite.Utilities.Commands.RebarUnobscuredCommand"
            ));

            pullUtils.AddPushButton(new PushButtonData(
                "cmdRebarIsolate",
                "Cô Lập Cốt Thép (Isolate Rebar)",
                assemblyPath,
                "JNNTool.RebarSuite.Utilities.Commands.IsolateRebarCommand"
            ));

            pullUtils.AddPushButton(new PushButtonData(
                "cmdRebarTypeManager",
                "Chuẩn Hóa RebarBarType (D6-D32)",
                assemblyPath,
                "JNNTool.RebarSuite.Utilities.Commands.RebarTypeManagerCommand"
            ));





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

            // ------------------- Tạm ẩn Module IFC Etabs (Chưa hoàn thiện) -------------------
            /*
            PushButtonData btnEtabsConverterData = new PushButtonData(
                "cmdEtabsConverter",
                "ETABS\nConverter",
                assemblyPath,
                "JNNTool.Tools.IFCEtabs.Commands.EtabsConverterCommand"
            );
            btnEtabsConverterData.ToolTip = "Chuyển đổi trực tiếp mô hình ETABS sang cấu kiện Native Revit (Dầm, Cột, Tầng, Trục) kèm tham số JNN.";

            PushButton btnEtabsConverter = csiPanel.AddItem(btnEtabsConverterData) as PushButton;
            if (File.Exists(iconPathImportE2K))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathImportE2K, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnEtabsConverter.LargeImage = img;
                btnEtabsConverter.Image = img;
            }

            PushButtonData btnPropViewerData = new PushButtonData(
                "cmdPropertyViewer",
                "Property\nViewer",
                assemblyPath,
                "JNNTool.Tools.IFCEtabs.Commands.PropertyViewerCommand"
            );
            btnPropViewerData.ToolTip = "Tra cứu thuộc tính nguồn gốc (ETABS / IFC) và các tham số chi tiết của cấu kiện kết cấu.";

            PushButton btnPropViewer = csiPanel.AddItem(btnPropViewerData) as PushButton;
            if (File.Exists(iconPathImportE2K))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathImportE2K, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnPropViewer.LargeImage = img;
                btnPropViewer.Image = img;
            }
            */

            // ------------------- Căn Lề Biên (Boundary Align) -------------------
            string iconPathBoundaryAlign = Path.Combine(resourcesPath, "ExtendBeam.png");
            PushButtonData btnBoundaryAlignData = new PushButtonData(
                "cmdBoundaryAlign",
                "Căn Lề\nBiên",
                assemblyPath,
                "JNNTool.Tools.CSIxRevit.Commands.BoundaryAlignCommand"
            );
            btnBoundaryAlignData.ToolTip = "Dời dầm và cột biên vào trong ranh đất / lưới trục (B/2), tự động co/kéo các dầm ngang vuông góc kết nối vào.";

            PushButton btnBoundaryAlign = csiPanel.AddItem(btnBoundaryAlignData) as PushButton;
            if (File.Exists(iconPathBoundaryAlign))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathBoundaryAlign, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnBoundaryAlign.LargeImage = img;
                btnBoundaryAlign.Image = img;
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

            // ------------------- Create Dim (Beam) -------------------
            string iconPathCreateDim = Path.Combine(resourcesPath, "CreateDim.png");
            PushButtonData btnCreateDimData = new PushButtonData(
                "cmdCreateDimBeam",
                "Dim\nBeam",
                assemblyPath,
                "JNNTool.Tools.CreateDim.CreateDimBeamCmd"
            );
            btnCreateDimData.ToolTip = "Chọn 1 hoặc nhiều dầm trên mặt bằng → tự tạo Dim chiều dài tổng (2 đầu dầm).";

            PushButton btnCreateDim = modelingPanel.AddItem(btnCreateDimData) as PushButton;
            if (File.Exists(iconPathCreateDim))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathCreateDim, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnCreateDim.LargeImage = img;
                btnCreateDim.Image = img;
            }

            // ------------------- Extend Beam -------------------
            string iconPathExtendBeam = Path.Combine(resourcesPath, "ExtendBeam.png");
            PushButtonData btnExtendBeamData = new PushButtonData(
                "cmdExtendBeam",
                "Extend\nBeam",
                assemblyPath,
                "JNNTool.Tools.ExtendBeam.ExtendBeamCmd"
            );
            btnExtendBeamData.ToolTip = "Chạy cho toàn bộ dầm trong view hiện hành (hoặc chỉ các dầm đã chọn sẵn) → tự nhận diện gối ở đầu Start / End (Cột, Dầm, Tường, Sàn, Móng) và đưa tim dầm 2 đầu tới đúng mặt gối. Dầm xuyên qua cấu kiện không bị cắt.";

            PushButton btnExtendBeam = modelingPanel.AddItem(btnExtendBeamData) as PushButton;
            if (File.Exists(iconPathExtendBeam))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPathExtendBeam, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                btnExtendBeam.LargeImage = img;
                btnExtendBeam.Image = img;
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
