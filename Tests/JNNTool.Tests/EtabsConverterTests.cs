using System;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Services;
using JNNTool.Tools.IFCEtabs.Core.Units;
using Xunit;

namespace JNNTool.Tests
{
    public class EtabsConverterTests
    {
        [Fact]
        public void Point3D_DistanceAndMath_CalculatesAccurately()
        {
            var p1 = new Point3D(0, 0, 0);
            var p2 = new Point3D(3000, 4000, 0);

            double dist = p1.DistanceTo(p2);
            Assert.Equal(5000.0, dist, precision: 2);

            var mid = new Point3D((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
            Assert.True(mid.IsAlmostEqualTo(new Point3D(1500, 2000, 0)));
        }

        [Fact]
        public void Vector3D_DotAndCross_CalculatesCorrectly()
        {
            var vx = Vector3D.UnitX;
            var vy = Vector3D.UnitY;
            var vz = vx.Cross(vy);

            Assert.True(vz.Equals(Vector3D.UnitZ));
            Assert.Equal(0.0, vx.Dot(vy), precision: 5);
            Assert.Equal(Math.PI / 2, vx.AngleTo(vy), precision: 4);
        }

        [Fact]
        public void CoordinateTransform_OffsetAndRotation_TransformsAccurately()
        {
            // Xoay 90 độ quanh trục Z và dời gốc (1000, 2000, 0)
            var transform = new CoordinateTransform(new Point3D(1000, 2000, 0), rotationDegrees: 90.0);
            var pt = new Point3D(500, 0, 0);

            var transformed = transform.TransformPoint(pt);
            // Xoay 90 độ: (500, 0) -> (0, 500) -> sau dời gốc -> (1000, 2500)
            Assert.True(Math.Abs(transformed.X - 1000.0) < 1e-3);
            Assert.True(Math.Abs(transformed.Y - 2500.0) < 1e-3);

            var inverse = transform.InverseTransformPoint(transformed);
            Assert.True(inverse.IsAlmostEqualTo(pt, 1e-3));
        }

        [Fact]
        public void UnitConverter_MmFeetMeters_ConvertsCorrectly()
        {
            double mm = 304.8;
            double feet = UnitConverter.MmToFeet(mm);
            Assert.Equal(1.0, feet, precision: 4);

            double backMm = UnitConverter.FeetToMm(feet);
            Assert.Equal(mm, backMm, precision: 4);

            double meters = 6.0;
            double mmFromM = UnitConverter.MetersToMm(meters);
            Assert.Equal(6000.0, mmFromM, precision: 4);
        }

        [Fact]
        public void ElementHashService_StabilityAndSensitivity_ProducesAccurateHash()
        {
            var beam1 = new ETABSBeam
            {
                Source = "ETABS",
                SourceId = "B101",
                Section = "B400x600",
                Material = "C30",
                Story = "L02",
                StartPoint = new Point3D(0, 0, 3600),
                EndPoint = new Point3D(6000, 0, 3600),
                WidthMm = 400,
                DepthMm = 600
            };

            var beam2 = new ETABSBeam
            {
                Source = "ETABS",
                SourceId = "B101",
                Section = "B400x600",
                Material = "C30",
                Story = "L02",
                StartPoint = new Point3D(0, 0, 3600),
                EndPoint = new Point3D(6000, 0, 3600),
                WidthMm = 400,
                DepthMm = 600
            };

            string hash1 = ElementHashService.Instance.ComputeHash(beam1);
            string hash2 = ElementHashService.Instance.ComputeHash(beam2);

            Assert.False(string.IsNullOrEmpty(hash1));
            Assert.Equal(hash1, hash2); // Tính ổn định: thuộc tính giống nhau cho cùng mã hash

            // Thay đổi tiết diện -> hash phải đổi
            beam2.Section = "B500x700";
            beam2.WidthMm = 500;
            beam2.DepthMm = 700;
            string hashChanged = ElementHashService.Instance.ComputeHash(beam2);

            Assert.NotEqual(hash1, hashChanged);
        }

        [Fact]
        public void StructuralModel_ElementCounts_TracksSummary()
        {
            var model = new StructuralModel { ModelName = "Tower_A" };
            model.Levels.Add(new ETABSLevel { Name = "L01", ElevationMm = 0 });
            model.Levels.Add(new ETABSLevel { Name = "L02", ElevationMm = 3600 });

            model.Columns.Add(new ETABSColumn
            {
                SourceId = "C1",
                BasePoint = new Point3D(0, 0, 0),
                TopPoint = new Point3D(0, 0, 3600),
                Section = "C600x600"
            });

            model.Beams.Add(new ETABSBeam
            {
                SourceId = "B1",
                StartPoint = new Point3D(0, 0, 3600),
                EndPoint = new Point3D(5000, 0, 3600),
                Section = "B300x500"
            });

            Assert.Equal(2, model.Levels.Count);
            Assert.Single(model.Columns);
            Assert.Single(model.Beams);
            Assert.Equal(4, model.TotalElements);
        }

        [Fact]
        public void IfcReader_ParseMockSpf_ExtractsElementsAndProperties()
        {
            string mockIfc =
@"ISO-10303-21;
HEADER;
ENDSEC;
DATA;
#1= IFCPROJECT('0h$1yZ...',#2,'Project Demo',$,$,$,$,$,#3);
#10= IFCBUILDINGSTOREY('2Qv...',#2,'Level 1',$,$,#11,$,$,.ELEMENT.,0.);
#20= IFCBEAM('3x9F...',#2,'B1',$,'Concrete Beam',#21,#22,'B400x600');
#30= IFCCOLUMN('1a2b...',#2,'C1',$,'Concrete Column',#31,#32,'C500x500');
#60= IFCPROPERTYSET('p1',#2,'Pset_BeamCommon',$,(#61,#62));
#61= IFCPROPERTYSINGLEVALUE('Reference',$,'B1',$);
#62= IFCPROPERTYSINGLEVALUE('LoadBearing',$,'TRUE',$);
#70= IFCRELDEFINESBYPROPERTIES('r1',#2,$,$,(#20),#60);
#80= IFCRELCONTAINEDINSPATIALSTRUCTURE('r2',#2,$,$,(#20,#30),#10);
ENDSEC;
END-ISO-10303-21;";

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Mock_{Guid.NewGuid():N}.ifc");
            try
            {
                System.IO.File.WriteAllText(tempFile, mockIfc);
                var model = JNNTool.Tools.IFCEtabs.IFC.IfcReader.Instance.ReadIfcFile(tempFile);

                Assert.Single(model.Levels);
                Assert.Equal("Level 1", model.Levels[0].Name);

                Assert.Single(model.Beams);
                var beam = model.Beams[0];
                Assert.Equal("B1", beam.Name);
                Assert.Equal("Level 1", beam.Story);
                Assert.Equal("B400x600", beam.Section);

                // Kiểm tra thuộc tính IFC Pset được bảo toàn
                Assert.True(beam.IFCProperties.ContainsKey("Pset_BeamCommon.LoadBearing"));
                Assert.Equal("TRUE", beam.IFCProperties["Pset_BeamCommon.LoadBearing"]);

                Assert.Single(model.Columns);
                var col = model.Columns[0];
                Assert.Equal("C1", col.Name);
                Assert.Equal("Level 1", col.Story);
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void ETABSWallAndSlab_GeometryProperties_StoredProperly()
        {
            var wall = new ETABSWall
            {
                SourceId = "W1",
                ThicknessMm = 250,
                HeightMm = 3300,
                BasePointStart = new Point3D(0, 0, 0),
                BasePointEnd = new Point3D(5000, 0, 0)
            };
            wall.BoundaryPoints.Add(wall.BasePointStart);
            wall.BoundaryPoints.Add(wall.BasePointEnd);

            Assert.Equal(250, wall.ThicknessMm);
            Assert.Equal(3300, wall.HeightMm);
            Assert.Equal(2, wall.BoundaryPoints.Count);

            var slab = new ETABSSlab
            {
                SourceId = "S1",
                ThicknessMm = 150
            };
            slab.OuterBoundary.Add(new Point3D(0, 0, 0));
            slab.OuterBoundary.Add(new Point3D(6000, 0, 0));
            slab.OuterBoundary.Add(new Point3D(6000, 6000, 0));
            slab.OuterBoundary.Add(new Point3D(0, 6000, 0));

            var opening = new System.Collections.Generic.List<Point3D>
            {
                new Point3D(1000, 1000, 0),
                new Point3D(2000, 1000, 0),
                new Point3D(2000, 2000, 0),
                new Point3D(1000, 2000, 0)
            };
            slab.Openings.Add(opening);

            Assert.Equal(4, slab.OuterBoundary.Count);
            Assert.Single(slab.Openings);
            Assert.Equal(4, slab.Openings[0].Count);
        }

        [Fact]
        public void ValidationSummary_CalculatesPassRate_AndTotalsCorrectly()
        {
            var summary = new JNNTool.Tools.IFCEtabs.Services.ValidationSummary
            {
                TotalChecks = 4,
                PassedCount = 3,
                WarningCount = 1,
                FailedCount = 0
            };

            summary.Items.Add(new JNNTool.Tools.IFCEtabs.Services.ValidationItem
            {
                Category = "Dầm",
                ItemName = "Chiều dài B101",
                Status = JNNTool.Tools.IFCEtabs.Services.ValidationStatus.Pass
            });

            Assert.Equal(75.0, summary.PassRate, precision: 1);
            Assert.Contains("75.0%", summary.ToString());
            Assert.Single(summary.Items);
        }

        [Fact]
        public void ReportGenerator_GenerateHtmlAndCsvReport_ProducesValidFiles()
        {
            var model = new StructuralModel { ModelName = "QA_Tower", SourceUnits = "mm" };
            model.Levels.Add(new ETABSLevel { Name = "Story 1", ElevationMm = 0 });
            model.Columns.Add(new ETABSColumn { SourceId = "C1", Section = "C500x500", Story = "Story 1" });
            model.Beams.Add(new ETABSBeam
            {
                SourceId = "B1",
                Section = "B300x600",
                Story = "Story 1",
                StartPoint = new Point3D(0, 0, 3600),
                EndPoint = new Point3D(6000, 0, 3600)
            });

            var idMap = new System.Collections.Generic.Dictionary<string, string>
            {
                { "C1", "12345" },
                { "B1", "67890" }
            };

            var validation = new JNNTool.Tools.IFCEtabs.Services.ValidationSummary
            {
                TotalChecks = 2,
                PassedCount = 2,
                WarningCount = 0,
                FailedCount = 0
            };
            validation.Items.Add(new JNNTool.Tools.IFCEtabs.Services.ValidationItem
            {
                Category = "Dầm",
                ElementId = "B1",
                ItemName = "Chiều dài dầm B1",
                ExpectedValue = "6000.0 mm",
                ActualValue = "6000.0 mm",
                Difference = 0.0,
                Status = JNNTool.Tools.IFCEtabs.Services.ValidationStatus.Pass,
                Message = "Khớp hình học"
            });

            var result = new ConversionReportResult
            {
                ColumnsCreated = 1,
                BeamsCreated = 1,
                LevelsCreated = 1
            };

            string htmlPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"QA_Report_{Guid.NewGuid():N}.html");
            string csvPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"QA_Report_{Guid.NewGuid():N}.csv");

            try
            {
                // 1. HTML Report
                string generatedHtml = JNNTool.Tools.IFCEtabs.Services.ReportGenerator.GenerateHtmlReport(
                    "QA_Project", model, idMap, validation, result, htmlPath);

                Assert.True(System.IO.File.Exists(generatedHtml));
                string htmlContent = System.IO.File.ReadAllText(generatedHtml);
                Assert.Contains("BÁO CÁO NGHIỆM THU CHUYỂN ĐỔI", htmlContent);
                Assert.Contains("QA_Tower", htmlContent);
                Assert.Contains("100.0%", htmlContent);
                Assert.Contains("Cột kết cấu (Columns)", htmlContent);
                Assert.Contains("Khớp hình học", htmlContent);

                // 2. CSV Report with UTF-8 BOM
                string generatedCsv = JNNTool.Tools.IFCEtabs.Services.ReportGenerator.GenerateCsvReport(
                    model, idMap, result, csvPath);

                Assert.True(System.IO.File.Exists(generatedCsv));
                byte[] bytes = System.IO.File.ReadAllBytes(generatedCsv);
                // Kiểm tra 3 byte đầu tiên là UTF-8 BOM: EF BB BF
                Assert.True(bytes.Length >= 3);
                Assert.Equal(0xEF, bytes[0]);
                Assert.Equal(0xBB, bytes[1]);
                Assert.Equal(0xBF, bytes[2]);

                string csvContent = System.IO.File.ReadAllText(generatedCsv);
                Assert.Contains("Nguon,MaID_Nguon,LoaiCauKien", csvContent);
                Assert.Contains("\"C1\"", csvContent);
                Assert.Contains("\"12345\"", csvContent);
                Assert.Contains("\"C500x500\"", csvContent);
                Assert.Contains("\"B1\"", csvContent);
                Assert.Contains("\"67890\"", csvContent);
            }
            finally
            {
                if (System.IO.File.Exists(htmlPath)) System.IO.File.Delete(htmlPath);
                if (System.IO.File.Exists(csvPath)) System.IO.File.Delete(csvPath);
            }
        }
    }
}
