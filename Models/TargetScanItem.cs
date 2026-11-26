using System;

namespace TargetPlatform.Models
{
    /// <summary>
    /// 表示从命令行参数或目录扫描中收集到的单个待检测 PE 文件项。
    /// </summary>
    public sealed class TargetScanItem
    {
        /// <summary>
        /// 初始化待检测目标文件项。
        /// </summary>
        /// <param name="filePath">目标文件的规范化完整路径。</param>
        /// <param name="displayPath">用于在控制台和日志中展示的路径文本。</param>
        /// <param name="sourceDirectory">若该文件来自目录扫描，则为对应的根输入目录；单文件输入时为 null。</param>
        public TargetScanItem(string filePath, string displayPath, string? sourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空。", nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(displayPath))
            {
                throw new ArgumentException("展示路径不能为空。", nameof(displayPath));
            }

            FilePath = filePath;
            DisplayPath = displayPath;
            SourceDirectory = sourceDirectory;
        }

        /// <summary>
        /// 获取目标文件的规范化完整路径。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 获取用于在控制台和日志中展示的文件路径。
        /// </summary>
        public string DisplayPath { get; }

        /// <summary>
        /// 获取所属的根扫描目录；若为直接传入的单个文件则为 null。
        /// </summary>
        public string? SourceDirectory { get; }
    }
}
