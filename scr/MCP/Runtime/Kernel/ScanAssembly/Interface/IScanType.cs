using System;

namespace MCP.Kernel.ScanAssembly
{
    public interface IScanType
    {
        /// <summary>
        /// 是否应该处理此类型
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>true:表示此扫描器将处理该类型</returns>
        bool AllowScan(Type type);
        void ScanType(Type type);
    }
}
