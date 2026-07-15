using System;
using System.IO;
using System.Text;
using TargetPlatform.Models;

namespace TargetPlatform.Services
{
    /// <summary>
    /// 基于 Windows PE 文件头实现 DLL 目标架构检测。
    /// </summary>
    public sealed class PeDllArchitectureDetector : IDllArchitectureDetector
    {
        private const ushort DosSignature = 0x5A4D;
        private const uint PeSignature = 0x00004550;
        private const ushort Pe32Magic = 0x010B;
        private const ushort Pe32PlusMagic = 0x020B;
        private const ushort MachineI386 = 0x014C;
        private const ushort MachineArm = 0x01C0;
        private const ushort MachineItanium = 0x0200;
        private const ushort MachineAmd64 = 0x8664;
        private const ushort MachineArm64 = 0xAA64;
        private const uint CorFlag32BitRequired = 0x00000002;
        private const uint CorFlag32BitPreferred = 0x00020000;
        private const int SectionHeaderSize = 40;
        private const int CliDataDirectoryIndex = 14;

        /// <inheritdoc />
        public DllArchitectureResult Detect(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

                PeHeaders headers = ReadPeHeaders(reader);
                bool isManaged = TryReadCorFlags(reader, headers, out uint corFlags);
                DllArchitecture architecture = ResolveArchitecture(headers.Machine, isManaged, corFlags);

                return DllArchitectureResult.Success(filePath, architecture, isManaged);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is InvalidDataException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                return DllArchitectureResult.Failure(filePath, "不是有效的 PE DLL 文件，或文件无法读取。", exception);
            }
        }

        private static PeHeaders ReadPeHeaders(BinaryReader reader)
        {
            Stream stream = reader.BaseStream;
            EnsureAvailable(stream, 0, 64, "文件长度不足，缺少 DOS 文件头。");

            stream.Position = 0;
            if (reader.ReadUInt16() != DosSignature)
            {
                throw new InvalidDataException("缺少 MZ 文件标识。");
            }

            stream.Position = 0x3C;
            int peHeaderOffset = reader.ReadInt32();
            if (peHeaderOffset < 0)
            {
                throw new InvalidDataException("PE 文件头偏移无效。");
            }

            EnsureAvailable(stream, peHeaderOffset, 24, "文件长度不足，缺少 PE 文件头。");
            stream.Position = peHeaderOffset;

            if (reader.ReadUInt32() != PeSignature)
            {
                throw new InvalidDataException("缺少 PE 文件标识。");
            }

            ushort machine = reader.ReadUInt16();
            ushort sectionCount = reader.ReadUInt16();
            stream.Position += 12;
            ushort optionalHeaderSize = reader.ReadUInt16();
            long optionalHeaderOffset = peHeaderOffset + 24L;

            EnsureAvailable(stream, optionalHeaderOffset, optionalHeaderSize, "可选 PE 文件头不完整。");
            if (optionalHeaderSize < 2)
            {
                throw new InvalidDataException("可选 PE 文件头无效。");
            }

            stream.Position = optionalHeaderOffset;
            ushort optionalHeaderMagic = reader.ReadUInt16();
            if (optionalHeaderMagic != Pe32Magic && optionalHeaderMagic != Pe32PlusMagic)
            {
                throw new InvalidDataException("不支持的 PE 可选文件头格式。");
            }

            uint cliHeaderRva = ReadCliHeaderRva(reader, optionalHeaderOffset, optionalHeaderSize, optionalHeaderMagic);
            long sectionHeadersOffset = optionalHeaderOffset + optionalHeaderSize;
            EnsureAvailable(stream, sectionHeadersOffset, (long)sectionCount * SectionHeaderSize, "PE 节表不完整。");

            return new PeHeaders(machine, sectionCount, sectionHeadersOffset, cliHeaderRva);
        }

        private static uint ReadCliHeaderRva(
            BinaryReader reader,
            long optionalHeaderOffset,
            ushort optionalHeaderSize,
            ushort optionalHeaderMagic)
        {
            long dataDirectoryOffset = optionalHeaderOffset + (optionalHeaderMagic == Pe32Magic ? 96 : 112);
            long cliDirectoryOffset = dataDirectoryOffset + (CliDataDirectoryIndex * 8L);
            long optionalHeaderEnd = optionalHeaderOffset + optionalHeaderSize;

            if (cliDirectoryOffset + 8 > optionalHeaderEnd)
            {
                return 0;
            }

            reader.BaseStream.Position = cliDirectoryOffset;
            return reader.ReadUInt32();
        }

