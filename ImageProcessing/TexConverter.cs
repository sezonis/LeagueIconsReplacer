using System.Text;
using System.Runtime.InteropServices;
using ImageMagick;

namespace LeagueIconsReplacer.Processing {
    public static class TexConverter {

            private const string TexMagic = "TEX\0";
            private const uint DdsMagic = 0x20534444; // "DDS "

            // DDS Flags
            private const uint DDSD_MIPMAPCOUNT = 0x00020000;
            private const uint DDPF_ALPHAPIXELS = 0x00000001;
            private const uint DDPF_FOURCC = 0x00000004;
            private const uint DDPF_RGB = 0x00000040;

            // Enums from tex.h
            public enum TexFormat : byte {
                Etc1 = 0x1,
                Etc2Eac = 0x2,
                Etc2 = 0x3,
                Dxt1 = 0xA,
                Dxt5 = 0xC,
                Bgra8 = 0x14
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct TexHeader {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
                public byte[] Magic;
                public ushort ImageWidth;
                public ushort ImageHeight;
                public byte Unk1;
                public byte Format;
                public byte Unk2;
                public byte HasMipmaps;
            }

            // Minimal DDS Header definitions needed for parsing
            [StructLayout(LayoutKind.Sequential)]
            private struct DdsPixelFormat {
                public uint Size;
                public uint Flags;
                public uint FourCC;
                public uint RGBBitCount;
                public uint RBitMask;
                public uint GBitMask;
                public uint BBitMask;
                public uint ABitMask;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DdsHeader {
                public uint Size;
                public uint Flags;
                public uint Height;
                public uint Width;
                public uint PitchOrLinearSize;
                public uint Depth;
                public uint MipMapCount;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
                public uint[] Reserved1;
                public DdsPixelFormat PixelFormat;
                public uint Caps;
                public uint Caps2;
                public uint Caps3;
                public uint Caps4;
                public uint Reserved2;
            }

            /// <summary>
            /// Converts a MagickImage to the custom TEX format.
            /// </summary>
            /// <param name="image">The source image.</param>
            /// <param name="outputPath">The file path to write the .tex file to.</param>
            public static void SaveAsTex(this MagickImage image, string outputPath) {
                using var ddsStream = new MemoryStream();

                image.Write(ddsStream, MagickFormat.Dds);
                ddsStream.Position = 0;

                using var reader = new BinaryReader(ddsStream);

                uint magic = reader.ReadUInt32();
                if (magic != DdsMagic)
                    throw new InvalidDataException("Generated data is not a valid DDS file.");

                byte[] headerBytes = reader.ReadBytes(Marshal.SizeOf(typeof(DdsHeader)));
                DdsHeader ddsHeader = ByteArrayToStructure<DdsHeader>(headerBytes);

                TexHeader texHeader = new TexHeader {
                    Magic = Encoding.ASCII.GetBytes(TexMagic),
                    ImageWidth = (ushort)ddsHeader.Width,
                    ImageHeight = (ushort)ddsHeader.Height,
                    Unk1 = 1, // Always 1 per C code
                    Unk2 = 0,
                    HasMipmaps = 0
                };

                bool customRgbaFormat = false;
                int[] rgbaIndices = new int[4];

                string fourCC = FourCCToString(ddsHeader.PixelFormat.FourCC);

                if (fourCC == "DXT1") {
                    texHeader.Format = (byte)TexFormat.Dxt1;
                } else if (fourCC == "DXT5") {
                    texHeader.Format = (byte)TexFormat.Dxt5;
                } else if ((ddsHeader.PixelFormat.Flags & DDPF_RGB) == DDPF_RGB) {
                    texHeader.Format = (byte)TexFormat.Bgra8;

                    // Sanity checks for BGRA8
                    if (ddsHeader.PixelFormat.RGBBitCount != 32)
                        throw new InvalidDataException($"Error: RGBBitCount is {ddsHeader.PixelFormat.RGBBitCount}, expected 32.");

                    // Check masks
                    if (ddsHeader.PixelFormat.BBitMask != 0x000000ff ||
                        ddsHeader.PixelFormat.GBitMask != 0x0000ff00 ||
                        ddsHeader.PixelFormat.RBitMask != 0x00ff0000 ||
                        ddsHeader.PixelFormat.ABitMask != 0xff000000) {
                        customRgbaFormat = true;
                        rgbaIndices[0] = MaskToIndex(ddsHeader.PixelFormat.RBitMask);
                        rgbaIndices[1] = MaskToIndex(ddsHeader.PixelFormat.GBitMask);
                        rgbaIndices[2] = MaskToIndex(ddsHeader.PixelFormat.BBitMask);
                        rgbaIndices[3] = MaskToIndex(ddsHeader.PixelFormat.ABitMask);

                        foreach (int idx in rgbaIndices) {
                            if (idx == -1)
                                throw new InvalidDataException("Error: bitmask data invalid. Can't convert to BGRA output format.");
                        }
                    }
                } else {
                    throw new InvalidDataException("Error: dds file needs to be in either DXT1, DXT5 or uncompressed BGRA8 format!");
                }

                int blockSize = GetBlockSize(texHeader.Format);
                if (ddsHeader.Width % blockSize != 0 || ddsHeader.Height % blockSize != 0) {
                    throw new InvalidDataException($"Error: Cannot convert to tex when dimensions ({ddsHeader.Width}x{ddsHeader.Height}) aren't divisible by {blockSize}!");
                }

                
                uint actualMipCount = (ddsHeader.Flags & DDSD_MIPMAPCOUNT) != 0 ? ddsHeader.MipMapCount : 0;
                if (actualMipCount == 0) actualMipCount = 1;

                if (actualMipCount > 1) {
                    texHeader.HasMipmaps = 1;

                    int maxDim = Math.Max((int)ddsHeader.Width, (int)ddsHeader.Height);
                    uint expectedMips = 0;
                    while (maxDim > 0) { maxDim >>= 1; expectedMips++; }

                    if (actualMipCount != expectedMips) {
                        throw new InvalidDataException($"Error: DDS mipmap count mismatch; expected {expectedMips} mipmaps, got {actualMipCount}");
                    }
                    Console.WriteLine("Info: DDS file has mipmaps");
                }

                long dataOffset = 4 + ddsHeader.Size; // Magic + Header Size
                long dataSize = ddsStream.Length - dataOffset;

                ddsStream.Seek(dataOffset, SeekOrigin.Begin);
                byte[] dataBuffer = reader.ReadBytes((int)dataSize);

                if (customRgbaFormat) {
                    if (dataBuffer.Length % 4 != 0) throw new InvalidDataException("Buffer size alignment error.");
                    SwapToBgra(dataBuffer, rgbaIndices);
                }

                using var texFile = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(texFile);

                // Write Header
                byte[] texHeaderBytes = StructureToByteArray(texHeader);
                writer.Write(texHeaderBytes);

                Console.WriteLine($"Info: Converting {texHeader.ImageWidth}x{texHeader.ImageHeight} DDS to TEX file \"{outputPath}\".");

                if (texHeader.HasMipmaps == 1) {
                    int currentOffset = dataBuffer.Length;
                    Console.WriteLine($"Writing {actualMipCount} mipmaps to TEX file...");

                    int bytesPerBlock = GetBytesPerBlock(texHeader.Format);

                    for (int i = (int)actualMipCount - 1; i >= 0; i--) {
                        uint currentW = (uint)Math.Max(texHeader.ImageWidth >> i, 1);
                        uint currentH = (uint)Math.Max(texHeader.ImageHeight >> i, 1);

                        uint blockW = (uint)(currentW + blockSize - 1) / (uint)blockSize;
                        uint blockH = (uint)(currentH + blockSize - 1) / (uint)blockSize;

                        int currentImageSize = (int)(bytesPerBlock * blockW * blockH);

                        Console.WriteLine($"Writing mipmap {i} with size {currentW}x{currentH}");

                        currentOffset -= currentImageSize;

                        if (currentOffset < 0)
                            throw new EndOfStreamException($"Error when attempting to write mipmap {i}: Not enough data!");

                        writer.Write(dataBuffer, currentOffset, currentImageSize);
                    }
                } else {
                    writer.Write(dataBuffer);
                }
            }


