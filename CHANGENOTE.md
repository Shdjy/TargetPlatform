# 变更记录

## 1.1.0 - 2026-11-26

- 新增单个及多个 `.exe` 可执行文件的 PE 平台位数检测支持，并与 `.dll` 统一兼容。
- 新增拖拽目录（支持多目录及文件/目录混合传入）功能，自动递归扫描目录下全部 `.dll` 与 `.exe` 文件、去重排序并输出相对路径。
- 新增目录扫描容错与汇总统计：支持跳过符号链接目录及无权限子目录告警，并在多文件检测结束后输出各架构分布统计。
- 新增 `ITargetFileCollector` 与 `FileSystemTargetCollector` 模块及 `PeFileType`、`TargetScanItem`、`TargetInputError`、`TargetCollectionReport` 模型，分离输入收集与控制台展示职责。
- 修复 `PeDllArchitectureDetector` 中 `.NET` 程序集同时存在 `32BitRequired` 与 `32BitPreferred` 标志时 `Any CPU（优先 32 位）` 被误判为 `x86` 的顺序问题，并增加 `ARM Thumb-2 (0x01C4)` 识别与共享读取兼容性。
- 在 `TargetPlatform.csproj` 中启用 `RollForward=Major`，支持在仅安装更高主版本 .NET 运行时（如 .NET 6/8/9）的机器上直接运行。

## 1.0.0 - 2026-07-15

- 新增 Windows 控制台 DLL 位数检测工具。
- 支持一次拖入多个 DLL 文件并逐个输出检测结果。
- 新增 PE 文件头解析，支持 x86、x64、ARM、ARM64、IA64 与 .NET Any CPU 检测。
- 新增应用日志，记录检测操作与异常信息。
- 新增项目使用说明和发布命令。