        private static bool TryReadCorFlags(BinaryReader reader, PeHeaders headers, out uint corFlags)
        {
            corFlags = 0;
            if (headers.CliHeaderRva == 0)
            {
                return false;
            }

            Stream stream = reader.BaseStream;
            for (int index = 0; index < headers.SectionCount; index++)
            {
                long sectionHeaderOffset = headers.SectionHeadersOffset + ((long)index * SectionHeaderSize);
                stream.Position = sectionHeaderOffset + 8;

                // PE 节头依次保存：VirtualSize、VirtualAddress、SizeOfRawData、PointerToRawData。
                uint virtualSize = reader.ReadUInt32();
                uint virtualAddress = reader.ReadUInt32();
                uint rawDataSize = reader.ReadUInt32();
                uint rawDataOffset = reader.ReadUInt32();

                ulong sectionSize = Math.Max(virtualSize, rawDataSize);
                ulong sectionStart = virtualAddress;
                ulong sectionEnd = sectionStart + sectionSize;
                ulong cliRva = headers.CliHeaderRva;

                if (cliRva < sectionStart || cliRva >= sectionEnd)
                {
                    continue;
                }

                long cliHeaderOffset = rawDataOffset + (long)(cliRva - sectionStart);
                EnsureAvailable(stream, cliHeaderOffset, 20, "CLR 文件头不完整。");
                stream.Position = cliHeaderOffset + 16;
                corFlags = reader.ReadUInt32();
                return true;
            }

            return false;
        }

        private static DllArchitecture ResolveArchitecture(ushort machine, bool isManaged, uint corFlags)
        {
            if (machine == MachineI386 && isManaged)
            {
                if ((corFlags & CorFlag32BitRequired) != 0)
                {
                    return DllArchitecture.X86;
                }

                if ((corFlags & CorFlag32BitPreferred) != 0)
                {
                    return DllArchitecture.AnyCpuPrefer32Bit;
                }

                return DllArchitecture.AnyCpu;
            }

            return machine switch
            {
                MachineI386 => DllArchitecture.X86,
                MachineAmd64 => DllArchitecture.X64,
                MachineArm => DllArchitecture.Arm32,
                MachineArm64 => DllArchitecture.Arm64,
                MachineItanium => DllArchitecture.Itanium64,
                _ => DllArchitecture.Unknown
            };
        }

        private static void EnsureAvailable(Stream stream, long offset, long length, string message)
        {
            if (offset < 0 || length < 0 || offset > stream.Length || length > stream.Length - offset)
            {
                throw new InvalidDataException(message);
            }
        }

        private sealed class PeHeaders
        {
            /// <summary>
            /// 初始化 PE 头解析结果。
            /// </summary>
            /// <param name="machine">COFF 文件头中的处理器标识。</param>
            /// <param name="sectionCount">PE 节数量。</param>
            /// <param name="sectionHeadersOffset">节表在文件中的起始偏移。</param>
            /// <param name="cliHeaderRva">CLR 文件头的相对虚拟地址。</param>
            public PeHeaders(ushort machine, ushort sectionCount, long sectionHeadersOffset, uint cliHeaderRva)
            {
                Machine = machine;
                SectionCount = sectionCount;
                SectionHeadersOffset = sectionHeadersOffset;
                CliHeaderRva = cliHeaderRva;
            }

            /// <summary>
            /// 获取 COFF 文件头中的处理器标识。
            /// </summary>
            public ushort Machine { get; }

            /// <summary>
            /// 获取 PE 节数量。
            /// </summary>
            public ushort SectionCount { get; }

            /// <summary>
            /// 获取节表在文件中的起始偏移。
            /// </summary>
            public long SectionHeadersOffset { get; }

            /// <summary>
            /// 获取 CLR 文件头的相对虚拟地址。
            /// </summary>
            public uint CliHeaderRva { get; }
        }
    }
}