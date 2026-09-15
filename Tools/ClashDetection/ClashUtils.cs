using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.ClashDetection
{
    public static class ClashUtils
    {
        public static List<Solid> GetSolids(Element elem)
        {
            var solids = new List<Solid>();
            Options opt = new Options { DetailLevel = ViewDetailLevel.Fine };
            GeometryElement geomElem = elem.get_Geometry(opt);
            if (geomElem == null) return solids;
            
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    solids.Add(solid);
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    foreach (GeometryObject instObj in instGeom)
                    {
                        if (instObj is Solid instSolid && instSolid.Volume > 0)
                        {
                            solids.Add(instSolid);
                        }
                    }
                }
            }
            return solids;
        }

        public static BoundingBoxXYZ ExpandBoundingBox(BoundingBoxXYZ bbox, double offset)
        {
            if (bbox == null) return null;
            return new BoundingBoxXYZ
            {
                Min = bbox.Min - new XYZ(offset, offset, offset),
                Max = bbox.Max + new XYZ(offset, offset, offset)
            };
        }
    }
}

