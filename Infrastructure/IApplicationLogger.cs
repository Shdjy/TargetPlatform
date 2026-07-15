using System;

namespace TargetPlatform.Infrastructure
{
    /// <summary>
    /// 定义应用程序运行日志的记录能力。
    /// </summary>
    public interface IApplicationLogger
    {
        /// <summary>
        /// 记录正常业务操作信息。
        /// </summary>
        /// <param name="module">产生日志的模块名称。</param>
        /// <param name="message">操作描述。</param>
        void Information(string module, string message);

        /// <summary>
        /// 记录异常或失败操作信息。
        /// </summary>
        /// <param name="module">产生日志的模块名称。</param>
        /// <param name="message">操作描述。</param>
        /// <param name="exception">关联异常；没有原始异常时可为 null。</param>
        void Error(string module, string message, Exception? exception);
    }
}