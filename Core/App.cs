using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace JNNTool
{
    public class App : IExternalApplication
    {
        /// <summary>
        /// Giữ static reference trường tồn cho Dockable Pane Host để ngăn Garbage Collector
        /// thu hồi vùng nhớ gây lỗi native Access Violation (0xC0000005) trong Revit.
        /// </summary>
        public static JNNTool.Core.Palette.PaletteHost? PaletteHostInstance { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            // Tự động resolve các assembly phụ thuộc trong cùng thư mục
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            // Chốt UI Dispatcher cho Command Palette và các Modeless UI
            JNNTool.Core.UI.UiDispatcher.Capture();

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

            // Đảm bảo môi trường WPF an toàn, ngăn lỗi "The Application object is being shut down"
            try
            {
                JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi khởi tạo WpfWindowHelper", ex);
            }

            // [Tạm thời ẩn] Đăng ký Dockable Pane cho Command Palette
            /*
            try
            {
                PaletteHostInstance = new JNNTool.Core.Palette.PaletteHost();
                application.RegisterDockablePane(
                    JNNTool.Core.Palette.PaletteHost.PaneId,
                    JNNTool.Core.Palette.PaletteHost.PaneTitle,
                    PaletteHostInstance
                );
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi đăng ký Dockable Pane Command Palette", ex);
            }
            */

            // Lấy đường dẫn thực thi của tệp DLL
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);
            
            // Hướng tới thư mục Resources
            string resourcesPath = Path.Combine(Directory.GetParent(assemblyDir).FullName, "Resources");

            // ═══════════════════════════════════════════════════════════════
            //  Panel 1: General (Bản quyền & Cài đặt hệ thống JNNTool)
            // ═══════════════════════════════════════════════════════════════
            try
            {
                RibbonPanel generalPanel = application.CreateRibbonPanel(tabName, "General");

                // [Tạm thời ẩn] Nút Command Palette trên thanh Ribbon
                /*
                PushButtonData btnPaletteData = new PushButtonData(
                    "cmdPalette",
                    "Command\nPalette",
                    assemblyPath,
                    "JNNTool.Core.Palette.TogglePaletteCmd"
                );
                btnPaletteData.AvailabilityClassName = "JNNTool.Core.Palette.AlwaysAvailable";
                btnPaletteData.ToolTip = "Mở bảng Command Palette (Ctrl+F) tra cứu và thực thi nhanh toàn bộ công cụ JNNTool.";

                PushButton btnPalette = generalPanel.AddItem(btnPaletteData) as PushButton;
                if (btnPalette != null)
                {
                    string iconPathPalette = Path.Combine(resourcesPath, "CommandPalette.png");
                    if (!File.Exists(iconPathPalette)) iconPathPalette = Path.Combine(resourcesPath, "UIManager.png");
                    if (File.Exists(iconPathPalette))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathPalette, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnPalette.LargeImage = img;
                        btnPalette.Image = img;
                    }
                }
                */

                // ------------------- Account / License -------------------
                PushButtonData btnLicenseData = new PushButtonData(
                    "cmdLicense",
                    "Account\nLicense",
                    assemblyPath,
                    "JNNTool.Tools.Login.Commands.LoginCmd"
                );
                btnLicenseData.ToolTip = "Quản lý bản quyền, đăng nhập tài khoản và kích hoạt JNNTool.";

                PushButton btnLicense = generalPanel.AddItem(btnLicenseData) as PushButton;
                if (btnLicense != null)
                {
                    string iconPathLicense = Path.Combine(resourcesPath, "Login.png");
                    if (File.Exists(iconPathLicense))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathLicense, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnLicense.LargeImage = img;
                        btnLicense.Image = img;
                    }
                }

                // ------------------- Ribbon Theme -------------------
                PushButtonData btnThemeData = new PushButtonData(
                    "cmdUIManager",
                    "Ribbon\nTheme",
                    assemblyPath,
                    "JNNTool.Tools.UIManager.Commands.UIManagerCmd"
                );
                btnThemeData.ToolTip = "Tùy biến màu sắc theme (12 bảng màu hoặc mã HEX) và kiểu hiển thị Icon (Badge, Glyph, Circle, Outline) trên thanh Ribbon JNNTool.";

                PushButton btnTheme = generalPanel.AddItem(btnThemeData) as PushButton;
                if (btnTheme != null)
                {
                    string iconPathTheme = Path.Combine(resourcesPath, "UIManager.png");
                    if (!File.Exists(iconPathTheme)) iconPathTheme = Path.Combine(resourcesPath, "SpeedOverrider.png");
                    if (File.Exists(iconPathTheme))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathTheme, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnTheme.LargeImage = img;
                        btnTheme.Image = img;
                    }
                }
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi tạo Ribbon Panel General", ex);
            }

            // Tạo Ribbon Panel Modeling
            RibbonPanel modelingPanel = application.CreateRibbonPanel(tabName, modelingPanelName);

            // ═══════════════════════════════════════════════════════════════
            //  Panel: JNN Rebar Suite (Tạm ẩn theo yêu cầu người dùng)
            // ═══════════════════════════════════════════════════════════════
