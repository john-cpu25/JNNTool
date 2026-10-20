using System;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Core.UI;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Bộ thực thi lệnh cho Command Palette. Vì Dockable Pane chạy trong môi trường Modeless,
    /// mọi lệnh đều được đưa qua ExternalEvent (ActionEventHandler):
    ///   1. Ưu tiên PostCommand theo đúng Ribbon Command Id.
    ///   2. Nếu nút không có trên Ribbon (hoặc bị ẩn), Direct Invoke IExternalCommand.Execute qua Reflection.
    /// </summary>
    internal static class CommandRunner
    {
        public static event Action<string, bool>? StatusChanged;

        public static void Run(ToolCommandInfo cmd)
        {
            if (cmd == null) return;
            Report($"Đang gọi {cmd.Title}…", false);
            ActionEventHandler.Instance.Post(app => Execute(app, cmd));
        }

        private static void Execute(UIApplication app, ToolCommandInfo cmd)
        {
            if (app.ActiveUIDocument == null)
            {
                Report("Hãy mở một dự án Revit trước khi chạy lệnh.", true);
                return;
            }

            try
            {
                RevitCommandId? id = RevitCommandId.LookupCommandId(CommandRegistry.GetPostableId(cmd));
                if (id != null && app.CanPostCommand(id))
                {
                    app.PostCommand(id);
                    Report($"Đang thực thi: {cmd.Title}", false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"CommandRunner.PostCommand failed for {cmd.Id}", ex);
            }

            InvokeDirect(app, cmd);
        }

        private static void InvokeDirect(UIApplication app, ToolCommandInfo cmd)
        {
            try
            {
                Type? type = typeof(CommandRunner).Assembly.GetType(cmd.ClassName, false);
                if (type == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType(cmd.ClassName, false);
                        if (type != null) break;
                    }
                }

                if (type == null)
                {
                    Report($"Không tìm thấy lớp lệnh: {cmd.ClassName}", true);
                    return;
                }

                var command = (IExternalCommand?)Activator.CreateInstance(type);
                if (command == null)
                {
                    Report($"Không thể khởi tạo lệnh: {cmd.Title}", true);
                    return;
                }

                ExternalCommandData data;
                try
                {
                    data = CreateCommandData(app);
                    if (data?.Application == null)
                    {
                        Report($"Vui lòng kích hoạt lệnh '{cmd.Title}' từ thanh Ribbon.", true);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Không thể khởi tạo ExternalCommandData cho {cmd.Title}: {ex.Message}");
                    Report($"Vui lòng bấm nút '{cmd.Title}' trực tiếp trên thanh Ribbon.", true);
                    return;
                }

                string message = string.Empty;
                var elements = new ElementSet();

                Result result = command.Execute(data, ref message, elements);

                if (result == Result.Failed)
                {
                    Report($"{cmd.Title}: {(string.IsNullOrEmpty(message) ? "Thất bại" : message)}", true);
                }
                else
                {
                    Report($"Đã hoàn thành: {cmd.Title}", false);
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                Report($"Đã hủy: {cmd.Title}", false);
            }
            catch (Exception ex)
            {
                Logger.Error($"CommandRunner.InvokeDirect failed for {cmd.Id}", ex);
                Report($"Lỗi khi chạy {cmd.Title}: {ex.Message}", true);
            }
        }

        private static ExternalCommandData CreateCommandData(UIApplication app)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ExternalCommandData data;
            try
            {
                data = (ExternalCommandData)Activator.CreateInstance(typeof(ExternalCommandData), true)!;
            }
            catch
            {
#if NETFRAMEWORK
                data = (ExternalCommandData)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ExternalCommandData));
#else
                data = (ExternalCommandData)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ExternalCommandData));
#endif
            }

            SetMemberValue(data, "Application", typeof(UIApplication), app, flags);
            if (app.ActiveUIDocument?.ActiveView != null)
            {
                SetMemberValue(data, "View", typeof(View), app.ActiveUIDocument.ActiveView, flags);
            }
            SetMemberValue(data, "JournalData", typeof(System.Collections.Generic.IDictionary<string, string>),
                           new System.Collections.Generic.Dictionary<string, string>(), flags);
            return data;
        }

        private static void SetMemberValue(object target, string propertyName, Type memberType, object value, BindingFlags flags)
        {
            try
            {
                PropertyInfo? prop = target.GetType().GetProperty(propertyName, flags);
                MethodInfo? setter = prop?.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new[] { value });
                    return;
                }

                foreach (FieldInfo f in target.GetType().GetFields(flags))
                {
                    if (memberType.IsAssignableFrom(f.FieldType))
                    {
                        f.SetValue(target, value);
                        return;
                    }
                }
            }
            catch { }
        }

        private static void Report(string message, bool isError)
        {
            UiDispatcher.Invoke(() => StatusChanged?.Invoke(message, isError));
        }
    }
}
