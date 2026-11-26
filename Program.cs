using System;
using TargetPlatform.Application;
using TargetPlatform.Infrastructure;
using TargetPlatform.Services;

namespace TargetPlatform
{
    /// <summary>
    /// 应用程序入口点，负责组装依赖服务并启动控制台检测流程。
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            IApplicationLogger logger = new FileApplicationLogger();
            IDllArchitectureDetector detector = new PeDllArchitectureDetector();
            ITargetFileCollector targetCollector = new FileSystemTargetCollector(logger);
            var application = new DllBitnessConsoleApplication(detector, targetCollector, logger);

            int exitCode = application.Run(args);

            if (ConsoleSession.ShouldKeepConsoleOpen())
            {
                Console.WriteLine();
                Console.WriteLine("按任意键关闭窗口...");
                Console.ReadKey(intercept: true);
            }

            Environment.ExitCode = exitCode;
        }
    }
}
