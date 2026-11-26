using System;
using System.Collections.Generic;

namespace TargetPlatform.Models
{
    /// <summary>
    /// 封装从输入参数中收集待检测 PE 文件的完整汇总报告。
    /// </summary>
    public sealed class TargetCollectionReport
    {
        /// <summary>
        /// 初始化目标收集汇总报告。
        /// </summary>
        /// <param name="targetFiles">已去重并排序的待检测 PE 文件列表。</param>
        /// <param name="emptyDirectories">未包含任何支持文件的有效目录列表。</param>
        /// <param name="skippedDirectories">因权限或访问受限而跳过的目录列表。</param>
        /// <param name="inputErrors">无效或不支持的输入参数错误列表。</param>
        public TargetCollectionReport(
            IReadOnlyList<TargetScanItem> targetFiles,
            IReadOnlyList<string> emptyDirectories,
            IReadOnlyList<string> skippedDirectories,
            IReadOnlyList<TargetInputError> inputErrors)
        {
            TargetFiles = targetFiles ?? throw new ArgumentNullException(nameof(targetFiles));
            EmptyDirectories = emptyDirectories ?? throw new ArgumentNullException(nameof(emptyDirectories));
            SkippedDirectories = skippedDirectories ?? throw new ArgumentNullException(nameof(skippedDirectories));
            InputErrors = inputErrors ?? throw new ArgumentNullException(nameof(inputErrors));
        }

        /// <summary>
        /// 获取待检测的目标文件列表。
        /// </summary>
        public IReadOnlyList<TargetScanItem> TargetFiles { get; }

        /// <summary>
        /// 获取未发现任何 DLL 或 EXE 文件的目录列表。
        /// </summary>
        public IReadOnlyList<string> EmptyDirectories { get; }

        /// <summary>
        /// 获取扫描期间因权限或异常而跳过的目录列表。
        /// </summary>
        public IReadOnlyList<string> SkippedDirectories { get; }

        /// <summary>
        /// 获取输入参数校验失败项列表。
        /// </summary>
        public IReadOnlyList<TargetInputError> InputErrors { get; }
    }
}
