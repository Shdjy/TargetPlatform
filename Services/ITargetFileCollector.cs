using TargetPlatform.Models;

namespace TargetPlatform.Services
{
    /// <summary>
    /// 定义从用户输入参数（单个文件或目录）中收集待检测 PE 文件的服务接口。
    /// </summary>
    public interface ITargetFileCollector
    {
        /// <summary>
        /// 解析输入的文件与目录路径集合，并收集所有待检测的 DLL 与 EXE 文件。
        /// </summary>
        /// <param name="arguments">来自命令行或拖拽操作的文件与目录路径参数集合。</param>
        /// <returns>包含有效目标文件列表、空目录列表、跳过目录列表及输入错误信息的汇总报告。</returns>
        TargetCollectionReport Collect(string[] arguments);
    }
}