#if false
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
            string iconPathRebarWallSuite = Path.Combine(resourcesPath, "RebarWall.png");
            if (!File.Exists(iconPathRebarWallSuite)) iconPathRebarWallSuite = Path.Combine(resourcesPath, "DisallowWallJoins.png");
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
            string iconPathRebarFdn = Path.Combine(resourcesPath, "RebarFoundation.png");
            if (!File.Exists(iconPathRebarFdn)) iconPathRebarFdn = Path.Combine(resourcesPath, "Connection.png");
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
            string iconPathRebarPile = Path.Combine(resourcesPath, "RebarPile.png");
            if (!File.Exists(iconPathRebarPile)) iconPathRebarPile = Path.Combine(resourcesPath, "PileLocation.png");
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
            string iconPathRebarStairSuite = Path.Combine(resourcesPath, "RebarStair.png");
            if (!File.Exists(iconPathRebarStairSuite)) iconPathRebarStairSuite = Path.Combine(resourcesPath, "StairDetail.png");
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
            string iconPathUtils = Path.Combine(resourcesPath, "RebarUtils.png");
            if (!File.Exists(iconPathUtils)) iconPathUtils = Path.Combine(resourcesPath, "SpeedOverrider.png");
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
#endif





#if false
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
#endif

            // ═══════════════════════════════════════════════════════════════
            //  Panel: Modeling (2 Cột x 3 Nút Nhỏ Xếp Tầng - Stacked Items)
            // ═══════════════════════════════════════════════════════════════
            // Cột 1: Split Beam, Create Section, Disallow Join Beam
            PushButtonData btnSplitBeamData = new PushButtonData(
                "cmdSplitBeamByColumn",
                "Split Beam",
                assemblyPath,
                "JNNTool.SplitBeamByColumnCmd"
            );
            btnSplitBeamData.ToolTip = "Ngắt dầm thành từng đoạn tại vị trí các cột hoặc dầm cắt ngang được chọn.";
            SetRibbonItemIcon(btnSplitBeamData, "SplitBeam.png", resourcesPath);

            PushButtonData btnCreateSectionData = new PushButtonData(
                "cmdCreateSection",
                "Create Section",
                assemblyPath,
                "JNNTool.CreateSectionCmd"
            );
            btnCreateSectionData.ToolTip = "Duplicate hàng loạt type dầm/cột với kích thước b×h mới.";
            SetRibbonItemIcon(btnCreateSectionData, "CreateSection.png", resourcesPath);

            PushButtonData btnDisallowData = new PushButtonData(
                "cmdDisallowJoinBeam",
                "Disallow Join Beam",
                assemblyPath,
                "JNNTool.Tools.DisallowJoinBeam.DisallowJoinBeamCmd"
            );
            btnDisallowData.ToolTip = "Quét chọn nhiều dầm và thực hiện Disallow Join ở 2 đầu.";
            SetRibbonItemIcon(btnDisallowData, "DisallowJoinBeam.png", resourcesPath);

            // Cột 2: Create Filter, Speed Overrider, Pile Location
            PushButtonData btnCreateFilterData = new PushButtonData(
                "cmdCreateFilter",
                "Create Filter",
                assemblyPath,
                "JNNTool.Tools.CreateFilter.Command"
            );
            btnCreateFilterData.ToolTip = "Tạo bộ lọc tự động từ danh sách phần tử (Create Filter).";
            SetRibbonItemIcon(btnCreateFilterData, "CreateFilter.png", resourcesPath);

            PushButtonData btnSpeedOverriderData = new PushButtonData(
                "cmdSpeedOverriderElement",
                "Speed Overrider",
                assemblyPath,
                "JNNTool.Tools.SpeedOverriderElement.Command"
            );
            btnSpeedOverriderData.ToolTip = "Override hiển thị phần tử nhanh (Speed Overrider).";
            SetRibbonItemIcon(btnSpeedOverriderData, "SpeedOverrider.png", resourcesPath);

            PushButtonData btnPileLocationData = new PushButtonData(
                "cmdPileLocation",
                "Pile Location",
                assemblyPath,
                "JNNTool.Tools.PileLocation.Command"
            );
            btnPileLocationData.ToolTip = "Chọn 1 cọc gốc → tool tự tìm tất cả cọc (Pile) trong view → ghi toạ độ thực tế (mm) vào X Coordinate / Y Coordinate.";
            SetRibbonItemIcon(btnPileLocationData, "PileLocation.png", resourcesPath);

            // ------------------- Merge Floor & Cut Floor (Nút nhỏ xếp tầng) -------------------
            SplitButtonData splitMergeFloorData = new SplitButtonData("cmdMergeFloorSplit", "Merge Floor");
            SetRibbonItemIcon(splitMergeFloorData, "MergeFloor.png", resourcesPath);

            PushButtonData btnCutFloorData = new PushButtonData(
                "cmdCutFloor",
                "Cut Floor",
                assemblyPath,
                "JNNTool.Tools.CutByFloor.CutByFloorCmd"
            );
            btnCutFloorData.ToolTip = "Quét chọn sàn to và các sàn nhỏ (hoặc chọn theo 2 bước) để tự động cắt/khoét ranh giới sàn to theo sàn nhỏ.";
            SetRibbonItemIcon(btnCutFloorData, "CutFloor.png", resourcesPath);

            // Xếp các nút nhỏ (icon nhỏ 16x16) vào Modeling Panel
            modelingPanel.AddStackedItems(btnSplitBeamData, btnCreateSectionData, btnDisallowData);
            modelingPanel.AddStackedItems(btnCreateFilterData, btnSpeedOverriderData, btnPileLocationData);
            IList<RibbonItem> floorStacked = modelingPanel.AddStackedItems(splitMergeFloorData, btnCutFloorData);

            if (floorStacked != null && floorStacked.Count >= 1 && floorStacked[0] is SplitButton splitMergeFloor)
            {
                PushButtonData btnMergeActiveViewData = new PushButtonData(
                    "cmdMergeFloorActiveView",
                    "Merge Floor (Active View)",
                    assemblyPath,
                    "JNNTool.Tools.MergeFloor.MergeFloorActiveViewCmd"
                );
                btnMergeActiveViewData.ToolTip = "Tự động quét và gộp tất cả các mảnh sàn tiếp giáp/gần nhau (cùng loại & cùng cao độ) trong View hiện hành mà không cần chọn tay.";
                SetRibbonItemIcon(btnMergeActiveViewData, "MergeFloor.png", resourcesPath);
                splitMergeFloor.AddPushButton(btnMergeActiveViewData);

                PushButtonData btnMergeSelectData = new PushButtonData(
                    "cmdMergeFloorSelect",
                    "Merge Floor (Selection)",
                    assemblyPath,
                    "JNNTool.Tools.MergeFloor.MergeFloorSelectCmd"
                );
                btnMergeSelectData.ToolTip = "Quét chọn thủ công các mảnh sàn trên màn hình để gộp thành 1 sàn duy nhất.";
                SetRibbonItemIcon(btnMergeSelectData, "MergeFloor.png", resourcesPath);
                splitMergeFloor.AddPushButton(btnMergeSelectData);
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

            // ------------------- Fit Scope Box -------------------
            string iconPathFitScopeBox = Path.Combine(resourcesPath, "DimGrid.png");
            PushButtonData btnFitScopeBoxData = new PushButtonData(
                "cmdFitScopeBox",
                "Fit Scope Box",
                assemblyPath,
                "JNNTool.Tools.CSIxRevit.Commands.FitScopeBoxCommand"
            );
            btnFitScopeBoxData.ToolTip = "Khóa và cắt khớp toàn bộ Lưới trục (Grid) và Cao độ (Level) theo Scope Box 'JNN_SB' sau khi bạn đã kéo bao quanh nhà.";
            SetRibbonItemIcon(btnFitScopeBoxData, "DimGrid.png", resourcesPath);

            // ------------------- Boundary Align -------------------
            string iconPathBoundaryAlign = Path.Combine(resourcesPath, "ExtendBeam.png");
            PushButtonData btnBoundaryAlignData = new PushButtonData(
                "cmdBoundaryAlign",
                "Boundary Align",
                assemblyPath,
                "JNNTool.Tools.CSIxRevit.Commands.BoundaryAlignCommand"
            );
            btnBoundaryAlignData.ToolTip = "Dời dầm và cột biên vào trong ranh đất / lưới trục (B/2), tự động co/kéo các dầm ngang vuông góc kết nối vào.";
            SetRibbonItemIcon(btnBoundaryAlignData, "ExtendBeam.png", resourcesPath);

            // ------------------- Auto Join -------------------
            string iconPathAutoJoin = Path.Combine(resourcesPath, "AutoJoin.png");
            if (!File.Exists(iconPathAutoJoin))
            {
                iconPathAutoJoin = Path.Combine(resourcesPath, "JoinElements.png");
            }

            SplitButtonData splitBtnData = new SplitButtonData("cmdAutoJoinSplit", "Auto Join");
            SetRibbonItemIcon(splitBtnData, Path.GetFileName(iconPathAutoJoin), resourcesPath);

            // Xếp 3 nút nhỏ xếp tầng (Fit Scope Box, Boundary Align, Auto Join) cột bên cạnh Import E2K
            IList<RibbonItem> csiStacked = csiPanel.AddStackedItems(btnFitScopeBoxData, btnBoundaryAlignData, splitBtnData);

            if (csiStacked != null && csiStacked.Count >= 3 && csiStacked[2] is SplitButton splitBtn)
            {
                PushButtonData btnActiveData = new PushButtonData(
                    "cmdAutoJoinActiveView",
                    "Auto Join (Active View)",
                    assemblyPath,
                    "JNNTool.Tools.AutoJoin.AutoJoinActiveViewCommand"
                );
                btnActiveData.ToolTip = "Tự động Join và đảo thứ tự ưu tiên (Column > Beam > Floor) trong View hiện hành.\n\n• Cột cắt Dầm và Sàn\n• Dầm cắt Sàn\n• Tự động đảo thứ tự (Switch Order) nếu đang bị ngược.";
                SetRibbonItemIcon(btnActiveData, Path.GetFileName(iconPathAutoJoin), resourcesPath);
                splitBtn.AddPushButton(btnActiveData);

                PushButtonData btnProjectData = new PushButtonData(
                    "cmdAutoJoinProject",
                    "Auto Join (Entire Project)",
                    assemblyPath,
                    "JNNTool.Tools.AutoJoin.AutoJoinProjectCommand"
                );
                btnProjectData.ToolTip = "Tự động Join và đảo thứ tự ưu tiên (Column > Beam > Floor) cho toàn bộ mô hình dự án.\n\n• Cột cắt Dầm và Sàn\n• Dầm cắt Sàn\n• Tự động đảo thứ tự (Switch Order) nếu đang bị ngược.";
                SetRibbonItemIcon(btnProjectData, Path.GetFileName(iconPathAutoJoin), resourcesPath);
                splitBtn.AddPushButton(btnProjectData);
            }

#if false
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
#endif

#if false
            // ------------------- Rebar Column (Chuyển ngầm qua Rebar Suite) -------------------
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
#endif

            // Các nút của Panel Modeling (Disallow Join, Create Filter, Speed Overrider, Pile Location)
            // đã được chuyển lên nhóm AddStackedItems trong khối khởi tạo modelingPanel ở trên.

#if false
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
#endif

            // ═══════════════════════════════════════════════════════════════
            //  Panel: Layout (Cùng tab với CSI x Revit & Modeling, không tạo tab riêng)
            // ═══════════════════════════════════════════════════════════════
            var targetTabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { tabName };
            try
            {
                var ibimPanels = application.GetRibbonPanels("IBIM OS");
                if (ibimPanels != null && ibimPanels.Count > 0)
                {
                    targetTabs.Add("IBIM OS");
                }
            }
            catch { }

            int tabIdx = 0;
            foreach (var currentTab in targetTabs)
            {
                RibbonPanel layoutPanel = null;
                try
                {
                    layoutPanel = application.CreateRibbonPanel(currentTab, "Layout");
                }
                catch (Exception)
                {
                    try
                    {
                        foreach (var p in application.GetRibbonPanels(currentTab))
                        {
                            if (p.Name == "Layout") { layoutPanel = p; break; }
                        }
                    }
                    catch { }
                }

                if (layoutPanel != null)
                {
                    string iconPathAutoViewSheet = Path.Combine(resourcesPath, "AutoViewSheet.png");
                    if (!File.Exists(iconPathAutoViewSheet))
                    {
                        iconPathAutoViewSheet = Path.Combine(resourcesPath, "AddViewsToSheets.png");
                    }
                    if (!File.Exists(iconPathAutoViewSheet))
                    {
                        iconPathAutoViewSheet = Path.Combine(resourcesPath, "views.png");
                    }

                    string btnId = tabIdx == 0 ? "cmdAutoViewSheet" : $"cmdAutoViewSheet_{tabIdx}";
                    PushButtonData btnAutoViewSheetData = new PushButtonData(
                        btnId,
                        "Auto View\n& Sheet",
                        assemblyPath,
                        "RincoNhan.Tools.AutoViewSheet.AutoViewSheetCommand"
                    );
                    btnAutoViewSheetData.ToolTip = "Tự động tạo View (OVER) và dàn Sheet hàng loạt theo tầng và Zone cho Mặt bằng kết cấu / Loading Plan.";

                    PushButton btnAutoViewSheet = layoutPanel.AddItem(btnAutoViewSheetData) as PushButton;
                    if (btnAutoViewSheet != null && File.Exists(iconPathAutoViewSheet))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathAutoViewSheet, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnAutoViewSheet.LargeImage = img;
                        btnAutoViewSheet.Image = img;
                    }

                    // ------------------- Create View -------------------
                    string iconPathCreateView = Path.Combine(resourcesPath, "CreateView.png");
                    string createViewBtnId = tabIdx == 0 ? "cmdCreateView" : $"cmdCreateView_{tabIdx}";
                    PushButtonData btnCreateViewData = new PushButtonData(
                        createViewBtnId,
                        "Create\nViews",
                        assemblyPath,
                        "JNNTool.Tools.CreateView.Commands.CreateViewCommand"
                    );
                    btnCreateViewData.ToolTip = "Tự động tạo và nhân bản nhiều View theo Level, gán View Template và nhóm cây thư mục Project Browser.";

                    PushButton btnCreateView = layoutPanel.AddItem(btnCreateViewData) as PushButton;
                    if (btnCreateView != null && File.Exists(iconPathCreateView))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathCreateView, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnCreateView.LargeImage = img;
                        btnCreateView.Image = img;
                    }

                    // ------------------- Auto Dim Grid -------------------
                    string iconPathDimGrid = Path.Combine(resourcesPath, "DimGrid.png");
                    string dimGridBtnId = tabIdx == 0 ? "cmdAutoDimGrid" : $"cmdAutoDimGrid_{tabIdx}";
                    PushButtonData btnDimGridData = new PushButtonData(
                        dimGridBtnId,
                        "Dim\nGrid",
                        assemblyPath,
                        "JNNTool.Tools.AutoDimGrid.AutoDimGridCommand"
                    );
                    btnDimGridData.ToolTip = "Auto Dim Grid: Dim tự động lưới trục 4 cạnh (2 hoặc 3 lớp: Tường / Trục / Tổng), hoặc Dim thủ công theo tường + điểm chọn.";

                    PushButton btnDimGrid = layoutPanel.AddItem(btnDimGridData) as PushButton;
                    if (btnDimGrid != null && File.Exists(iconPathDimGrid))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathDimGrid, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnDimGrid.LargeImage = img;
                        btnDimGrid.Image = img;
                    }

                    // ------------------- Auto Dim Column -------------------
                    string iconPathDimCol = Path.Combine(resourcesPath, "DimColumn.png");
                    string dimColBtnId = tabIdx == 0 ? "cmdAutoDimColumn" : $"cmdAutoDimColumn_{tabIdx}";
                    PushButtonData btnDimColData = new PushButtonData(
                        dimColBtnId,
                        "Dim\nCol/Wall",
                        assemblyPath,
                        "JNNTool.Tools.AutoDimColumn.Commands.AutoDimColumnCommand"
                    );
                    btnDimColData.ToolTip = "Auto Dim Column & Wall: Tự động ghi kích thước tiết diện Cột, Vách (b × h / L) và định vị từ Lưới trục (Grid) trên mặt bằng kết cấu.";

                    PushButton btnDimCol = layoutPanel.AddItem(btnDimColData) as PushButton;
                    if (btnDimCol != null && File.Exists(iconPathDimCol))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathDimCol, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnDimCol.LargeImage = img;
                        btnDimCol.Image = img;
                    }

                    // ------------------- Element Mark -------------------
                    string iconPathElementMark = Path.Combine(resourcesPath, "ElementMark.png");
                    string elementMarkBtnId = tabIdx == 0 ? "cmdElementMark" : $"cmdElementMark_{tabIdx}";
                    PushButtonData btnElementMarkData = new PushButtonData(
                        elementMarkBtnId,
                        "Element\nMark",
                        assemblyPath,
                        "JNNTool.Tools.ElementMark.ElementMarkCommand"
                    );
                    btnElementMarkData.ToolTip = "Đánh Mark tự động cho Dầm, Cột, Sàn theo quy tắc người dùng.\n\n" +
                                                 "• Dầm: DS1X01, dầm liên tục nhiều nhịp DS1X01.1, DS1X01.2…\n" +
                                                 "• Cột: C1, C2… dùng chung từ móng đến mái\n" +
                                                 "• Sàn: S1 cho mọi sàn thuộc Level 1\n" +
                                                 "• Hướng đánh số Trái/Phải, Trên/Dưới theo view đang mở";

                    PushButton btnElementMark = layoutPanel.AddItem(btnElementMarkData) as PushButton;
                    if (btnElementMark != null && File.Exists(iconPathElementMark))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathElementMark, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnElementMark.LargeImage = img;
                        btnElementMark.Image = img;
                    }

