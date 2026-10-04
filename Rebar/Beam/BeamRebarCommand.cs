using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Beam.Views;

namespace JNNTool.RebarSuite.Beam
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BeamRebarCommand : IExternalCommand
    {
        private static BeamRebarWindow? _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new BeamRebarWindow();
                    _window.Closed += (s, e) => _window = null;
                    _window.Show();
                }
                else
                {
                    _window.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi mở cửa sổ Thép Dầm", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
