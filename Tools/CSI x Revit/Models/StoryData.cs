namespace JNNTool.Tools.CSIxRevit.Models
{
    public class StoryData
    {
        public string Name { get; set; }
        public double Elevation { get; set; }
        public double Height { get; set; }
        public string TargetName { get; set; }
        public bool IsSelected { get; set; } = true;
    }
}
