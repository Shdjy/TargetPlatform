# DLL 位数检测工具

这是一个 Windows 控制台程序。将一个或多个 `.dll` 文件拖到编译后的 `DllBitnessDetector.exe` 上，即可逐行输出每个 DLL 的目标架构。

## 输出类型

- `32 位 (x86)`
- `64 位 (x64)`
- `32 位 (ARM)`
- `64 位 (ARM64)`
- `Any CPU（32/64 位均可）`
- `Any CPU（优先 32 位）`

程序直接读取 PE 文件头，不会加载或执行被检测的 DLL。对于 .NET DLL，额外读取 CLR 标志以区分 `x86`、`Any CPU` 与“优先 32 位”。

## 使用方式

1. 编译发布程序。
2. 在资源管理器中选中一个或多个 DLL 文件。
3. 将选中的文件拖到 `DllBitnessDetector.exe` 上。
4. 控制台会依次显示文件名、位数和程序集类型。

也可在命令行中执行：

```powershell
.\DllBitnessDetector.exe "C:\LibraryA.dll" "C:\LibraryB.dll"
```

## 构建与发布

当前项目目标框架为 `.NET 5.0`，与工作区已安装的 SDK 保持一致。

```powershell
dotnet build .\TargetPlatform.csproj -c Release
dotnet publish .\TargetPlatform.csproj -c Release -r win-x64 --self-contained false
```

运行日志保存在当前用户目录：`%LocalAppData%\DllBitnessDetector\Logs\application.log`。