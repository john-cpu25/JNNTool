using System.Collections.Generic;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Mô tả một lệnh của JNNTool dùng cho Ribbon và Command Palette.
    /// </summary>
    public sealed class ToolCommandInfo
    {
        public ToolCommandInfo(string id, string ribbonText, string title, string group,
                               string className, string description, string? iconFile = null,
                               string? keywords = null)
        {
            Id = id;
            RibbonText = ribbonText;
            Title = title;
            Group = group;
            ClassName = className;
            Description = description;
            IconFile = iconFile;
            Keywords = keywords ?? string.Empty;
        }

        public string Id { get; }
        public string RibbonText { get; }
        public string Title { get; }
        public string Group { get; }
        public string ClassName { get; }
        public string Description { get; }
        public string? IconFile { get; }
        public string Keywords { get; }
    }

    /// <summary>
    /// Danh mục toàn bộ lệnh của JNNTool phục vụ tra cứu và chạy nhanh từ Command Palette.
    /// </summary>
    internal static class CommandRegistry
    {
        public const string TabName = "JNNTool";

        public static readonly IReadOnlyList<ToolCommandInfo> Commands = new List<ToolCommandInfo>
        {
            // ───────────────── Panel: General ─────────────────
            new ToolCommandInfo(
                "cmdPalette", "Command\nPalette", "Command Palette", "General",
                "JNNTool.Core.Palette.TogglePaletteCmd",
                "Trung tâm điều khiển (Ctrl+F) tra cứu và thực thi nhanh toàn bộ công cụ JNNTool.",
                "CommandPalette.png", "command palette launcher tim kiem lenh search control center"
            ),
            new ToolCommandInfo(
                "cmdLicense", "Account\nLicense", "Account & License", "General",
                "JNNTool.Tools.Login.Commands.LoginCmd",
                "Quản lý bản quyền, đăng nhập tài khoản và kích hoạt JNNTool.",
                "Login.png", "license ban quyen dang nhap account active tai khoan"
            ),
            new ToolCommandInfo(
                "cmdUIManager", "Ribbon\nTheme", "Ribbon Theme", "General",
                "JNNTool.Tools.UIManager.Commands.UIManagerCmd",
                "Tùy biến màu sắc theme và kiểu hiển thị Icon trên thanh Ribbon JNNTool.",
                "UIManager.png", "theme mau ribbon icon style palette giao dien color"
            ),

            // ───────────────── Panel: Modeling ─────────────────
            new ToolCommandInfo(
                "cmdSplitBeamByColumn", "Split\nBeam", "Split Beam", "Modeling",
                "JNNTool.SplitBeamByColumnCmd",
                "Ngắt dầm thành từng đoạn tại vị trí các cột hoặc dầm cắt ngang.",
                "SplitBeam.png", "split ngat cat dam column beam phan doan"
            ),
            new ToolCommandInfo(
                "cmdCreateSection", "Create\nSection", "Create Section", "Modeling",
                "JNNTool.CreateSectionCmd",
                "Duplicate hàng loạt type dầm/cột với kích thước b×h mới.",
                "CreateSection.png", "tao tiet dien section duplicate size dam cot kich thuoc"
            ),
            new ToolCommandInfo(
                "cmdRebarColumn", "Rebar\nColumn", "Rebar Column (Quick)", "Modeling",
                "JNNTool.Tools.RebarColumn.Command",
                "Bố trí thép cột nhanh (thép dọc, đai, nối thép).",
                "RebarColumn.png", "thep cot rebar column dai thep bo tri nhanh"
            ),
            new ToolCommandInfo(
                "cmdDisallowJoinBeam", "Disallow\nJoin Beam", "Disallow Join Beam", "Modeling",
                "JNNTool.Tools.DisallowJoinBeam.DisallowJoinBeamCmd",
                "Quét chọn nhiều dầm và thực hiện Disallow Join ở 2 đầu.",
                "DisallowJoinBeam.png", "disallow join khong noi dam end beam dau dam"
            ),
            new ToolCommandInfo(
                "cmdCreateFilter", "Create\nFilter", "Create Filter", "Modeling",
                "JNNTool.Tools.CreateFilter.Command",
                "Tạo bộ lọc tự động từ danh sách phần tử và gán màu nhanh.",
                "CreateFilter.png", "filter bo loc mau sac view color loc"
            ),
            new ToolCommandInfo(
                "cmdSpeedOverriderElement", "Speed\nOverrider", "Speed Overrider", "Modeling",
                "JNNTool.Tools.SpeedOverriderElement.Command",
                "Override hiển thị đồ họa phần tử nhanh trong view hiện hành.",
                "SpeedOverrider.png", "speed overrider mau net graphic override to mau"
            ),
            new ToolCommandInfo(
                "cmdPileLocation", "Pile\nLocation", "Pile Location", "Modeling",
                "JNNTool.Tools.PileLocation.Command",
                "Tự động tìm cọc trong view và ghi tọa độ thực tế (mm) vào parameter.",
                "PileLocation.png", "toa do coc pile location toa do x y coordinate dinh vi coc"
            ),
            new ToolCommandInfo(
                "cmdFloorByRoom", "Floor By\nRoom", "Floor By Room", "Modeling",
                "JNNTool.FloorByRoomCmd",
                "Vẽ sàn tự động theo Boundary của Room.",
                "FloorByRoom.png", "ve san floor by room phong bien dang room boundary"
            ),
            new ToolCommandInfo(
                "cmdRebarBeam", "Rebar\nBeam", "Rebar Beam (Quick)", "Modeling",
                "JNNTool.RebarBeamCmd",
                "Bố trí thép dầm liên tục (Lớp trên, dưới, và đai theo L0/4).",
                "RebarBeam.png", "thep dam rebar beam dai lop tren duoi"
            ),
            new ToolCommandInfo(
                "cmdConnection", "Steel\nConnection", "Steel Connection", "Modeling",
                "JNNTool.Tools.Connection.Command",
                "Tạo liên kết thép (Steel Connection) cho dầm và cột.",
                "Connection.png", "lien ket thep connection steel joint ban ma bu long"
            ),
            new ToolCommandInfo(
                "cmdClashDetection", "Clash\nDetection", "Clash Detection", "Modeling",
                "JNNTool.Tools.ClashDetection.Command",
                "Kiểm tra va chạm giữa model và Revit Link.",
                "ClashDetection.png", "va cham clash detection link collision xung dot"
            ),
            new ToolCommandInfo(
                "cmdCreateDimBeam", "Dim\nBeam", "Dim Beam", "Modeling",
                "JNNTool.Tools.CreateDim.CreateDimBeamCmd",
                "Tự tạo Dim chiều dài tổng 2 đầu dầm trên mặt bằng.",
                "CreateDim.png", "dim dam create dim beam chieu dai 2 dau"
            ),
            new ToolCommandInfo(
                "cmdExtendBeam", "Extend\nBeam", "Extend Beam", "Modeling",
                "JNNTool.Tools.ExtendBeam.ExtendBeamCmd",
                "Nhận diện gối ở 2 đầu và đưa tim dầm tới đúng mặt gối chính xác.",
                "ExtendBeam.png", "extend trim keo dai dam goi support mat goi tim dam"
            ),

            // ───────────────── Panel: CSI x Revit ─────────────────
            new ToolCommandInfo(
                "cmdImportE2K", "Import\nE2K", "Import E2K", "CSI x Revit",
                "JNNTool.Tools.CSIxRevit.Commands.ImportE2KCommand",
                "Import mô hình từ file E2K (ETABS/SAP2000) vào Revit.",
                "ImportE2K.png", "import etabs e2k sap csi chuyen mo hinh etabs to revit"
            ),
            new ToolCommandInfo(
                "cmdFitScopeBox", "Fit\nScope Box", "Fit Scope Box", "CSI x Revit",
                "JNNTool.Tools.CSIxRevit.Commands.FitScopeBoxCommand",
                "Khóa và cắt khớp toàn bộ Lưới trục và Cao độ theo Scope Box.",
                "DimGrid.png", "fit scope box grid level cat truc khoa truc hop gioi han"
            ),
            new ToolCommandInfo(
                "cmdBoundaryAlign", "Căn Lề\nBiên", "Boundary Align", "CSI x Revit",
                "JNNTool.Tools.CSIxRevit.Commands.BoundaryAlignCommand",
                "Dời dầm và cột biên vào trong ranh đất / lưới trục, co kéo dầm ngang.",
                "ExtendBeam.png", "can le bien boundary align ranh dat mep bien can mep"
            ),
            new ToolCommandInfo(
                "cmdAutoJoinActiveView", "Auto Join\n(View)", "Auto Join (Active View)", "CSI x Revit",
                "JNNTool.Tools.AutoJoin.AutoJoinActiveViewCommand",
                "Tự động Join và đảo thứ tự ưu tiên (Column > Beam > Floor) trong View hiện hành.",
                "AutoJoin.png", "auto join view cat cot dam san switch order active view"
            ),
            new ToolCommandInfo(
                "cmdAutoJoinProject", "Auto Join\n(Project)", "Auto Join (Entire Project)", "CSI x Revit",
                "JNNTool.Tools.AutoJoin.AutoJoinProjectCommand",
                "Tự động Join và đảo thứ tự ưu tiên cho toàn bộ mô hình dự án.",
                "AutoJoin.png", "auto join project toan bo mo hinh join toan bo"
            ),

            // ───────────────── Panel: Layout ─────────────────
            new ToolCommandInfo(
                "cmdAutoViewSheet", "Auto View\n& Sheet", "Auto View & Sheet", "Layout",
                "RincoNhan.Tools.AutoViewSheet.AutoViewSheetCommand",
                "Tự động tạo View (OVER) và dàn Sheet hàng loạt theo tầng và Zone.",
                "AutoViewSheet.png", "tao view dan sheet auto view sheet over loading plan ban ve"
            ),
            new ToolCommandInfo(
                "cmdAutoDimGrid", "Dim\nGrid", "Dim Grid", "Layout",
                "JNNTool.Tools.AutoDimGrid.AutoDimGridCommand",
                "Dim tự động lưới trục 4 cạnh (2 hoặc 3 lớp: Tường / Trục / Tổng).",
                "DimGrid.png", "auto dim grid luoi truc kich thuoc tong ghi kich thuoc truc"
            ),
            new ToolCommandInfo(
                "cmdAutoDimColumn", "Dim\nCol/Wall", "Dim Column & Wall", "Layout",
                "JNNTool.Tools.AutoDimColumn.Commands.AutoDimColumnCommand",
                "Ghi kích thước tiết diện Cột, Vách và định vị từ Lưới trục trên mặt bằng.",
                "DimColumn.png", "dim column wall cot vach dinh vi truc mat bang ket cau"
            ),
            new ToolCommandInfo(
                "cmdElementMark", "Element\nMark", "Element Mark", "Layout",
                "JNNTool.Tools.ElementMark.ElementMarkCommand",
                "Đánh Mark tự động cho Dầm, Cột, Sàn theo quy tắc dự án.",
                "ElementMark.png", "danh mark auto element mark ten cau kien ds c s"
            ),

            // ───────────────── Panel: Stair ─────────────────
            new ToolCommandInfo(
                "cmdStairModel", "Stair\nModel", "Stair Model", "Stair",
                "JNNTool.Tools.StairModel.Commands.StairModelCommand",
                "Dựng cầu thang kết cấu BTCT (Thẳng, U, L) kèm chiếu nghỉ và dầm đỡ.",
                "StairModel.png", "dung thang cau thang stair model chieu nghi dam do 2d preview"
            ),
            new ToolCommandInfo(
                "cmdStairDesigner", "Stair\nDesigner", "Stair Designer", "Stair",
                "JNN.Stair.Commands.StairDesignerCommand",
                "Thiết kế tham số hình học cầu thang 3D Solid và xuất Family (.rfa).",
                "StairModel.png", "thiet ke thang stair designer 3d solid family rfa winder"
            ),
            new ToolCommandInfo(
                "cmdStairRebarSuiteNew", "Stair\nRebar", "Stair Rebar", "Stair",
                "JNN.Stair.Commands.StairDetailingCommand",
                "Bố trí uốn cốt thép thang đan chéo góc lõm chống bung nổ bê tông theo TCVN 5574.",
                "StairDetail.png", "thep thang stair rebar uon thep goc lom dan cheo"
            ),

            // ───────────────── Panel: Quantity ─────────────────
            new ToolCommandInfo(
                "cmdQuantityTakeoff", "Quantity\nTakeoff", "Quantity Takeoff", "Quantity",
                "JNNTool.Tools.QuantityTakeoff.Commands.QuantityTakeoffCommand",
                "Bóc tách khối lượng toàn diện (Bê tông, Ván khuôn, Thép) và tạo 3D Box.",
                "QuantityTakeoff.png", "boc tach khoi luong quantity takeoff qto excel be tong van khuon thep"
            ),

            // ───────────────── Panel: Rebar Suite ─────────────────
            new ToolCommandInfo(
                "cmdSlabRebar", "Rebar\nSlab", "Rebar Slab", "Rebar Suite",
                "JNNTool.RebarSuite.Slab.SlabRebarCommand",
                "Bố trí thép sàn tự động: Lớp dưới, lớp trên và thép mũ gối L/4 theo TCVN 5574.",
                "RebarSlab.png", "thep san rebar slab lop duoi lop tren thep mu l4"
            ),
            new ToolCommandInfo(
                "cmdBeamRebarSuite", "Rebar\nBeam", "Rebar Beam Suite", "Rebar Suite",
                "JNNTool.RebarSuite.Beam.BeamRebarCommand",
                "Bố trí thép dầm liên tục: Thép chủ, gia cường gối L/4, đai dày/thưa và tạo mặt cắt tự động.",
                "RebarBeam.png", "thep dam rebar beam chu gia cuong dai mat cat lien tuc"
            ),
            new ToolCommandInfo(
                "cmdColumnRebarSuite", "Rebar\nColumn", "Rebar Column Suite", "Rebar Suite",
                "JNNTool.RebarSuite.Column.ColumnRebarCommand",
                "Bố trí thép chuỗi cột theo tầng: Bẻ cổ chai 1:6, đai dày/thưa, đai AB/CN và tạo MCD.",
                "RebarColumn.png", "thep chuoi cot rebar column co chai dai ab cn mcd chuoi tang"
            ),
            new ToolCommandInfo(
                "cmdWallRebarSuite", "Rebar\nWall", "Rebar Wall", "Rebar Suite",
                "JNNTool.RebarSuite.Wall.WallRebarCommand",
                "Bố trí thép vách 2 lớp: Thép đứng, thép ngang, đai C ghim biên, chống thấm.",
                "RebarWall.png", "thep vach rebar wall dung ngang dai c bien chong tham"
            ),
            new ToolCommandInfo(
                "cmdFoundationRebarSuite", "Rebar\nFooting", "Rebar Footing", "Rebar Suite",
                "JNNTool.RebarSuite.Foundation.FoundationRebarCommand",
                "Bố trí thép móng & đài cọc: Lưới đáy móc uốn 90°, lưới trên, thép chờ cột chân vịt.",
                "RebarFoundation.png", "thep mong dai coc rebar foundation footing luoi day thep cho"
            ),
            new ToolCommandInfo(
                "cmdPileRebarSuite", "Rebar\nPile", "Rebar Pile", "Rebar Suite",
                "JNNTool.RebarSuite.Pile.PileRebarCommand",
                "Bố trí thép cọc khoan nhồi / cọc vuông: Lồng thép, đai phân vùng và xuất tọa độ CSV.",
                "RebarPile.png", "thep coc rebar pile khoan nhoi coc vuong dai long thep"
            ),
            new ToolCommandInfo(
                "cmdStairRebarSuite", "Rebar\nStair", "Rebar Stair Suite", "Rebar Suite",
                "JNNTool.RebarSuite.Stair.StairRebarCommand",
                "Bố trí thép bản thang: Thép lớp dưới chịu nhịp, thép mũ gối trên neo dầm.",
                "RebarStair.png", "thep thang rebar stair ban thang neo dam"
            ),
            new ToolCommandInfo(
                "cmdRebarVolume", "Rebar\nVolume", "Rebar Volume (CSV)", "Rebar Suite",
                "JNNTool.RebarSuite.Utilities.Commands.RebarVolumeCommand",
                "Thống kê khối lượng thép (CSV) theo đường kính, cấu kiện và tầng.",
                "RebarUtils.png", "thong ke thep rebar volume khoi luong csv duong kinh"
            ),
            new ToolCommandInfo(
                "cmdRebarNumber", "Rebar\nNumber", "Auto Mark Rebar", "Rebar Suite",
                "JNNTool.RebarSuite.Utilities.Commands.RebarNumberCommand",
                "Đánh số hiệu thép tự động (Auto Mark).",
                "RebarUtils.png", "danh so thep rebar number mark so hieu"
            ),
            new ToolCommandInfo(
                "cmdRebarUnobscured", "View\nUnobscured", "View Unobscured", "Rebar Suite",
                "JNNTool.RebarSuite.Utilities.Commands.RebarUnobscuredCommand",
                "Bật/Tắt hiển thị nhìn xuyên cốt thép (View Unobscured) trong 3D.",
                "RebarUtils.png", "nhin xuyen unobscured rebar 3d hien thi thep"
            ),
            new ToolCommandInfo(
                "cmdRebarIsolate", "Isolate\nRebar", "Isolate Rebar", "Rebar Suite",
                "JNNTool.RebarSuite.Utilities.Commands.IsolateRebarCommand",
                "Cô lập hiển thị cốt thép trong view hiện hành.",
                "RebarUtils.png", "co lap thep isolate rebar view an hien"
            ),
            new ToolCommandInfo(
                "cmdRebarTypeManager", "Rebar\nManager", "Rebar Type Manager", "Rebar Suite",
                "JNNTool.RebarSuite.Utilities.Commands.RebarTypeManagerCommand",
                "Chuẩn hóa và tạo mới các RebarBarType D6-D32, HookType, CoverType.",
                "RebarUtils.png", "chuan hoa rebar type d6 d32 hook cover loai thep"
            ),
        };

        public static string GetPostableId(ToolCommandInfo cmd)
            => $"CustomCtrl_%CustomCtrl_%{TabName}%{cmd.Group}%{cmd.Id}";
    }
}