#if false
                    // ------------------- Stair Model (Chuyển sang panel Stair) -------------------
                    string iconPathStairModelLayout = Path.Combine(resourcesPath, "StairModel.png");
                    if (!File.Exists(iconPathStairModelLayout))
                    {
                        iconPathStairModelLayout = Path.Combine(resourcesPath, "StairDetail.png");
                    }
                    string stairModelBtnId = tabIdx == 0 ? "cmdStairModel_Layout" : $"cmdStairModel_Layout_{tabIdx}";
                    PushButtonData btnStairModelLayoutData = new PushButtonData(
                        stairModelBtnId,
                        "Stair\nModel",
                        assemblyPath,
                        "JNNTool.Tools.StairModel.Commands.StairModelCommand"
                    );
                    btnStairModelLayoutData.ToolTip = "Tự động dựng cầu thang kết cấu bê tông cốt thép (Straight, U-shaped, L-shaped) gồm bản thang nghiêng, chiếu nghỉ và dầm đỡ với preview 2D trực quan.";

                    PushButton btnStairModelLayout = layoutPanel.AddItem(btnStairModelLayoutData) as PushButton;
                    if (btnStairModelLayout != null && File.Exists(iconPathStairModelLayout))
                    {
                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.UriSource = new Uri(iconPathStairModelLayout, UriKind.Absolute);
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        btnStairModelLayout.LargeImage = img;
                        btnStairModelLayout.Image = img;
                    }
