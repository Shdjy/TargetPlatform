namespace TargetPlatform.Models
{
    /// <summary>
    /// 表示待检测或已检测的 PE 文件类型。
    /// </summary>
    public enum PeFileType
    {
        /// <summary>
        /// 未知或未明确区分的 PE 文件类型。
        /// </summary>
        Unknown,

        /// <summary>
        /// 动态链接库文件（.dll）。
        /// </summary>
        Dll,

        /// <summary>
        /// 可执行程序文件（.exe）。
        /// </summary>
        Exe
    }
}
