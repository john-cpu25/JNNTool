using Autodesk.Revit.DB;

namespace JNNTool.Tools.AutoJoin
{
    /// <summary>
    /// Bộ xử lý cảnh báo tự động: Xóa bỏ mọi warning của Revit khi Join/Switch Order 
    /// để tránh đơ giao diện hoặc gián đoạn transaction.
    /// </summary>
    public class DismissWarningsPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failureMessages = failuresAccessor.GetFailureMessages();
            foreach (var failure in failureMessages)
            {
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
