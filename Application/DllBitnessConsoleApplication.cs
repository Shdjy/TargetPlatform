using System;
using System.IO;
using TargetPlatform.Infrastructure;
using TargetPlatform.Models;
using TargetPlatform.Services;

namespace TargetPlatform.Application
{
    /// <summary>
    /// 协调命令行参数处理、DLL 位数检测与控制台展示。
    /// </summary>
    public sealed class DllBitnessConsoleApplication
    {
        private readonly IDllArchitectureDetector _detector;
        private readonly IApplicationLogger _logger;

        /// <summary>
        /// 初始化控制台应用服务。
        /// </summary>
        /// <param name="detector">DLL 架构检测服务。</param>
        /// <param name="logger">应用日志服务。</param>
        public DllBitnessConsoleApplication(
            IDllArchitectureDetector detector,
            IApplicationLogger logger)
        {
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检测命令行中传入的一个或多个 DLL 文件，并输出检测结果。
        /// </summary>
        /// <param name="arguments">拖入程序的 DLL 文件路径集合。</param>
        /// <returns>全部成功时返回 0；参数为空时返回 1；存在检测失败时返回 2。</returns>
        public int Run(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                Console.WriteLine("请将一个或多个 DLL 文件拖到此程序上。");
                Console.WriteLine("示例：DllBitnessDetector.exe LibraryA.dll LibraryB.dll");
                _logger.Information("Console", "未接收到 DLL 文件路径。");
                return 1;
            }

            int failureCount = 0;
            Console.WriteLine("DLL 位数检测结果：");

            foreach (string argument in arguments)
            {
                if (!TryGetDllPath(argument, out string filePath, out string validationError))
                {
                    failureCount++;
                    Console.WriteLine("[失败] {0} -> {1}", argument, validationError);
                    _logger.Error("Console", validationError, null);
                    continue;
                }

                DllArchitectureResult result = _detector.Detect(filePath);
                WriteResult(result);

                if (result.IsSuccess)
                {
                    _logger.Information(
                        "DllArchitectureDetector",
                        string.Format("检测成功：{0}，结果：{1}。", result.FilePath, result.ArchitectureDisplay));
                }
                else
                {
                    failureCount++;
                    _logger.Error(
                        "DllArchitectureDetector",
                        string.Format("检测失败：{0}。", result.FilePath),
                        result.Exception);
                }
            }

            Console.WriteLine();
            Console.WriteLine("完成：共处理 {0} 个文件，失败 {1} 个。", arguments.Length, failureCount);
            return failureCount == 0 ? 0 : 2;
        }

        private static bool TryGetDllPath(string argument, out string filePath, out string validationError)
        {
            filePath = string.Empty;
            validationError = string.Empty;

            if (string.IsNullOrWhiteSpace(argument))
            {
                validationError = "文件路径不能为空。";
                return false;
            }

            try
            {
                filePath = Path.GetFullPath(argument.Trim().Trim('"'));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                validationError = "文件路径无效。";
                return false;
            }

            if (!string.Equals(Path.GetExtension(filePath), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                validationError = "仅支持 DLL 文件。";
                return false;
            }

            if (!File.Exists(filePath))
            {
                validationError = "找不到指定文件。";
                return false;
            }

            return true;
        }

        private static void WriteResult(DllArchitectureResult result)
        {
            if (result.IsSuccess)
            {
                string managedDescription = result.IsManaged ? ".NET 程序集" : "原生 DLL";
                Console.WriteLine(
                    "[成功] {0} -> {1}（{2}）",
                    result.FileName,
                    result.ArchitectureDisplay,
                    managedDescription);
                return;
            }

            Console.WriteLine("[失败] {0} -> {1}", result.FileName, result.ErrorMessage);
        }
    }
}