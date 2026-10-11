#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.StairModel.Engine;

namespace JNNTool.Tools.StairModel.Services
{
    /// <summary>
    /// Điều phối quá trình tạo toàn bộ cấu kiện thang trong Revit bên trong TransactionGroup.
    /// </summary>
    public static class StairModelBuilder
    {
        public static (bool Success, string Message, List<ElementId> CreatedIds) BuildStair(Document doc, StairConfig config)
        {
            var createdIds = new List<ElementId>();

            if (doc == null)
                return (false, "Document null", createdIds);

            // 1. Tính toán hình học thuần C#
            var geom = StairGeometryEngine.Generate(config);
            if (!geom.Success)
            {
                return (false, geom.ErrorMessage ?? "Lỗi tính toán hình học cầu thang.", createdIds);
            }

            // 2. Lấy BaseLevel và FloorType
            if (!long.TryParse(config.BaseLevelId, out long baseLvlLong) || baseLvlLong == -1)
                return (false, "Chưa chọn Base Level.", createdIds);

            ElementId baseLevelId = ElementIdCompat.Create(baseLvlLong);
            Level? baseLevel = doc.GetElement(baseLevelId) as Level;
            if (baseLevel == null)
                return (false, "Không tìm thấy Base Level trong mô hình.", createdIds);

            if (!long.TryParse(config.SelectedFloorTypeId, out long floorTypeLong) || floorTypeLong == -1)
                return (false, "Chưa chọn Floor Type (loại sàn thang).", createdIds);

            ElementId floorTypeId = ElementIdCompat.Create(floorTypeLong);

            ElementId beamSymbolId = ElementId.InvalidElementId;
            if (config.SupportBeamOption != SupportBeamOption.None &&
                long.TryParse(config.SelectedBeamSymbolId, out long beamSymLong) && beamSymLong != -1)
            {
                beamSymbolId = ElementIdCompat.Create(beamSymLong);
            }

            var createdFloors = new List<Floor>();

            // 3. Thực thi trong TransactionGroup để có thể Undo 1 lần duy nhất
            using (var tg = new TransactionGroup(doc, "JNN Auto Structural Stair"))
            {
                tg.Start();

                try
                {
                    using (var t = new Transaction(doc, "Tạo cấu kiện cầu thang kết cấu"))
                    {
                        var failureOptions = t.GetFailureHandlingOptions();
                        failureOptions.SetFailuresPreprocessor(new StairWarningSwallower());
                        t.SetFailureHandlingOptions(failureOptions);

                        t.Start();

                        // 3.1 Tạo các vế thang (Flights)
                        foreach (var flight in geom.Flights)
                        {
                            var floor = FlightSlabBuilder.Build(doc, flight, floorTypeId, baseLevelId);
                            if (floor != null)
                            {
                                createdFloors.Add(floor);
                                createdIds.Add(floor.Id);
                            }
                        }

                        // 3.2 Tạo các chiếu nghỉ (Landings)
                        foreach (var landing in geom.Landings)
                        {
                            var floor = LandingBuilder.Build(doc, landing, floorTypeId, baseLevelId);
                            if (floor != null)
                            {
                                createdFloors.Add(floor);
                                createdIds.Add(floor.Id);
                            }
                        }

                        // 3.3 Tạo dầm đỡ (Support Beams)
                        if (beamSymbolId != ElementId.InvalidElementId && geom.Beams.Count > 0)
                        {
                            foreach (var beam in geom.Beams)
                            {
                                var beamInst = StairBeamBuilder.Build(doc, beam, beamSymbolId, baseLevel);
                                if (beamInst != null)
                                {
                                    createdIds.Add(beamInst.Id);
                                }
                            }
                        }

                        doc.Regenerate();

                        // 3.4 Tự động Join các bản sàn giao nhau (Auto Join Slabs)
                        for (int i = 0; i < createdFloors.Count; i++)
                        {
                            for (int j = i + 1; j < createdFloors.Count; j++)
                            {
                                try
                                {
                                    if (!JoinGeometryUtils.AreElementsJoined(doc, createdFloors[i], createdFloors[j]))
                                    {
                                        JoinGeometryUtils.JoinGeometry(doc, createdFloors[i], createdFloors[j]);
                                    }
                                }
                                catch
                                {
                                    // Bỏ qua nếu 2 sàn không chạm nhau
                                }
                            }
                        }

                        t.Commit();
                    }

                    tg.Assimilate();
                    return (true, $"Tạo thành công {createdIds.Count} cấu kiện cầu thang kết cấu bê tông.", createdIds);
                }
                catch (Exception ex)
                {
                    tg.RollBack();
                    return (false, $"Lỗi khi dựng mô hình thang trong Revit: {ex.Message}", createdIds);
                }
            }
        }
    }

    /// <summary>
    /// Bộ lọc cảnh báo (Warning Swallower) triệt tiêu các thông báo cảnh báo không ảnh hưởng mô hình.
    /// </summary>
    public class StairWarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failureMessages = failuresAccessor.GetFailureMessages();
            foreach (var message in failureMessages)
            {
                if (message.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(message);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