            private static int GetBytesPerBlock(byte format) {
                return format switch {
                    (byte)TexFormat.Dxt1 => 8,
                    (byte)TexFormat.Dxt5 => 16,
                    (byte)TexFormat.Bgra8 => 4,
                    _ => throw new ArgumentException("Unknown format")
                };
            }

            private static int GetBlockSize(byte format) {
                return format switch {
                    (byte)TexFormat.Dxt1 => 4,
                    (byte)TexFormat.Dxt5 => 4,
                    (byte)TexFormat.Bgra8 => 1,
                    _ => throw new ArgumentException("Unknown format")
                };
            }

            private static int MaskToIndex(uint mask) {
                return mask switch {
                    0x000000ff => 0,
                    0x0000ff00 => 1,
                    0x00ff0000 => 2,
                    0xff000000 => 3,
                    _ => -1
                };
            }

            private static void SwapToBgra(byte[] data, int[] rgbaIndices) {
                int rIdx = rgbaIndices[0];
                int gIdx = rgbaIndices[1];
                int bIdx = rgbaIndices[2];
                int aIdx = rgbaIndices[3];

                for (int i = 0; i < data.Length; i += 4) {
                    byte r = data[i + rIdx];
                    byte g = data[i + gIdx];
                    byte b = data[i + bIdx];
                    byte a = data[i + aIdx];

                    // Write as BGRA (Little Endian uint32: B G R A)
                    // data[i] is lowest byte
                    data[i + 0] = b;
                    data[i + 1] = g;
                    data[i + 2] = r;
                    data[i + 3] = a;
                }
            }

            private static string FourCCToString(uint fourCC) {
                byte[] bytes = BitConverter.GetBytes(fourCC);
                return Encoding.ASCII.GetString(bytes).Trim('\0');
            }

            private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct {
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                } finally {
                    handle.Free();
                }
            }

            private static byte[] StructureToByteArray<T>(T obj) where T : struct {
                int len = Marshal.SizeOf(obj);
                byte[] arr = new byte[len];
                IntPtr ptr = Marshal.AllocHGlobal(len);
                try {
                    Marshal.StructureToPtr(obj, ptr, true);
                    Marshal.Copy(ptr, arr, 0, len);
                } finally {
                    Marshal.FreeHGlobal(ptr);
                }
                return arr;
            }
        }
}
