using System;
using System.Collections.Generic;
using System.Text;
using TargetPlatform.Infrastructure;
using TargetPlatform.Models;
using TargetPlatform.Services;

namespace TargetPlatform.Application
{
    /// <summary>
    /// 协调命令行参数收集、DLL/EXE 位数检测与控制台结果展示。
    /// </summary>
    public sealed class DllBitnessConsoleApplication
    {
        private const string kConsoleModule = "Console";
        private const string kDetectorModule = "DllArchitectureDetector";

        private static readonly DllArchitecture[] s_summaryOrder =
        {
            DllArchitecture.X64,
            DllArchitecture.X86,
            DllArchitecture.AnyCpu,
            DllArchitecture.AnyCpuPrefer32Bit,
            DllArchitecture.Arm64,
            DllArchitecture.Arm32,
            DllArchitecture.Itanium64,
            DllArchitecture.Unknown
        };

        private readonly IDllArchitectureDetector m_detector;
        private readonly ITargetFileCollector m_targetCollector;
        private readonly IApplicationLogger m_logger;

        /// <summary>
        /// 使用默认的文件系统目标收集器初始化控制台应用服务。
        /// </summary>
        /// <param name="detector">PE 架构检测服务。</param>
        /// <param name="logger">应用日志服务。</param>
        public DllBitnessConsoleApplication(
            IDllArchitectureDetector detector,
            IApplicationLogger logger)
            : this(detector, new FileSystemTargetCollector(logger), logger)
        {
        }

        /// <summary>
        /// 使用指定依赖服务初始化控制台应用服务。
        /// </summary>
        /// <param name="detector">PE 架构检测服务。</param>
        /// <param name="targetCollector">文件与目录目标收集服务。</param>
        /// <param name="logger">应用日志服务。</param>
        public DllBitnessConsoleApplication(
            IDllArchitectureDetector detector,
            ITargetFileCollector targetCollector,
            IApplicationLogger logger)
        {
            m_detector = detector ?? throw new ArgumentNullException(nameof(detector));
            m_targetCollector = targetCollector ?? throw new ArgumentNullException(nameof(targetCollector));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 解析传入的单个或多个 DLL/EXE 文件及目录路径，执行平台位数检测并输出结果。
        /// </summary>
        /// <param name="arguments">拖入程序的 DLL/EXE 文件路径或目录路径集合。</param>
        /// <returns>全部成功时返回 0；未传入任何参数时返回 1；存在无效输入或检测失败时返回 2。</returns>
        public int Run(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                Console.WriteLine("请将一个或多个 DLL/EXE 文件或目录拖到此程序上。");
                Console.WriteLine("示例：DllBitnessDetector.exe LibraryA.dll App.exe C:\\Plugins");
                m_logger.Information(kConsoleModule, "未接收到 DLL/EXE 文件或目录路径。");
                return 1;
            }

            TargetCollectionReport collectionReport = m_targetCollector.Collect(arguments);
            int failureCount = 0;
            int successCount = 0;
            var architectureCounts = new Dictionary<DllArchitecture, int>();

            Console.WriteLine("PE 文件（DLL/EXE）位数检测结果：");

            foreach (TargetInputError inputError in collectionReport.InputErrors)
            {
                failureCount++;
                Console.WriteLine("[失败] {0} -> {1}", inputError.InputArgument, inputError.ErrorMessage);
            }

            foreach (string skippedDirectory in collectionReport.SkippedDirectories)
            {
                Console.WriteLine("[警告] 跳过无法访问的目录 -> {0}", skippedDirectory);
            }

            foreach (string emptyDirectory in collectionReport.EmptyDirectories)
            {
                Console.WriteLine("[提示] 目录中未找到 DLL 或 EXE 文件 -> {0}", emptyDirectory);
            }

            foreach (TargetScanItem targetItem in collectionReport.TargetFiles)
            {
                DllArchitectureResult result = m_detector.Detect(targetItem.FilePath);
                WriteResult(targetItem.DisplayPath, result);

                if (result.IsSuccess)
                {
                    successCount++;
                    IncrementArchitectureCount(architectureCounts, result.Architecture);
                    m_logger.Information(
                        kDetectorModule,
                        string.Format(
                            "检测成功：{0}，结果：{1}（{2}）。",
                            result.FilePath,
                            result.ArchitectureDisplay,
                            result.FileTypeDescription));
                }
                else
                {
                    failureCount++;
                    m_logger.Error(
                        kDetectorModule,
                        string.Format("检测失败：{0}。", result.FilePath),
                        result.Exception);
                }
            }

            WriteSummary(collectionReport, successCount, failureCount, architectureCounts);
            return failureCount == 0 ? 0 : 2;
        }

        private void WriteSummary(
            TargetCollectionReport collectionReport,
            int successCount,
            int failureCount,
            IReadOnlyDictionary<DllArchitecture, int> architectureCounts)
        {
            Console.WriteLine();

            if (successCount > 0 && collectionReport.TargetFiles.Count > 1)
            {
                string distributionText = BuildDistributionSummary(architectureCounts);
                if (!string.IsNullOrEmpty(distributionText))
                {
                    Console.WriteLine("架构分布：{0}", distributionText);
                }
            }

            if (collectionReport.InputErrors.Count > 0)
            {
                Console.WriteLine(
                    "完成：共处理 {0} 个文件，成功 {1} 个，失败 {2} 个（含 {3} 个无效输入）。",
                    collectionReport.TargetFiles.Count,
                    successCount,
                    failureCount,
                    collectionReport.InputErrors.Count);
            }
            else
            {
                Console.WriteLine(
                    "完成：共处理 {0} 个文件，成功 {1} 个，失败 {2} 个。",
                    collectionReport.TargetFiles.Count,
                    successCount,
                    failureCount);
            }

            m_logger.Information(
                kConsoleModule,
                string.Format(
                    "检测结束：共处理 {0} 个文件，成功 {1} 个，失败 {2} 个。",
                    collectionReport.TargetFiles.Count,
                    successCount,
                    failureCount));
        }

        private static void IncrementArchitectureCount(
            IDictionary<DllArchitecture, int> architectureCounts,
            DllArchitecture architecture)
        {
            if (architectureCounts.TryGetValue(architecture, out int count))
            {
                architectureCounts[architecture] = count + 1;
            }
            else
            {
                architectureCounts[architecture] = 1;
            }
        }

        private static string BuildDistributionSummary(IReadOnlyDictionary<DllArchitecture, int> architectureCounts)
        {
            var builder = new StringBuilder();
            foreach (DllArchitecture architecture in s_summaryOrder)
            {
                if (!architectureCounts.TryGetValue(architecture, out int count) || count <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("，");
                }

                builder.Append(GetSummaryLabel(architecture));
                builder.Append(' ');
                builder.Append(count);
                builder.Append(" 个");
            }

            return builder.ToString();
        }

        private static string GetSummaryLabel(DllArchitecture architecture)
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

        private static void WriteResult(string displayPath, DllArchitectureResult result)
        {
            if (result.IsSuccess)
            {
                Console.WriteLine(
                    "[成功] {0} -> {1}（{2}）",
                    displayPath,
                    result.ArchitectureDisplay,
                    result.FileTypeDescription);
                return;
            }

            Console.WriteLine("[失败] {0} -> {1}", displayPath, result.ErrorMessage);
        }
    }
}
