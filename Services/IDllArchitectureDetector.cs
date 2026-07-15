using TargetPlatform.Models;

namespace TargetPlatform.Services
{
    /// <summary>
    /// 定义 DLL 目标架构检测服务。
    /// </summary>
    public interface IDllArchitectureDetector
    {
        /// <summary>
        /// 读取 DLL 的 PE 文件头并判断其目标架构。
        /// </summary>
        /// <param name="filePath">待检测 DLL 的完整路径。</param>
        /// <returns>包含架构、程序集类型或失败原因的检测结果。</returns>
        DllArchitectureResult Detect(string filePath);
    }
}