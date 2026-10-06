using System;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class ColumnRotationHelper
    {
        /// <summary>
        /// Calculates the required Revit rotation angle (in degrees) to match the ETABS column orientation.
        /// </summary>
        /// <param name="etabsD_mm">ETABS section dimension D (Depth) in mm.</param>
        /// <param name="etabsW_mm">ETABS section dimension B/W (Width) in mm.</param>
        /// <param name="etabsAngleDeg">ETABS column angle (ANG) in degrees.</param>
        /// <param name="revitB_mm">Revit symbol parameter 'b' in mm.</param>
        /// <param name="revitH_mm">Revit symbol parameter 'h' in mm.</param>
        /// <returns>Normalized rotation angle in degrees [0, 180).</returns>
        public static double CalculateRotationAngle(double etabsD_mm, double etabsW_mm, double etabsAngleDeg, double revitB_mm, double revitH_mm)
        {
            double targetAngleDeg = etabsAngleDeg;

            // Non-square rectangular column
            if (etabsD_mm > 0 && etabsW_mm > 0 && Math.Abs(etabsD_mm - etabsW_mm) > 5.0)
            {
                // In ETABS coordinate conventions for rectangular frame sections:
                // Dimension D (Depth) is oriented along the element's local axis corresponding to Global X when Angle = 0.
                // Dimension B/W (Width) is oriented perpendicular to D (along Global Y when Angle = 0).
                double angleEtabsD = etabsAngleDeg;

                // In Revit Family placed at rotation 0:
                // Parameter 'b' is oriented along Global X (0 deg).
                // Parameter 'h' is oriented along Global Y (90 deg).
                if (revitB_mm > 0 && revitH_mm > 0)
                {
                    double diffB = Math.Abs(revitB_mm - etabsD_mm);
                    double diffH = Math.Abs(revitH_mm - etabsD_mm);

                    if (diffH < diffB)
                    {
                        // ETABS D matches Revit parameter 'h' (which is at 90 deg at rotation 0).
                        // To rotate Revit 'h' to align with angleEtabsD:
                        targetAngleDeg = angleEtabsD - 90.0;
                    }
                    else
                    {
                        // ETABS D matches Revit parameter 'b' (which is at 0 deg at rotation 0).
                        targetAngleDeg = angleEtabsD - 0.0;
                    }
                }
                else
                {
                    // Fallback if Revit dimensions cannot be read:
                    // Standard Revit rectangular column has h > b (h along 90 deg).
                    targetAngleDeg = (etabsD_mm > etabsW_mm) ? (angleEtabsD - 90.0) : angleEtabsD;
                }
            }

            // Rectangular columns have 180° rotational symmetry
            double normalizedAngle = ((targetAngleDeg % 180.0) + 180.0) % 180.0;
            return normalizedAngle;
        }
    }
}