#endif
                }
                tabIdx++;
            }

            // ═══════════════════════════════════════════════════════════════
            //  Panel: Stair (Bộ công cụ Thiết kế & Bố trí Thép Cầu Thang)
            // ═══════════════════════════════════════════════════════════════
            try
            {
                RibbonPanel stairPanel = application.CreateRibbonPanel(tabName, "Stair");

                // ------------------- Stair Model -------------------
                string iconPathStairModel = Path.Combine(resourcesPath, "StairModel.png");
                if (!File.Exists(iconPathStairModel))
                {
                    iconPathStairModel = Path.Combine(resourcesPath, "StairDetail.png");
                }
                PushButtonData btnStairModelData = new PushButtonData(
                    "cmdStairModel",
                    "Stair\nModel",
                    assemblyPath,
                    "JNNTool.Tools.StairModel.Commands.StairModelCommand"
                );
                btnStairModelData.ToolTip = "Tự động dựng cầu thang kết cấu bê tông cốt thép (Straight, U-shaped, L-shaped) gồm bản thang nghiêng, chiếu nghỉ và dầm đỡ với preview 2D trực quan.";

                PushButton btnStairModel = stairPanel.AddItem(btnStairModelData) as PushButton;
                if (btnStairModel != null && File.Exists(iconPathStairModel))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(iconPathStairModel, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    btnStairModel.LargeImage = img;
                    btnStairModel.Image = img;
                }

                // ------------------- Stair Designer -------------------
                string iconPathStairDesigner = Path.Combine(resourcesPath, "StairModel.png");
                PushButtonData btnStairDesignerData = new PushButtonData(
                    "cmdStairDesigner",
                    "Stair\nDesigner",
                    assemblyPath,
                    "JNN.Stair.Commands.StairDesignerCommand"
                );
                btnStairDesignerData.ToolTip = "Thiết kế tham số hình học cầu thang 3D (Thẳng, U-Turn, L-Shape, Rẻ quạt Winder), dầm chiếu nghỉ, tự động dựng Solid và xuất nạp Family (.rfa).";

                PushButton btnStairDesigner = stairPanel.AddItem(btnStairDesignerData) as PushButton;
                if (btnStairDesigner != null && File.Exists(iconPathStairDesigner))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(iconPathStairDesigner, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    btnStairDesigner.LargeImage = img;
                    btnStairDesigner.Image = img;
                }

#if false
                // ------------------- Stair Rebar -------------------
                string iconPathStairRebar = Path.Combine(resourcesPath, "StairDetail.png");
                if (!File.Exists(iconPathStairRebar))
                {
                    iconPathStairRebar = Path.Combine(resourcesPath, "RebarStair.png");
                }
                PushButtonData btnStairRebarData = new PushButtonData(
                    "cmdStairRebarSuiteNew",
                    "Stair\nRebar",
                    assemblyPath,
                    "JNN.Stair.Commands.StairDetailingCommand"
                );
                btnStairRebarData.ToolTip = "Bố trí & uốn cốt thép thang tự động 3D/2D: Đan chéo chịu kéo tại góc lõm chống bung nổ bê tông theo TCVN 5574:2018.";

                PushButton btnStairRebar = stairPanel.AddItem(btnStairRebarData) as PushButton;
                if (btnStairRebar != null && File.Exists(iconPathStairRebar))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(iconPathStairRebar, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    btnStairRebar.LargeImage = img;
                    btnStairRebar.Image = img;
                }
#endif
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi tạo Ribbon Panel Stair", ex);
            }

            // ═══════════════════════════════════════════════════════════════
            //  Panel: Quantity (Bóc tách khối lượng & 3D Section Box) trên tab JNNTool
            // ═══════════════════════════════════════════════════════════════
            try
            {
                RibbonPanel quantityPanel = application.CreateRibbonPanel(tabName, "Quantity");

                string iconPathQto = Path.Combine(resourcesPath, "QuantityTakeoff.png");
                PushButtonData btnQtoData = new PushButtonData(
                    "cmdQuantityTakeoff",
                    "Quantity\nTakeoff",
                    assemblyPath,
                    "JNNTool.Tools.QuantityTakeoff.Commands.QuantityTakeoffCommand"
                );
                btnQtoData.ToolTip = "Bóc tách khối lượng toàn diện (Bê tông, Ván khuôn, Thép, Diện tích, Chiều dài) theo từng tầng và cấu kiện.\n\n• Tạo Box 3D lập tức cho tầng hoặc cấu kiện\n• Xuất bảng tính Excel đa Sheet chuyên nghiệp\n• Lọc theo Category và Level linh hoạt";

                PushButton btnQto = quantityPanel.AddItem(btnQtoData) as PushButton;
                if (btnQto != null && File.Exists(iconPathQto))
                {
                    BitmapImage img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(iconPathQto, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    btnQto.LargeImage = img;
                    btnQto.Image = img;
                }
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi tạo Ribbon Panel Quantity", ex);
            }



            // ─── Tự động nạp theme Ribbon (Mặc định áp dụng bộ Vector Icon siêu sắc nét 3x) ───
            try
            {
                var savedTheme = JNNTool.Tools.UIManager.Models.RibbonThemeSettings.Load() 
                              ?? new JNNTool.Tools.UIManager.Models.RibbonThemeSettings();
                savedTheme.UseVectorTheme = true;
                JNNTool.Tools.UIManager.Services.RibbonThemeService.ApplyThemeToRibbon(application, savedTheme);
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi tự động nạp Ribbon Theme", ex);
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            return Result.Succeeded;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var requestedName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(requestedName)) return null;

                // 0. Bỏ qua các assembly Revit và hệ thống để tránh xung đột phiên bản và crash CLR
                if (requestedName.StartsWith("RevitAPI", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("RevitServices", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                    requestedName.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // 1. Ưu tiên tìm trong thư mục chứa JNNTool.dll (bundle của phiên bản Revit hiện hành)
                string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(currentDir))
                {
                    string targetFile = Path.Combine(currentDir, requestedName + ".dll");
                    if (File.Exists(targetFile))
                    {
                        try
                        {
                            return Assembly.LoadFrom(targetFile);
                        }
                        catch { }
                    }
                }

                // 2. Tìm trong các assembly đã nạp sẵn trong AppDomain
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(asm.GetName().Name, requestedName, StringComparison.OrdinalIgnoreCase))
                    {
                        return asm;
                    }
                }
            }
            catch { }

            return null;
        }

        private static void SetRibbonItemIcon(RibbonItemData data, string iconFileName, string resourcesPath)
        {
            try
            {
                string iconPath = Path.Combine(resourcesPath, iconFileName);
                if (!File.Exists(iconPath))
                {
                    string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string parentDir = Directory.GetParent(baseDir)?.FullName ?? baseDir;
                    string toolName = Path.GetFileNameWithoutExtension(iconFileName);
                    string toolIcon = Path.Combine(parentDir, "Tools", toolName, iconFileName);
                    if (File.Exists(toolIcon)) iconPath = toolIcon;
                }

                if (File.Exists(iconPath))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(iconPath, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    img.Freeze();

                    if (data is PushButtonData pbd)
                    {
                        pbd.LargeImage = img;
                        pbd.Image = img;
                    }
                    else if (data is SplitButtonData sbd)
                    {
                        sbd.LargeImage = img;
                        sbd.Image = img;
                    }
                    else if (data is PulldownButtonData pud)
                    {
                        pud.LargeImage = img;
                        pud.Image = img;
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>Model nhẹ để đọc version.json (tránh dependency nặng).</summary>
    internal sealed class VersionInfo
    {
        [Newtonsoft.Json.JsonProperty("version")]
        public string Version { get; set; } = "0.0.0";

        [Newtonsoft.Json.JsonProperty("manifestUrl")]
        public string ManifestUrl { get; set; } = "";
    }
}
