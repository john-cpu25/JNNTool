using System;
using JNNTool.Tools.IFCEtabs.Core.Models;

namespace JNNTool.Tools.IFCEtabs.IFC.Interfaces
{
    public interface IIfcService
    {
        /// <summary>
        /// Đọc tệp tin IFC và chuyển đổi thành JNN StructuralModel.
        /// </summary>
        StructuralModel ReadIfcFile(string filePath, Action<string, double>? progressCallback = null);
    }
}
