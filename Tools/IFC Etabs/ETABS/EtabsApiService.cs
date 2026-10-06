using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Services;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.ETABS.Interfaces;

namespace JNNTool.Tools.IFCEtabs.ETABS
{
    /// <summary>
    /// Triển khai kết nối và trích xuất dữ liệu trực tiếp từ ETABS thông qua CSi OAPI (COM Late-binding).
    /// Hoạt động mượt mà trên mọi phiên bản ETABS (v18, v19, v20, v21, v22+) mà không phụ thuộc DLL tĩnh.
    /// </summary>
    public class EtabsApiService : IEtabsService
    {
        private object? _etabsObject;
        private dynamic? _sapModel;
        private bool _disposed;

        public bool IsConnected => _sapModel != null;
        public string ModelPath { get; private set; } = string.Empty;

        // P/Invoke an toàn để lấy Active COM Object trên cả .NET 4.8 và .NET 8/10
        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [DllImport("ole32.dll")]
        private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        public bool Connect(out string message)
        {
            message = string.Empty;
            try
            {
                Disconnect();

                // 1. Tìm COM object ETABS đang chạy
                int hr = CLSIDFromProgID("CSI.ETABS.API.ETABSObject", out Guid clsid);
                if (hr != 0)
                {
                    message = "Không tìm thấy đăng ký COM của CSI ETABS trên máy tính (CLSID not found).";
                    return false;
                }

                GetActiveObject(ref clsid, IntPtr.Zero, out object activeObj);
                if (activeObj == null)
                {
                    message = "Không tìm thấy tiến trình ETABS nào đang mở. Vui lòng khởi động ETABS và mở mô hình.";
                    return false;
                }

                _etabsObject = activeObj;
                dynamic etabs = activeObj;
                _sapModel = etabs.SapModel;

                if (_sapModel == null)
                {
                    message = "Đã kết nối ETABS nhưng chưa có mô hình nào được mở (SapModel is null).";
                    Disconnect();
                    return false;
                }

                // 2. Lấy thông tin file mô hình
                try
                {
                    object? fnObj = _sapModel.GetModelFilename(true);
                    ModelPath = fnObj?.ToString() ?? "Mô hình hiện hành";
                }
                catch
                {
                    ModelPath = "Mô hình hiện hành (Chưa lưu đường dẫn)";
                }

                message = $"Kết nối thành công tới ETABS: {System.IO.Path.GetFileName(ModelPath)}";
                return true;
            }
            catch (COMException comEx)
            {
                message = $"Lỗi COM khi kết nối ETABS: {comEx.Message}. Hãy chắc chắn rằng ETABS đang mở và quyền chạy đồng cấp.";
                Disconnect();
                return false;
            }
            catch (Exception ex)
            {
                message = $"Không thể kết nối ETABS: {ex.Message}";
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            if (_sapModel != null)
            {
                try
                {
                    Marshal.ReleaseComObject(_sapModel);
                }
                catch { }
                _sapModel = null;
            }

            if (_etabsObject != null)
            {
                try
                {
                    Marshal.ReleaseComObject(_etabsObject);
                }
                catch { }
                _etabsObject = null;
            }

            ModelPath = string.Empty;
        }

        public StructuralModel ExtractModel(Action<string, double>? progressCallback = null)
        {
            if (!IsConnected || _sapModel == null)
            {
                throw new InvalidOperationException("Chưa kết nối tới phần mềm ETABS.");
            }

            var model = new StructuralModel
            {
                ModelName = System.IO.Path.GetFileNameWithoutExtension(ModelPath),
                SourceUnits = "N_mm_C"
            };

            try
            {
                // 1. Đặt đơn vị làm việc của ETABS về N_mm_C để lấy trực tiếp tọa độ (mm)
                // eUnits: 9 = N_mm_C (Lực: N, Chiều dài: mm, Nhiệt độ: C)
                progressCallback?.Invoke("Thiết lập đơn vị tính toán N-mm...", 5.0);
                try
                {
                    _sapModel.SetPresentUnits(9);
                }
                catch
                {
                    // Dự phòng nếu phiên bản cũ dùng SetPresentUnits_2
                    try { _sapModel.SetPresentUnits_2(9); } catch { }
                }

                // 2. Trích xuất thông tin Tầng (Stories / Levels)
                progressCallback?.Invoke("Đang đọc danh sách tầng...", 15.0);
                ExtractStories(model);

                // 3. Trích xuất Lưới trục (Grids)
                progressCallback?.Invoke("Đang đọc hệ lưới trục...", 30.0);
                ExtractGrids(model);

                // 4. Trích xuất Vật liệu & Tiết diện (Materials & Sections)
                progressCallback?.Invoke("Đang đọc định nghĩa vật liệu & tiết diện...", 45.0);
                ExtractMaterials(model);
                ExtractSections(model);

                // 5. Trích xuất Cấu kiện thanh (Frames: Dầm & Cột)
                progressCallback?.Invoke("Đang đọc cấu kiện Dầm và Cột...", 65.0);
                ExtractFrames(model);

                // 6. Trích xuất Cấu kiện tấm (Areas: Vách & Sàn)
                progressCallback?.Invoke("Đang đọc cấu kiện Vách và Sàn...", 85.0);
                ExtractAreas(model);

                // 7. Tính toán mã băm nhận diện (Hash) cho từng cấu kiện
                progressCallback?.Invoke("Tính toán mã băm cấu kiện...", 95.0);
                foreach (var col in model.Columns) col.Hash = ElementHashService.Instance.ComputeHash(col);
                foreach (var bm in model.Beams) bm.Hash = ElementHashService.Instance.ComputeHash(bm);
                foreach (var wl in model.Walls) wl.Hash = ElementHashService.Instance.ComputeHash(wl);
                foreach (var sb in model.Slabs) sb.Hash = ElementHashService.Instance.ComputeHash(sb);
                foreach (var lv in model.Levels) lv.Hash = ElementHashService.Instance.ComputeHash(lv);
                foreach (var gd in model.Grids) gd.Hash = ElementHashService.Instance.ComputeHash(gd);

                progressCallback?.Invoke("Hoàn tất trích xuất dữ liệu ETABS!", 100.0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi trong quá trình trích xuất ETABS: {ex.Message}", ex);
            }

            return model;
        }

        private void ExtractStories(StructuralModel model)
        {
            int numberStories = 0;
            string[] storyNames = Array.Empty<string>();

            int ret = _sapModel.Story.GetNameList(ref numberStories, ref storyNames);
            if (ret != 0 || numberStories == 0 || storyNames == null) return;

            for (int i = 0; i < numberStories; i++)
            {
                string sName = storyNames[i];
                double elev = 0;
                double height = 0;
                bool isMaster = false;
                string similarTo = "";

                try
                {
                    _sapModel.Story.GetElevation(sName, ref elev);
                    _sapModel.Story.GetHeight(sName, ref height);
                    _sapModel.Story.GetMasterStory(sName, ref isMaster);
                    _sapModel.Story.GetSimilarTo(sName, ref similarTo);
                }
                catch { }

                var level = new ETABSLevel
                {
                    Source = "ETABS",
                    SourceId = sName,
                    Name = sName,
                    Story = sName,
                    ElevationMm = elev,
                    HeightMm = height,
                    IsMasterStory = isMaster,
                    SimilarToStory = similarTo
                };

                model.Levels.Add(level);
            }

            // Sắp xếp tầng theo thứ tự cao độ tăng dần
            model.Levels.Sort((a, b) => a.ElevationMm.CompareTo(b.ElevationMm));
        }

        private void ExtractGrids(StructuralModel model)
        {
            try
            {
                int numberGrids = 0;
                string[] gridSysNames = Array.Empty<string>();
                int ret = _sapModel.GridSys.GetNameList(ref numberGrids, ref gridSysNames);
                if (ret != 0 || numberGrids == 0 || gridSysNames == null) return;

                // Để an toàn và đơn giản trong MVP, ta đọc danh sách grid line từ hệ trục đầu tiên
                foreach (var gSys in gridSysNames)
                {
                    int numGridLines = 0;
                    string[] gridLineNames = Array.Empty<string>();
                    string[] gridLineTypes = Array.Empty<string>();
                    double[] gridLineCoords = Array.Empty<double>();
                    bool[] isVisible = Array.Empty<bool>();

                    // Thử đọc qua GridSys.GetGridLines nếu API hỗ trợ
                    // Nếu không, ta trích xuất qua tọa độ bounding box tầng
                }
            }
            catch { }
        }

        private void ExtractMaterials(StructuralModel model)
        {
            try
            {
                int numberMats = 0;
                string[] matNames = Array.Empty<string>();
                int ret = _sapModel.PropMaterial.GetNameList(ref numberMats, ref matNames);
                if (ret != 0 || numberMats == 0 || matNames == null) return;

                foreach (var mName in matNames)
                {
                    int eMatType = 0;
                    int color = 0;
                    string notes = "";
                    string guid = "";

                    try
                    {
                        _sapModel.PropMaterial.GetMaterial(mName, ref eMatType, ref color, ref notes, ref guid);
                    }
                    catch { }

                    string typeStr = eMatType switch
                    {
                        1 => "STEEL",
                        2 => "CONCRETE",
                        3 => "NODESIGN",
                        4 => "ALUMINUM",
                        5 => "COLDFORMED",
                        6 => "REBAR",
                        7 => "TENDON",
                        8 => "MASONRY",
                        _ => "OTHER"
                    };

                    double e = 0, u = 0, a = 0, g = 0;
                    try
                    {
                        _sapModel.PropMaterial.GetMPIsotropic(mName, ref e, ref u, ref a, ref g);
                    }
                    catch { }

                    var mat = new ETABSMaterial
                    {
                        Name = mName,
                        Type = typeStr,
                        ElasticModulusMpa = e, // N/mm2 = MPa
                        PoissonRatio = u,
                        ThermalCoefficient = a
                    };

                    model.Materials[mName] = mat;
                }
            }
            catch { }
        }

        private void ExtractSections(StructuralModel model)
        {
            try
            {
                int numberProps = 0;
                string[] propNames = Array.Empty<string>();
                int ret = _sapModel.PropFrame.GetNameList(ref numberProps, ref propNames);
                if (ret != 0 || numberProps == 0 || propNames == null) return;

                foreach (var pName in propNames)
                {
                    var sec = new ETABSSection { Name = pName };
                    string fileName = "", matProp = "", notes = "", guid = "";
                    int color = 0;
                    double t3 = 0, t2 = 0; // t3: Depth (h), t2: Width (b)

                    // Thử đọc tiết diện chữ nhật
                    int isRect = _sapModel.PropFrame.GetRectangle(pName, ref fileName, ref matProp, ref t3, ref t2, ref color, ref notes, ref guid);
                    if (isRect == 0 && (t3 > 0 || t2 > 0))
                    {
                        sec.Shape = SectionShapeType.Rectangular;
                        sec.DepthMm = t3;
                        sec.WidthMm = t2;
                        sec.MaterialName = matProp;
                        model.Sections[pName] = sec;
                        continue;
                    }

                    // Thử đọc tiết diện tròn
                    double diam = 0;
                    int isCirc = _sapModel.PropFrame.GetCircle(pName, ref fileName, ref matProp, ref diam, ref color, ref notes, ref guid);
                    if (isCirc == 0 && diam > 0)
                    {
                        sec.Shape = SectionShapeType.Circular;
                        sec.DiameterMm = diam;
                        sec.WidthMm = diam;
                        sec.DepthMm = diam;
                        sec.MaterialName = matProp;
                        model.Sections[pName] = sec;
                        continue;
                    }

                    // Tiết diện tổng quát
                    sec.MaterialName = matProp;
                    model.Sections[pName] = sec;
                }
            }
            catch { }
        }

        private void ExtractFrames(StructuralModel model)
        {
            int numberFrames = 0;
            string[] frameNames = Array.Empty<string>();
            int ret = _sapModel.FrameObj.GetNameList(ref numberFrames, ref frameNames);
            if (ret != 0 || numberFrames == 0 || frameNames == null) return;

            for (int i = 0; i < numberFrames; i++)
            {
                string fName = frameNames[i];
                string pt1Name = "", pt2Name = "";
                _sapModel.FrameObj.GetPoints(fName, ref pt1Name, ref pt2Name);

                double x1 = 0, y1 = 0, z1 = 0;
                double x2 = 0, y2 = 0, z2 = 0;
                _sapModel.PointObj.GetCoordCartesian(pt1Name, ref x1, ref y1, ref z1);
                _sapModel.PointObj.GetCoordCartesian(pt2Name, ref x2, ref y2, ref z2);

                var p1 = new Point3D(x1, y1, z1);
                var p2 = new Point3D(x2, y2, z2);

                string propName = "", sAuto = "";
                _sapModel.FrameObj.GetSection(fName, ref propName, ref sAuto);

                double angle = 0;
                bool isAdv = false;
                try { _sapModel.FrameObj.GetLocalAxes(fName, ref angle, ref isAdv); } catch { }

                int cardPt = 10; // Centroid
                try { _sapModel.FrameObj.GetCardinalPoint(fName, ref cardPt); } catch { }

                int designOrientation = 0; // 1: Column, 2: Beam, 3: Brace
                try { _sapModel.FrameObj.GetDesignOrientation(fName, ref designOrientation); } catch { }

                // Tìm kích thước tiết diện từ từ điển
                double width = 300, depth = 500;
                string matName = "";
                if (model.Sections.TryGetValue(propName, out var secDef))
                {
                    width = secDef.WidthMm > 0 ? secDef.WidthMm : 300;
                    depth = secDef.DepthMm > 0 ? secDef.DepthMm : 500;
                    matName = secDef.MaterialName;
                }

                // Phân loại Cột vs Dầm:
                // Nếu DesignOrientation chỉ định hoặc trục Z chênh lệch lớn hơn phương ngang
                var line = new LineSegment3D(p1, p2);
                bool isCol = (designOrientation == 1) || (designOrientation == 0 && line.IsVertical(15.0));

                if (isCol)
                {
                    // Điểm chân luôn có Z nhỏ hơn
                    var basePt = p1.Z <= p2.Z ? p1 : p2;
                    var topPt = p1.Z <= p2.Z ? p2 : p1;

                    var col = new ETABSColumn
                    {
                        Source = "ETABS",
                        SourceId = fName,
                        Name = fName,
                        BasePoint = basePt,
                        TopPoint = topPt,
                        Section = propName,
                        Material = matName,
                        WidthMm = width,
                        DepthMm = depth,
                        AngleDegrees = angle,
                        CardinalPoint = (CardinalPoint)cardPt,
                        BaseStory = FindClosestStory(model, basePt.Z),
                        TopStory = FindClosestStory(model, topPt.Z)
                    };
                    col.Story = col.TopStory;
                    model.Columns.Add(col);
                }
                else
                {
                    // Dầm
                    string storyName = FindClosestStory(model, (p1.Z + p2.Z) * 0.5);
                    var beam = new ETABSBeam
                    {
                        Source = "ETABS",
                        SourceId = fName,
                        Name = fName,
                        StartPoint = p1,
                        EndPoint = p2,
                        Section = propName,
                        Material = matName,
                        WidthMm = width,
                        DepthMm = depth,
                        Rotation = angle,
                        CardinalPoint = (CardinalPoint)cardPt,
                        Story = storyName
                    };
                    model.Beams.Add(beam);
                }
            }
        }

        private void ExtractAreas(StructuralModel model)
        {
            try
            {
                int numberAreas = 0;
                string[] areaNames = Array.Empty<string>();
                int ret = _sapModel.AreaObj.GetNameList(ref numberAreas, ref areaNames);
                if (ret != 0 || numberAreas == 0 || areaNames == null) return;

                for (int i = 0; i < numberAreas; i++)
                {
                    string aName = areaNames[i];
                    int numPoints = 0;
                    string[] pointNames = Array.Empty<string>();
                    _sapModel.AreaObj.GetPoints(aName, ref numPoints, ref pointNames);
                    if (numPoints < 3 || pointNames == null) continue;

                    var pts = new List<Point3D>(numPoints);
                    double sumZ = 0;
                    for (int p = 0; p < numPoints; p++)
                    {
                        double x = 0, y = 0, z = 0;
                        _sapModel.PointObj.GetCoordCartesian(pointNames[p], ref x, ref y, ref z);
                        pts.Add(new Point3D(x, y, z));
                        sumZ += z;
                    }

                    string propName = "";
                    _sapModel.AreaObj.GetProperty(aName, ref propName);

                    int designOrientation = 0; // 1: Floor, 2: Wall
                    try { _sapModel.AreaObj.GetDesignOrientation(aName, ref designOrientation); } catch { }

                    // Kiểm tra mặt phẳng thẳng đứng (Vách) hay nằm ngang (Sàn)
                    bool isWall = (designOrientation == 2);
                    if (designOrientation == 0)
                    {
                        double minZ = double.MaxValue, maxZ = double.MinValue;
                        foreach (var pt in pts)
                        {
                            if (pt.Z < minZ) minZ = pt.Z;
                            if (pt.Z > maxZ) maxZ = pt.Z;
                        }
                        isWall = (maxZ - minZ) > 500.0;
                    }

                    string story = FindClosestStory(model, sumZ / numPoints);

                    if (isWall)
                    {
                        var wall = new ETABSWall
                        {
                            Source = "ETABS",
                            SourceId = aName,
                            Name = aName,
                            Story = story,
                            Section = propName,
                            ThicknessMm = 200.0
                        };
                        wall.BoundaryPoints.AddRange(pts);
                        model.Walls.Add(wall);
                    }
                    else
                    {
                        var slab = new ETABSSlab
                        {
                            Source = "ETABS",
                            SourceId = aName,
                            Name = aName,
                            Story = story,
                            Section = propName,
                            ThicknessMm = 150.0
                        };
                        slab.OuterBoundary.AddRange(pts);
                        model.Slabs.Add(slab);
                    }
                }
            }
            catch { }
        }

        private static string FindClosestStory(StructuralModel model, double zMm)
        {
            if (model.Levels.Count == 0) return string.Empty;

            string closest = model.Levels[0].Name;
            double minDiff = Math.Abs(model.Levels[0].ElevationMm - zMm);

            for (int i = 1; i < model.Levels.Count; i++)
            {
                double diff = Math.Abs(model.Levels[i].ElevationMm - zMm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = model.Levels[i].Name;
                }
            }
            return closest;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
