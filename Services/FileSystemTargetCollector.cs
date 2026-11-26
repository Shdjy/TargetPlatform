using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using TargetPlatform.Infrastructure;
using TargetPlatform.Models;

namespace TargetPlatform.Services
{
    /// <summary>
    /// 基于文件系统实现单个 DLL/EXE 文件解析及拖拽目录的递归扫描收集。
    /// </summary>
    public sealed class FileSystemTargetCollector : ITargetFileCollector
    {
        private const string kModuleName = "TargetCollector";
        private const string kDllExtension = ".dll";
        private const string kExeExtension = ".exe";

        private readonly IApplicationLogger m_logger;

        /// <summary>
        /// 初始化文件系统目标收集服务。
        /// </summary>
        /// <param name="logger">应用日志记录服务。</param>
        public FileSystemTargetCollector(IApplicationLogger logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public TargetCollectionReport Collect(string[] arguments)
        {
            var collectedMap = new Dictionary<string, TargetScanItem>(StringComparer.OrdinalIgnoreCase);
            var emptyDirectories = new List<string>();
            var skippedDirectories = new List<string>();
            var inputErrors = new List<TargetInputError>();

            if (arguments == null || arguments.Length == 0)
            {
                return new TargetCollectionReport(
                    Array.Empty<TargetScanItem>(),
                    emptyDirectories,
                    skippedDirectories,
                    inputErrors);
            }

            List<string> parsedDirectoryRoots = CollectExistingDirectoryRoots(arguments);
            bool includeRootPrefix = parsedDirectoryRoots.Count > 1;
            bool hasDuplicateRootName = HasDuplicateDirectoryName(parsedDirectoryRoots);

            foreach (string argument in arguments)
            {
                if (!TryNormalizePath(argument, out string fullPath, out string normalizeError))
                {
                    inputErrors.Add(new TargetInputError(argument ?? string.Empty, normalizeError));
                    m_logger.Error(kModuleName, string.Format("无效输入参数：{0}，原因：{1}", argument, normalizeError), null);
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    HandleSingleFile(argument, fullPath, collectedMap, inputErrors);
                    continue;
                }

                if (Directory.Exists(fullPath))
                {
                    string rootPath = TrimNonRootDirectorySeparator(fullPath);
                    HandleDirectory(
                        rootPath,
                        includeRootPrefix,
                        hasDuplicateRootName,
                        collectedMap,
                        emptyDirectories,
                        skippedDirectories);
                    continue;
                }

                const string notFoundMessage = "找不到指定的文件或目录。";
                inputErrors.Add(new TargetInputError(argument, notFoundMessage));
                m_logger.Error(kModuleName, string.Format("路径不存在：{0}。", fullPath), null);
            }

            var orderedTargets = new List<TargetScanItem>(collectedMap.Values);
            orderedTargets.Sort(CompareTargetScanItems);

            return new TargetCollectionReport(orderedTargets, emptyDirectories, skippedDirectories, inputErrors);
        }

        private void HandleSingleFile(
            string originalArgument,
            string fullPath,
            IDictionary<string, TargetScanItem> collectedMap,
            ICollection<TargetInputError> inputErrors)
        {
            if (!IsSupportedPeExtension(fullPath))
            {
                const string unsupportedMessage = "仅支持 .dll 或 .exe 文件，或包含这些文件的目录。";
                inputErrors.Add(new TargetInputError(originalArgument, unsupportedMessage));
                m_logger.Error(kModuleName, string.Format("不支持的文件类型：{0}。", fullPath), null);
                return;
            }

            if (!collectedMap.ContainsKey(fullPath))
            {
                string fileName = Path.GetFileName(fullPath);
                collectedMap[fullPath] = new TargetScanItem(fullPath, fileName, null);
            }
        }

        private void HandleDirectory(
            string rootDirectory,
            bool includeRootPrefix,
            bool useFullRootPrefix,
            IDictionary<string, TargetScanItem> collectedMap,
            ICollection<string> emptyDirectories,
            ICollection<string> skippedDirectories)
        {
            m_logger.Information(kModuleName, string.Format("开始扫描目录：{0}。", rootDirectory));

            int matchedFileCount = 0;
            var visitedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var directoryStack = new Stack<string>();
            directoryStack.Push(rootDirectory);

            while (directoryStack.Count > 0)
            {
                string currentDirectory = directoryStack.Pop();
                if (!visitedDirectories.Add(currentDirectory))
                {
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDirectory);
                }
                catch (Exception exception) when (IsDirectoryAccessException(exception))
                {
                    skippedDirectories.Add(currentDirectory);
                    m_logger.Error(kModuleName, string.Format("无法读取目录中的文件：{0}。", currentDirectory), exception);
                    continue;
                }

                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string filePath in files)
                {
                    if (!IsSupportedPeExtension(filePath))
                    {
                        continue;
                    }

                    matchedFileCount++;
                    string fullFilePath = Path.GetFullPath(filePath);
                    if (collectedMap.ContainsKey(fullFilePath))
                    {
                        continue;
                    }

                    string displayPath = BuildDirectoryDisplayPath(
                        rootDirectory,
                        fullFilePath,
                        includeRootPrefix,
                        useFullRootPrefix);
                    collectedMap[fullFilePath] = new TargetScanItem(fullFilePath, displayPath, rootDirectory);
                }

                PushSubDirectories(currentDirectory, directoryStack, skippedDirectories);
            }

