using System;
using TargetPlatform.Application;
using TargetPlatform.Infrastructure;
using TargetPlatform.Services;

namespace TargetPlatform
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            IApplicationLogger logger = new FileApplicationLogger();
            IDllArchitectureDetector detector = new PeDllArchitectureDetector();
            var application = new DllBitnessConsoleApplication(detector, logger);

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