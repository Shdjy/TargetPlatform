using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace TargetPlatform.Infrastructure
{
    /// <summary>
    /// 将应用日志追加写入当前用户本地应用数据目录的日志实现。
    /// </summary>
    public sealed class FileApplicationLogger : IApplicationLogger
    {
        private readonly object _syncRoot = new object();
        private readonly string _logFilePath;

        /// <summary>
        /// 使用默认日志目录初始化日志服务。
        /// </summary>
        public FileApplicationLogger()
        {
            string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logFilePath = Path.Combine(applicationDataPath, "DllBitnessDetector", "Logs", "application.log");
        }

        /// <inheritdoc />
        public void Information(string module, string message)
        {
            Write("INFO", module, message, null);
        }

        /// <inheritdoc />
        public void Error(string module, string message, Exception? exception)
        {
            Write("ERROR", module, message, exception);
        }

        private void Write(string level, string module, string message, Exception? exception)
        {
            string exceptionMessage = exception == null ? string.Empty : " 异常：" + exception;
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] {3}{4}",
                DateTime.Now,
                level,
                module,
                message,
                exceptionMessage);

            try
            {
                lock (_syncRoot)
                {
                    string? directory = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(_logFilePath, line + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
            }
            catch
            {
                // 日志写入失败不能影响 DLL 检测主流程。
            }
        }
    }
}