            if (matchedFileCount == 0)
            {
                emptyDirectories.Add(rootDirectory);
                m_logger.Information(kModuleName, string.Format("目录中未找到任何 DLL 或 EXE 文件：{0}。", rootDirectory));
                return;
            }

            m_logger.Information(
                kModuleName,
                string.Format("目录扫描完成：{0}，共发现 {1} 个支持的文件。", rootDirectory, matchedFileCount));
        }

        private void PushSubDirectories(
            string currentDirectory,
            Stack<string> directoryStack,
            ICollection<string> skippedDirectories)
        {
            string[] subDirectories;
            try
            {
                subDirectories = Directory.GetDirectories(currentDirectory);
            }
            catch (Exception exception) when (IsDirectoryAccessException(exception))
            {
                skippedDirectories.Add(currentDirectory);
                m_logger.Error(kModuleName, string.Format("无法枚举子目录：{0}。", currentDirectory), exception);
                return;
            }

            Array.Sort(subDirectories, StringComparer.OrdinalIgnoreCase);

            // 逆序压栈以保证出栈顺序与目录字母升序保持一致。
            for (int index = subDirectories.Length - 1; index >= 0; index--)
            {
                string subDirectory = subDirectories[index];
                if (IsReparsePointDirectory(subDirectory))
                {
                    m_logger.Information(kModuleName, string.Format("跳过符号链接/目录联接：{0}。", subDirectory));
                    continue;
                }

                directoryStack.Push(subDirectory);
            }
        }

        private static bool IsReparsePointDirectory(string directoryPath)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(directoryPath);
                return (attributes & FileAttributes.ReparsePoint) != 0;
            }
            catch (Exception exception) when (IsDirectoryAccessException(exception))
            {
                return false;
            }
        }

        private static List<string> CollectExistingDirectoryRoots(string[] arguments)
        {
            var uniqueRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new List<string>();

            foreach (string argument in arguments)
            {
                if (!TryNormalizePath(argument, out string fullPath, out _))
                {
                    continue;
                }

                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                string normalizedRoot = TrimNonRootDirectorySeparator(fullPath);
                if (uniqueRoots.Add(normalizedRoot))
                {
                    roots.Add(normalizedRoot);
                }
            }

            return roots;
        }

        private static bool HasDuplicateDirectoryName(IReadOnlyList<string> directoryRoots)
        {
            if (directoryRoots.Count <= 1)
            {
                return false;
            }

            var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in directoryRoots)
            {
                string directoryName = GetRootDisplayLabel(root);
                if (!nameSet.Add(directoryName))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildDirectoryDisplayPath(
            string rootDirectory,
            string fullFilePath,
            bool includeRootPrefix,
            bool useFullRootPrefix)
        {
            string relativePath = Path.GetRelativePath(rootDirectory, fullFilePath);
            if (!includeRootPrefix)
            {
                return relativePath;
            }

            string prefix = useFullRootPrefix ? rootDirectory : GetRootDisplayLabel(rootDirectory);
            return Path.Combine(prefix, relativePath);
        }

        private static string GetRootDisplayLabel(string directoryPath)
        {
            string trimmed = TrimNonRootDirectorySeparator(directoryPath);
            string name = Path.GetFileName(trimmed);
            return string.IsNullOrWhiteSpace(name) ? trimmed : name;
        }

        private static string TrimNonRootDirectorySeparator(string path)
        {
            string trimmed = Path.TrimEndingDirectorySeparator(path);
            return string.IsNullOrEmpty(trimmed) ? path : trimmed;
        }

        private static bool TryNormalizePath(string argument, out string fullPath, out string validationError)
        {
            fullPath = string.Empty;
            validationError = string.Empty;

            if (string.IsNullOrWhiteSpace(argument))
            {
                validationError = "路径不能为空。";
                return false;
            }

            string cleanedArgument = argument.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(cleanedArgument))
            {
                validationError = "路径不能为空。";
                return false;
            }

            // 处理 Windows 命令行传入 "X:\" 时反斜杠被转义吃掉导致只剩 "X:" 的场景。
            if (cleanedArgument.Length == 2 &&
                char.IsLetter(cleanedArgument[0]) &&
                cleanedArgument[1] == ':' &&
                argument.TrimEnd().EndsWith('"'))
            {
                cleanedArgument += Path.DirectorySeparatorChar;
            }

            try
            {
                fullPath = Path.GetFullPath(cleanedArgument);
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException ||
                exception is SecurityException)
            {
                validationError = "文件路径或目录路径无效。";
                return false;
            }
        }

        private static bool IsSupportedPeExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return string.Equals(extension, kDllExtension, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, kExeExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectoryAccessException(Exception exception)
        {
            return exception is UnauthorizedAccessException ||
                   exception is IOException ||
                   exception is DirectoryNotFoundException ||
                   exception is SecurityException;
        }

        private static int CompareTargetScanItems(TargetScanItem left, TargetScanItem right)
        {
            int displayCompare = StringComparer.OrdinalIgnoreCase.Compare(left.DisplayPath, right.DisplayPath);
            if (displayCompare != 0)
            {
                return displayCompare;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(left.FilePath, right.FilePath);
        }
    }
}
