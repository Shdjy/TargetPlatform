using System;
using System.IO;

namespace TargetPlatform.Models
{
    /// <summary>
    /// 封装单个 DLL 文件的架构检测结果。
    /// </summary>
    public sealed class DllArchitectureResult
    {
        private DllArchitectureResult(
            string filePath,
            DllArchitecture architecture,
            bool isManaged,
            string? errorMessage,
            Exception? exception)
        {
            FilePath = filePath;
            Architecture = architecture;
            IsManaged = isManaged;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        /// <summary>
        /// 获取被检测文件的完整路径。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 获取被检测文件的名称。
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        /// 获取检测出的 DLL 架构。
        /// </summary>
        public DllArchitecture Architecture { get; }

        /// <summary>
        /// 获取一个值，表示该文件是否包含 .NET CLR 头。
        /// </summary>
        public bool IsManaged { get; }

        /// <summary>
        /// 获取检测是否成功。
        /// </summary>
        public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// 获取失败时的错误描述。
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// 获取失败时捕获的原始异常。
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// 获取用于控制台展示的架构文本。
        /// </summary>
        public string ArchitectureDisplay => GetArchitectureDisplay(Architecture);

        /// <summary>
        /// 创建成功检测结果。
        /// </summary>
        /// <param name="filePath">被检测文件的完整路径。</param>
        /// <param name="architecture">检测出的架构。</param>
        /// <param name="isManaged">文件是否为 .NET 程序集。</param>
        /// <returns>成功的检测结果。</returns>
        public static DllArchitectureResult Success(string filePath, DllArchitecture architecture, bool isManaged)
        {
            return new DllArchitectureResult(filePath, architecture, isManaged, null, null);
        }

        /// <summary>
        /// 创建失败检测结果。
        /// </summary>
        /// <param name="filePath">被检测文件的完整路径。</param>
        /// <param name="errorMessage">错误描述。</param>
        /// <param name="exception">关联异常。</param>
        /// <returns>失败的检测结果。</returns>
        public static DllArchitectureResult Failure(string filePath, string errorMessage, Exception? exception)
        {
            return new DllArchitectureResult(filePath, DllArchitecture.Unknown, false, errorMessage, exception);
        }

        private static string GetArchitectureDisplay(DllArchitecture architecture)
        {
            return architecture switch
            {
                DllArchitecture.X86 => "32 位 (x86)",
                DllArchitecture.X64 => "64 位 (x64)",
                DllArchitecture.Arm32 => "32 位 (ARM)",
                DllArchitecture.Arm64 => "64 位 (ARM64)",
                DllArchitecture.Itanium64 => "64 位 (IA64)",
                DllArchitecture.AnyCpu => "Any CPU（32/64 位均可）",
                DllArchitecture.AnyCpuPrefer32Bit => "Any CPU（优先 32 位）",
                _ => "未知架构"
            };
        }
    }
}