using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Revit.Builders;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;
using JNNTool.Tools.IFCEtabs.Revit.Tracking;

namespace JNNTool.Tools.IFCEtabs.Revit.Services
{
    /// <summary>
    /// Dịch vụ chuyển đổi chính: Thực thi quy trình sinh dựng mô hình Revit theo TransactionGroup có kiểm soát.
    /// Hỗ trợ Rollback, cô lập lỗi từng thanh để không làm đứt đoạn toàn bộ tiến trình.
    /// </summary>
    public class RevitConversionService
    {
        private readonly Document _doc;

        public RevitConversionService(Document doc)
        {
            _doc = doc;
        }

        public ConversionReportResult ConvertModel(
            StructuralModel model,
            ConversionOptions options,
            Action<string, double>? progressCallback = null)
        {
            var result = new ConversionReportResult();

            using var tg = new TransactionGroup(_doc, "JNN - Chuyển đổi ETABS sang Revit");
            tg.Start();

            try
            {
                // 1. Khởi tạo và kiểm tra tham số chia sẻ JNN
                progressCallback?.Invoke("Khởi tạo tham số chia sẻ JNN...", 5.0);
                using (var tParams = new Transaction(_doc, "JNN - Khởi tạo tham số"))
                {
                    tParams.Start();
                    SharedParameterManager.EnsureSharedParameters(_doc);
                    tParams.Commit();
                }

                // 2. Tạo & Ánh xạ Tầng (Levels)
                var levelMap = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase);
                var levelBuilder = new LevelBuilder(_doc);

                if (options.ImportLevels && model.Levels.Count > 0)
                {
                    progressCallback?.Invoke("Tạo và ánh xạ các tầng...", 15.0);
                    using (var tLevels = new Transaction(_doc, "JNN - Tạo tầng"))
                    {
                        tLevels.Start();
                        foreach (var elvl in model.Levels)
                        {
                            try
                            {
                                var rlvl = levelBuilder.GetOrCreateLevel(elvl, options.ElevationToleranceMm);
                                if (rlvl != null)
                                {
                                    levelMap[elvl.Name] = rlvl;
                                    result.LevelsCreated++;
                                }
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Lỗi tầng {elvl.Name}: {ex.Message}");
                            }
                        }
                        tLevels.Commit();
                    }
                }
                levelBuilder.RefreshLevelCache();

                // 3. Tạo Lưới trục (Grids)
                if (options.ImportGrids && model.Grids.Count > 0)
                {
                    progressCallback?.Invoke("Tạo và ánh xạ lưới trục...", 30.0);
                    using (var tGrids = new Transaction(_doc, "JNN - Tạo lưới trục"))
                    {
                        tGrids.Start();
                        var gridBuilder = new GridBuilder(_doc);
                        foreach (var egrid in model.Grids)
                        {
                            try
                            {
                                var rgrid = gridBuilder.GetOrCreateGrid(egrid);
                                if (rgrid != null) result.GridsCreated++;
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Lỗi lưới {egrid.Name}: {ex.Message}");
                            }
                        }
                        tGrids.Commit();
                    }
                }

                // 4. Quét tracking và đối soát mô hình
                progressCallback?.Invoke("Quét đối soát các cấu kiện hiện có...", 40.0);
                var tracker = new ElementTracker(_doc);
                var updateEngine = new UpdateEngine();
                updateEngine.Differentiate(model, tracker);

                var typeCreator = new TypeAutoCreator(_doc);
                var colBuilder = new ColumnBuilder(_doc, typeCreator);
                var beamBuilder = new BeamBuilder(_doc, typeCreator);

                // 5. Tạo & Cập nhật Cột (Structural Columns)
                if (options.ImportColumns && model.Columns.Count > 0)
                {
                    progressCallback?.Invoke("Đang dựng cấu kiện Cột kết cấu...", 60.0);
                    using (var tCols = new Transaction(_doc, "JNN - Dựng cột"))
                    {
                        tCols.Start();
                        int colIdx = 0;
                        int totalCols = model.Columns.Count;

                        foreach (var col in model.Columns)
                        {
                            colIdx++;
                            try
                            {
                                // Tìm Base Level & Top Level
                                if (!levelMap.TryGetValue(col.BaseStory, out var baseLvl))
                                {
                                    baseLvl = levelBuilder.GetOrCreateLevel(new ETABSLevel { Name = col.BaseStory, ElevationMm = col.BasePoint.Z });
                                    levelMap[col.BaseStory] = baseLvl;
                                }

                                if (!levelMap.TryGetValue(col.TopStory, out var topLvl))
                                {
                                    topLvl = levelBuilder.GetOrCreateLevel(new ETABSLevel { Name = col.TopStory, ElevationMm = col.TopPoint.Z });
                                    levelMap[col.TopStory] = topLvl;
                                }

                                if (baseLvl.Id == topLvl.Id)
                                {
                                    var nextLvl = levelBuilder.GetNextLevelAbove(baseLvl);
                                    if (nextLvl != null)
                                    {
                                        topLvl = nextLvl;
                                    }
                                }

                                if (tracker.TrackedElements.TryGetValue(col.SourceId, out var tracked) &&
                                    tracked.Element is FamilyInstance existCol)
                                {
                                    if (options.UpdateExisting && col.Status == ConversionStatus.Updated)
                                    {
                                        colBuilder.UpdateColumn(existCol, col);
                                        result.ColumnsUpdated++;
                                    }
                                }
                                else
                                {
                                    var inst = colBuilder.CreateColumn(col, baseLvl, topLvl);
                                    if (inst != null) result.ColumnsCreated++;
                                }
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Cột {col.Name}: {ex.Message}");
                            }

                            if (colIdx % 50 == 0)
                            {
                                progressCallback?.Invoke($"Đang dựng cột {colIdx}/{totalCols}...", 60.0 + (colIdx / (double)totalCols) * 15.0);
                            }
                        }
                        tCols.Commit();
                    }
                }

                // 6. Tạo & Cập nhật Dầm (Structural Framing)
                if (options.ImportBeams && model.Beams.Count > 0)
                {
                    progressCallback?.Invoke("Đang dựng cấu kiện Dầm kết cấu...", 80.0);
                    using (var tBeams = new Transaction(_doc, "JNN - Dựng dầm"))
                    {
                        tBeams.Start();
                        int bmIdx = 0;
                        int totalBeams = model.Beams.Count;

                        foreach (var beam in model.Beams)
                        {
                            bmIdx++;
                            try
                            {
                                if (!levelMap.TryGetValue(beam.Story, out var refLevel))
                                {
                                    double avgZ = (beam.StartPoint.Z + beam.EndPoint.Z) * 0.5;
                                    refLevel = levelBuilder.GetOrCreateLevel(new ETABSLevel { Name = beam.Story, ElevationMm = avgZ });
                                    levelMap[beam.Story] = refLevel;
                                }

                                if (tracker.TrackedElements.TryGetValue(beam.SourceId, out var tracked) &&
                                    tracked.Element is FamilyInstance existBeam)
                                {
                                    if (options.UpdateExisting && beam.Status == ConversionStatus.Updated)
                                    {
                                        beamBuilder.UpdateBeam(existBeam, beam);
                                        result.BeamsUpdated++;
                                    }
                                }
                                else
                                {
                                    var inst = beamBuilder.CreateBeam(beam, refLevel);
                                    if (inst != null) result.BeamsCreated++;
                                }
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Dầm {beam.Name}: {ex.Message}");
                            }

                            if (bmIdx % 50 == 0)
                            {
                                progressCallback?.Invoke($"Đang dựng dầm {bmIdx}/{totalBeams}...", 80.0 + (bmIdx / (double)totalBeams) * 10.0);
                            }
                        }
                        tBeams.Commit();
                    }
                }

                var wallBuilder = new WallBuilder(_doc);
                var floorBuilder = new FloorBuilder(_doc);

                // 7. Tạo Vách (Walls)
                if (options.ImportWalls && model.Walls.Count > 0)
                {
                    progressCallback?.Invoke("Đang dựng cấu kiện Vách kết cấu...", 90.0);
                    using (var tWalls = new Transaction(_doc, "JNN - Dựng vách"))
                    {
                        tWalls.Start();
                        foreach (var wall in model.Walls)
                        {
                            try
                            {
                                if (!levelMap.TryGetValue(wall.Story, out var refLevel))
                                {
                                    refLevel = levelBuilder.GetOrCreateLevel(new ETABSLevel { Name = wall.Story, ElevationMm = wall.BasePointStart.Z });
                                    levelMap[wall.Story] = refLevel;
                                }

                                var inst = wallBuilder.CreateWall(wall, refLevel);
                                if (inst != null) result.WallsCreated++;
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Vách {wall.Name}: {ex.Message}");
                            }
                        }
                        tWalls.Commit();
                    }
                }

                // 8. Tạo Sàn (Floors / Slabs)
                if (options.ImportSlabs && model.Slabs.Count > 0)
                {
                    progressCallback?.Invoke("Đang dựng cấu kiện Sàn kết cấu...", 95.0);
                    using (var tSlabs = new Transaction(_doc, "JNN - Dựng sàn"))
                    {
                        tSlabs.Start();
                        foreach (var slab in model.Slabs)
                        {
                            try
                            {
                                if (!levelMap.TryGetValue(slab.Story, out var refLevel))
                                {
                                    double avgZ = slab.OuterBoundary.Count > 0 ? slab.OuterBoundary[0].Z : 0;
                                    refLevel = levelBuilder.GetOrCreateLevel(new ETABSLevel { Name = slab.Story, ElevationMm = avgZ });
                                    levelMap[slab.Story] = refLevel;
                                }

                                var inst = floorBuilder.CreateFloor(slab, refLevel);
                                if (inst != null) result.SlabsCreated++;
                            }
                            catch (Exception ex)
                            {
                                result.ErrorsCount++;
                                result.ErrorMessages.Add($"Sàn {slab.Name}: {ex.Message}");
                            }
                        }
                        tSlabs.Commit();
                    }
                }

                // 9. Hoàn tất toàn bộ giao dịch vào 1 lần Undo
                tg.Assimilate();
                progressCallback?.Invoke("Chuyển đổi hoàn tất thành công!", 100.0);
            }
            catch (Exception ex)
            {
                if (tg.HasStarted()) tg.RollBack();
                JNNTool.Core.Logging.Logger.Error("Lỗi trong quá trình chuyển đổi ETABS sang Revit", ex);
                throw new Exception($"Quá trình chuyển đổi thất bại: {ex.Message}", ex);
            }

            return result;
        }
    }
}
