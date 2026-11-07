using System;
using System.IO;
using System.IO.Compression;

namespace EditorAssetsTools
{
    /// <summary>
    /// PNG 编码器：纯 C# 实现，直接写入源像素值（不做任何色彩空间转换），
    /// 保证输出的 PNG 与 Unity Inspector 预览看到的画面一致
    /// </summary>
    public static class PngEncoder
    {
        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly uint[] CrcTable = BuildCrcTable();

        /// <summary>
        /// 将解码后的图像编码为 PNG 字节
        /// </summary>
        /// <param name="image">TGA 解码结果</param>
        /// <param name="keepAlpha">是否保留 Alpha 通道；false 时输出 RGB（与 Unity 预览一致）</param>
        public static byte[] Encode(TgaImage image, bool keepAlpha)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            int width = image.Width;
            int height = image.Height;
            int channels = keepAlpha ? 4 : 3;
            byte colorType = keepAlpha ? (byte)6 : (byte)2;

            // 每行前置 1 字节过滤类型，统一使用 Paeth 过滤以获得较好的压缩率
            int stride = width * channels + 1;
            byte[] raw = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int dst = y * stride;
                raw[dst++] = 4;
                for (int x = 0; x < width; x++)
                {
                    Rgba32 pixel = image.Pixels[y * width + x];
                    for (int c = 0; c < channels; c++)
                    {
                        byte value = c == 0 ? pixel.R : c == 1 ? pixel.G : c == 2 ? pixel.B : pixel.A;
                        byte left = x > 0 ? GetChannel(image.Pixels[y * width + x - 1], c) : (byte)0;
                        byte up = y > 0 ? GetChannel(image.Pixels[(y - 1) * width + x], c) : (byte)0;
                        byte upLeft = x > 0 && y > 0
                            ? GetChannel(image.Pixels[(y - 1) * width + x - 1], c)
                            : (byte)0;
                        raw[dst++] = unchecked((byte)(value - PaethPredictor(left, up, upLeft)));
                    }
                }
            }

            byte[] idat = BuildZlibStream(raw);

            using (MemoryStream png = new MemoryStream())
            {
                png.Write(PngSignature, 0, PngSignature.Length);
                WriteChunk(png, "IHDR", BuildIHDR(width, height, colorType));
                WriteChunk(png, "IDAT", idat);
                WriteChunk(png, "IEND", new byte[0]);
                return png.ToArray();
            }
        }

        /// <summary>取指定通道的值</summary>
        private static byte GetChannel(Rgba32 pixel, int channel)
        {
            switch (channel)
            {
                case 0: return pixel.R;
                case 1: return pixel.G;
                case 2: return pixel.B;
                default: return pixel.A;
            }
        }

        /// <summary>Paeth 预测器</summary>
        private static byte PaethPredictor(byte left, byte up, byte upLeft)
        {
            int p = left + up - upLeft;
            int pa = Math.Abs(p - left);
            int pb = Math.Abs(p - up);
            int pc = Math.Abs(p - upLeft);
            if (pa <= pb && pa <= pc)
            {
                return left;
            }

            return pb <= pc ? up : upLeft;
        }

        /// <summary>构造 IHDR 数据块</summary>
        private static byte[] BuildIHDR(int width, int height, byte colorType)
        {
            byte[] ihdr = new byte[13];
            WriteInt32(ihdr, 0, width);
            WriteInt32(ihdr, 4, height);
            ihdr[8] = 8;          // 位深度
            ihdr[9] = colorType;  // 颜色类型
            return ihdr;
        }

        /// <summary>将像素数据打包为 zlib 流（DeflateStream 输出裸 deflate，需自行补头部与 Adler32）</summary>
        private static byte[] BuildZlibStream(byte[] raw)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                stream.WriteByte(0x78);
                stream.WriteByte(0x9C);
                using (DeflateStream deflate = new DeflateStream(stream, CompressionLevel.Optimal, true))
                {
                    deflate.Write(raw, 0, raw.Length);
                }

                uint checksum = Adler32(raw);
                stream.WriteByte((byte)((checksum >> 24) & 0xFF));
                stream.WriteByte((byte)((checksum >> 16) & 0xFF));
                stream.WriteByte((byte)((checksum >> 8) & 0xFF));
                stream.WriteByte((byte)(checksum & 0xFF));
                return stream.ToArray();
            }
        }

        /// <summary>计算 Adler32 校验值</summary>
        private static uint Adler32(byte[] data)
        {
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % 65521;
                b = (b + a) % 65521;
            }

            return (b << 16) | a;
        }

        /// <summary>写入一个 PNG 数据块（长度 + 类型 + 数据 + CRC）</summary>
        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            byte[] typeBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                typeBytes[i] = (byte)type[i];
            }

            byte[] length = new byte[4];
            WriteInt32(length, 0, data.Length);
            stream.Write(length, 0, 4);
            stream.Write(typeBytes, 0, 4);
            stream.Write(data, 0, data.Length);

            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < 4; i++)
            {
                crc = CrcTable[(crc ^ typeBytes[i]) & 0xFF] ^ (crc >> 8);
            }

            for (int i = 0; i < data.Length; i++)
            {
                crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }

            crc ^= 0xFFFFFFFF;
            byte[] crcBytes = new byte[4];
            WriteInt32(crcBytes, 0, unchecked((int)crc));
            stream.Write(crcBytes, 0, 4);
        }

        /// <summary>生成 CRC32 查表</summary>
        private static uint[] BuildCrcTable()
        {
            uint[] table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }

        /// <summary>以大端序写入 32 位整数</summary>
        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }
    }
}
