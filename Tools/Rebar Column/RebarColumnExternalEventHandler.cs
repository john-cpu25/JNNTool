using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.RebarColumn.ViewModels;

namespace JNNTool.Tools.RebarColumn
{
    public class RebarColumnExternalEventHandler : IExternalEventHandler
    {
        public RebarColumnViewModel ViewModel { get; set; }
        public Document Doc { get; set; }
        public FamilyInstance SelectedColumn { get; set; }

        public void Execute(UIApplication app)
        {
            if (ViewModel == null || Doc == null || SelectedColumn == null) return;

            try
            {
                var config = new JNNTool.Tools.RebarColumn.Models.RebarConfig
                {
                    Cover = ViewModel.Cover,
                    CountX = ViewModel.CountX,
                    CountY = ViewModel.CountY,
                    MainDiameter = ViewModel.MainDiameter,
                    StirrupDiameter = ViewModel.StirrupDiameter,
                    LapFactor = ViewModel.LapFactor,
                    IsSeismic = ViewModel.IsSeismic,
                    Spacing1 = ViewModel.Spacing1,
                    Spacing2 = ViewModel.Spacing2,
                    Spacing3 = ViewModel.Spacing3,
                    SelectedPattern = ViewModel.SelectedPattern,
                    SelectedStirrupShapeName = ViewModel.SelectedStirrupShapeName
                };

                var logic = new RebarColumnLogic(Doc, SelectedColumn);
                logic.CreateRebar(config);
                
                TaskDialog.Show("Rebar Column", "Reinforcement generated successfully!");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
            }
        }

        public string GetName()
        {
            return "RebarColumnEventHandler";
        }
    }
}

