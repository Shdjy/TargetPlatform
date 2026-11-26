using System;
using System.IO;

namespace TargetPlatform.Models
{
    /// <summary>
    /// 封装单个 PE 文件（DLL/EXE）的架构检测结果。
    /// </summary>
    public sealed class DllArchitectureResult
    {
        private DllArchitectureResult(
            string filePath,
            DllArchitecture architecture,
            bool isManaged,
            PeFileType fileType,
            string? errorMessage,
            Exception? exception)
        {
            FilePath = filePath;
            Architecture = architecture;
            IsManaged = isManaged;
            FileType = fileType;
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
        /// 获取检测出的目标处理器架构。
        /// </summary>
        public DllArchitecture Architecture { get; }

        /// <summary>
        /// 获取一个值，表示该文件是否包含 .NET CLR 头。
        /// </summary>
        public bool IsManaged { get; }

        /// <summary>
        /// 获取被检测 PE 文件的类型（DLL 或 EXE）。
        /// </summary>
        public PeFileType FileType { get; }

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
        /// 获取用于控制台展示的文件程序集类型文本。
        /// </summary>
        public string FileTypeDescription => GetFileTypeDescription(IsManaged, FileType);

        /// <summary>
        /// 创建成功检测结果，并根据文件扩展名自动推断 PE 文件类型。
        /// </summary>
        /// <param name="filePath">被检测文件的完整路径。</param>
        /// <param name="architecture">检测出的架构。</param>
        /// <param name="isManaged">文件是否为 .NET 程序集。</param>
        /// <returns>成功的检测结果。</returns>
        public static DllArchitectureResult Success(string filePath, DllArchitecture architecture, bool isManaged)
        {
            PeFileType inferredType = InferFileTypeFromPath(filePath);
            return new DllArchitectureResult(filePath, architecture, isManaged, inferredType, null, null);
        }

        /// <summary>
        /// 创建包含明确文件类型的成功检测结果。
        /// </summary>
        /// <param name="filePath">被检测文件的完整路径。</param>
        /// <param name="architecture">检测出的架构。</param>
        /// <param name="isManaged">文件是否为 .NET 程序集。</param>
        /// <param name="fileType">解析得到的 PE 文件类型。</param>
        /// <returns>成功的检测结果。</returns>
        public static DllArchitectureResult Success(
            string filePath,
            DllArchitecture architecture,
            bool isManaged,
            PeFileType fileType)
        {
            return new DllArchitectureResult(filePath, architecture, isManaged, fileType, null, null);
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
            PeFileType inferredType = InferFileTypeFromPath(filePath);
            return new DllArchitectureResult(filePath, DllArchitecture.Unknown, false, inferredType, errorMessage, exception);
        }

        private static PeFileType InferFileTypeFromPath(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                return PeFileType.Exe;
            }

            if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
            {
                return PeFileType.Dll;
            }

            return PeFileType.Unknown;
        }

        private static string GetFileTypeDescription(bool isManaged, PeFileType fileType)
        {
            if (isManaged)
            {
                return ".NET 程序集";
            }

            return fileType switch
            {
                PeFileType.Exe => "原生 EXE",
                PeFileType.Dll => "原生 DLL",
                _ => "原生 PE 文件"
            };
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
