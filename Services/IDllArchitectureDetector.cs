using TargetPlatform.Models;

namespace TargetPlatform.Services
{
    /// <summary>
    /// 定义 Windows PE 文件（DLL/EXE）目标架构检测服务。
    /// </summary>
    public interface IDllArchitectureDetector
    {
        /// <summary>
        /// 读取指定 DLL 或 EXE 文件的 PE 文件头与 CLR 头，判断其目标处理器架构与程序集类型。
        /// </summary>
        /// <param name="filePath">待检测 PE 文件（.dll 或 .exe）的完整路径。</param>
        /// <returns>包含架构、文件类型、托管标识或失败原因的检测结果。</returns>
        DllArchitectureResult Detect(string filePath);
    }
}
