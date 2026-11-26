# DLL / EXE 位数检测工具

这是一个 Windows 控制台程序。将一个或多个 `.dll`、`.exe` 文件，或者整个文件夹拖到编译后的 `DllBitnessDetector.exe` 上，即可批量检测并逐行输出每个 PE 文件的目标平台架构与位数。

## 核心功能

- **单文件与多文件检测**：支持单个或多个 `.dll`、`.exe` 文件的平台位数识别。
- **目录拖拽递归扫描**：支持直接拖入一个或多个目录，自动递归扫描目录下的全部 `.dll` 与 `.exe` 文件，去重并按路径有序输出。
- **混合输入与汇总**：支持同时传入多个文件与目录，在检测末尾展示各架构的数量统计与成功/失败汇总。
- **容错处理**：安全跳过目录联接（符号链接）及无权限访问的子目录，避免扫描意外中断。

## 输出类型

- `32 位 (x86)`
- `64 位 (x64)`
- `32 位 (ARM)`
- `64 位 (ARM64)`
- `64 位 (IA64)`
- `Any CPU（32/64 位均可）`
- `Any CPU（优先 32 位）`

程序直接解析文件头（DOS 头、PE 头、COFF 特性与 CLR 头），不会加载或执行被检测的 DLL/EXE。对于 `.NET` 程序集，额外读取 CLR 标志以准确区分 `x86`、`Any CPU` 与 `Any CPU（优先 32 位）`。

## 使用方式

### 1. 资源管理器拖拽

1. 编译发布程序。
2. 在资源管理器中选中一个或多个 `.dll`/`.exe` 文件，或直接选中一个或多个目录。
3. 将选中的文件或目录拖到 `DllBitnessDetector.exe` 上。
4. 控制台会依次显示文件路径、平台位数、程序集类型（`.NET 程序集` / `原生 DLL` / `原生 EXE`）及架构统计汇总。

### 2. 命令行执行

```powershell
# 检测单个 DLL 与单个 EXE
.\DllBitnessDetector.exe "C:\App\MyApp.exe" "C:\App\LibraryA.dll"

# 检测整个目录（自动递归子目录）
.\DllBitnessDetector.exe "C:\App\Release"

# 同时检测多个目录与文件
.\DllBitnessDetector.exe "C:\App\Plugins" "C:\Tools\Helper.exe"
```

## 构建与发布

当前项目目标框架为 `.NET 5.0`，并已启用 `RollForward=Major` 兼容配置，可在已安装 `.NET 5/6/8/9` 运行时的 Windows 环境下直接运行。

```powershell
dotnet build .\TargetPlatform.csproj -c Release
dotnet publish .\TargetPlatform.csproj -c Release -r win-x64 --self-contained false
```

运行日志保存在当前用户目录：`%LocalAppData%\DllBitnessDetector\Logs\application.log`。
