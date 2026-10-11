using System;
using System.Collections.Generic;
using JNN.Stair.Models;

namespace JNN.Stair.Core
{
    /// <summary>
    /// Khối đa diện 3D hoặc bề mặt bậc thang
    /// </summary>
    public class StairMeshComponent
    {
        public string Name { get; set; } = string.Empty;
        public List<StairPoint3D> Vertices { get; set; } = new();
        public List<int[]> Faces { get; set; } = new(); // Chỉ số các đỉnh tạo thành mặt tam giác/tứ giác
    }

    /// <summary>
    /// Engine tính toán toàn bộ hình học không gian 3D của cầu thang BTCT
    /// </summary>
    public static class StairGeometryEngine
    {
        /// <summary>
        /// Tạo mô hình hình học 3D đầy đủ cho thang 2 vế chữ U (UShape)
        /// </summary>
        public static List<StairMeshComponent> BuildUShapeStair(StairLayout layout)
        {
            var components = new List<StairMeshComponent>();

            double W = layout.StairWidthMm;
            double H = layout.FloorToFloorMm;
            double T = layout.TreadMm;
            double waist = layout.WaistThicknessMm;
            double landingD = layout.LandingDepthMm;
            double gap = layout.FlightGapMm;

            var risersPerFlight = layout.GetRisersPerFlight();
            int n1 = risersPerFlight[0];
            int n2 = risersPerFlight.Count > 1 ? risersPerFlight[1] : n1;

            double R = layout.ActualRiserMm;
            double landingZ = n1 * R;

            // 1. Dựng vế 1 (Flight 1)
            components.Add(BuildStraightFlightMesh(
                flightName: "Vế 1",
                startX: 0,
                startY: 0,
                startZ: 0,
                width: W,
                tread: T,
                riser: R,
                riserCount: n1,
                waist: waist,
                directionY: 1.0));

            // 2. Dựng chiếu nghỉ (Landing)
            double landingStartX = 0;
            double landingStartY = n1 * T;
            double landingTotalWidth = 2 * W + gap;

            components.Add(BuildLandingMesh(
                landingName: "Chiếu nghỉ",
                minX: landingStartX,
                minY: landingStartY,
                minZ: landingZ - waist,
                widthX: landingTotalWidth,
                depthY: landingD,
                thicknessZ: waist));

            // 3. Dựng vế 2 (Flight 2) - Ngược chiều với vế 1
            double f2StartX = W + gap;
            double f2StartY = landingStartY;
            components.Add(BuildStraightFlightMesh(
                flightName: "Vế 2",
                startX: f2StartX,
                startY: f2StartY,
                startZ: landingZ,
                width: W,
                tread: T,
                riser: R,
                riserCount: n2,
                waist: waist,
                directionY: -1.0));

            // 4. Dựng dầm chiếu nghỉ (nếu có)
            if (layout.LandingBeamWidthMm > 0 && layout.LandingBeamHeightMm > 0)
            {
                components.Add(BuildLandingMesh(
                    landingName: "Dầm chiếu nghỉ",
                    minX: landingStartX,
                    minY: landingStartY + landingD - layout.LandingBeamWidthMm,
                    minZ: landingZ - layout.LandingBeamHeightMm,
                    widthX: landingTotalWidth,
                    depthY: layout.LandingBeamWidthMm,
                    thicknessZ: layout.LandingBeamHeightMm));
            }

            return components;
        }

        private static StairMeshComponent BuildStraightFlightMesh(
            string flightName,
            double startX,
            double startY,
            double startZ,
            double width,
            double tread,
            double riser,
            int riserCount,
            double waist,
            double directionY)
        {
            var mesh = new StairMeshComponent { Name = flightName };

            // Tạo các bậc zic-zac mặt trên và đường nghiêng đáy
            // Mỗi bậc có 2 điểm trên (mép trong & mép ngoài của mặt bậc)
            for (int i = 0; i < riserCount; i++)
            {
                double zBottom = startZ + i * riser;
                double zTop = zBottom + riser;
                double yStart = startY + i * tread * directionY;
                double yEnd = yStart + tread * directionY;

                // Điểm bậc
                mesh.Vertices.Add(new StairPoint3D(startX, yStart, zTop));
                mesh.Vertices.Add(new StairPoint3D(startX + width, yStart, zTop));
                mesh.Vertices.Add(new StairPoint3D(startX, yEnd, zTop));
                mesh.Vertices.Add(new StairPoint3D(startX + width, yEnd, zTop));
            }

            return mesh;
        }

        private static StairMeshComponent BuildLandingMesh(
            string landingName,
            double minX,
            double minY,
            double minZ,
            double widthX,
            double depthY,
            double thicknessZ)
        {
            var mesh = new StairMeshComponent { Name = landingName };

            // 8 đỉnh khối hộp chiếu nghỉ
            mesh.Vertices.Add(new StairPoint3D(minX, minY, minZ));
            mesh.Vertices.Add(new StairPoint3D(minX + widthX, minY, minZ));
            mesh.Vertices.Add(new StairPoint3D(minX + widthX, minY + depthY, minZ));
            mesh.Vertices.Add(new StairPoint3D(minX, minY + depthY, minZ));

            mesh.Vertices.Add(new StairPoint3D(minX, minY, minZ + thicknessZ));
            mesh.Vertices.Add(new StairPoint3D(minX + widthX, minY, minZ + thicknessZ));
            mesh.Vertices.Add(new StairPoint3D(minX + widthX, minY + depthY, minZ + thicknessZ));
            mesh.Vertices.Add(new StairPoint3D(minX, minY + depthY, minZ + thicknessZ));

            return mesh;
        }
    }
}
