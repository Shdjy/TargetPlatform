namespace TargetPlatform.Models
{
    /// <summary>
    /// 表示 DLL 的目标处理器架构或运行位数。
    /// </summary>
    public enum DllArchitecture
    {
        /// <summary>
        /// 无法从文件中识别架构。
        /// </summary>
        Unknown,

        /// <summary>
        /// 32 位 x86 架构。
        /// </summary>
        X86,

        /// <summary>
        /// 64 位 x64 架构。
        /// </summary>
        X64,

        /// <summary>
        /// 32 位 ARM 架构。
        /// </summary>
        Arm32,

        /// <summary>
        /// 64 位 ARM 架构。
        /// </summary>
        Arm64,

        /// <summary>
        /// 64 位 Itanium 架构。
        /// </summary>
        Itanium64,

        /// <summary>
        /// .NET Any CPU 程序集，可在 32 位或 64 位进程中运行。
        /// </summary>
        AnyCpu,

        /// <summary>
        /// .NET Any CPU 程序集，优先在 32 位进程中运行。
        /// </summary>
        AnyCpuPrefer32Bit
    }
}