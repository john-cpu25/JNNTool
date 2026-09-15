using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JNNTool.Tools.DisallowJoinBeam
{
    [Transaction(TransactionMode.Manual)]
    public class DisallowJoinBeamCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Cho phép người dùng quét chọn nhiều đối tượng (chỉ lọc dầm - StructuralFraming)
                IList<Reference> references = uidoc.Selection.PickObjects(ObjectType.Element, new BeamSelectionFilter(), "Quét chọn các dầm để Disallow Join");

                if (references.Count == 0)
                    return Result.Cancelled;

                using (Transaction tx = new Transaction(doc, "Disallow Join Beams"))
                {
                    tx.Start();

                    int count = 0;
                    foreach (Reference r in references)
                    {
                        Element elem = doc.GetElement(r);
                        if (elem is FamilyInstance beam && beam.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                        {
                            // Disallow join at both ends (0: Start, 1: End)
                            StructuralFramingUtils.DisallowJoinAtEnd(beam, 0);
                            StructuralFramingUtils.DisallowJoinAtEnd(beam, 1);
                            count++;
                        }
                    }

                    tx.Commit();
                    
                    TaskDialog.Show("Thành công", $"Đã thực hiện Disallow Join cho {count} dầm.");
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class BeamSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category != null && elem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
