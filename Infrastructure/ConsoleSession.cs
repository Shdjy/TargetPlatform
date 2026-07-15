using System;
using System.Runtime.InteropServices;

namespace TargetPlatform.Infrastructure
{
    /// <summary>
    /// 判断控制台是否由资源管理器独立启动，以决定是否保留结果窗口。
    /// </summary>
    internal static class ConsoleSession
    {
        /// <summary>
        /// 当程序未继承命令行宿主的控制台时返回 true。
        /// </summary>
        /// <returns>需要等待用户查看检测结果时返回 true。</returns>
        public static bool ShouldKeepConsoleOpen()
        {
            try
            {
                var processIds = new uint[2];
                uint processCount = GetConsoleProcessList(processIds, (uint)processIds.Length);
                return processCount == 1;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}