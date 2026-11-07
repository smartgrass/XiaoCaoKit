using System;

namespace EditorAssetsTools
{
    /// <summary>
    /// 8 位/通道的 RGBA 像素
    /// </summary>
    public readonly struct Rgba32
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public Rgba32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    /// <summary>
    /// TGA 解码结果
    /// </summary>
    public sealed class TgaImage
    {
        /// <summary>图片宽度</summary>
        public int Width;

        /// <summary>图片高度</summary>
        public int Height;

        /// <summary>像素数据，行序自上而下、每行自左向右，与 Unity 导入后的方向一致</summary>
        public Rgba32[] Pixels;

        /// <summary>源图是否带有 Alpha 通道</summary>
        public bool HasAlpha;
    }

    /// <summary>
    /// TGA 解码器：不依赖 Unity，纯字节解析，保证输出与 Unity 导入结果一致
    /// 支持 8/16/24/32 位深度、调色板、RLE 压缩，并自动处理 TGA 的原点方向标记
    /// 注：16 位图的 alpha 位在各实现中解释不一致（本实现按 1=不透明），该规格实际极少遇到
    /// </summary>
    public static class TgaDecoder
    {
        /// <summary>
        /// 解析 TGA 字节数据
        /// </summary>
        public static TgaImage Decode(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < 18)
            {
                throw new ArgumentException("TGA 数据长度不足，无法解析文件头");
            }

            int idLength = data[0];
            int colorMapType = data[1];
            int imageType = data[2];
            int colorMapOrigin = Read16(data, 3);
            int colorMapLength = Read16(data, 5);
            int colorMapDepth = data[7];
            int width = Read16(data, 12);
            int height = Read16(data, 14);
            int pixelDepth = data[16];
            int descriptor = data[17];

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException(string.Format("TGA 尺寸非法: {0}x{1}", width, height));
            }

            // 描述符 bit5：0=原点在左下（行自下而上），1=原点在左上；bit4：1=行内像素自右向左
            bool topDown = (descriptor & 0x20) != 0;
            bool rightToLeft = (descriptor & 0x10) != 0;
            int alphaBits = descriptor & 0x0F;

            // 8~11 为 0~3 的 RLE 版本；32/33 等霍夫曼/四叉树类型不支持
            bool rle = imageType >= 8 && imageType <= 11;
            int baseType = rle ? imageType - 8 : imageType;
            if (baseType < 1 || baseType > 3)
            {
                throw new NotSupportedException(string.Format("不支持的 TGA 图像类型: {0}", imageType));
            }

            int pos = 18 + idLength;

            // 调色板（真彩图为 0，调色板图为 1）
            Rgba32[] palette = null;
            if (colorMapType != 0 && colorMapLength > 0)
            {
                palette = ReadPalette(data, ref pos, colorMapOrigin, colorMapLength, colorMapDepth);
            }

            if (baseType == 1 && palette == null)
            {
                throw new ArgumentException("调色板型 TGA 缺少调色板数据");
            }

            int total = width * height;
            Rgba32[] stored = new Rgba32[total];

            if (rle)
            {
                ReadRlePixels(data, ref pos, stored, total, pixelDepth, palette, alphaBits);
            }
            else
            {
                for (int i = 0; i < total; i++)
                {
                    stored[i] = ReadPixel(data, ref pos, pixelDepth, palette, alphaBits);
                }
            }

            // 按方向标记整理为 Unity 的行序（首行为顶部）
            Rgba32[] pixels = new Rgba32[total];
            for (int y = 0; y < height; y++)
            {
                int srcRow = topDown ? y : height - 1 - y;
                int srcBase = srcRow * width;
                int dstBase = y * width;
                for (int x = 0; x < width; x++)
                {
                    int srcX = rightToLeft ? width - 1 - x : x;
                    pixels[dstBase + x] = stored[srcBase + srcX];
                }
            }

            return new TgaImage
            {
                Width = width,
                Height = height,
                Pixels = pixels,
                HasAlpha = pixelDepth == 32 || (pixelDepth == 16 && alphaBits > 0)
            };
        }

        /// <summary>读取小端 16 位无符号整数</summary>
        private static int Read16(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        /// <summary>5 位通道扩展到 8 位</summary>
        private static byte Scale5To8(int value)
        {
            return (byte)((value << 3) | (value >> 2));
        }

        /// <summary>读取调色板数据（TGA 调色板按 BGR(A) 存储）</summary>
        private static Rgba32[] ReadPalette(byte[] data, ref int pos, int origin, int length, int depth)
        {
            Rgba32[] palette = new Rgba32[origin + length];
            for (int i = origin; i < palette.Length; i++)
            {
                switch (depth)
                {
                    case 8:
                        {
                            byte v = data[pos++];
                            palette[i] = new Rgba32(v, v, v, 255);
                            break;
                        }
                    case 15:
                    case 16:
                        {
                            int c = data[pos] | (data[pos + 1] << 8);
                            pos += 2;
                            palette[i] = new Rgba32(
                                Scale5To8((c >> 10) & 0x1F),
                                Scale5To8((c >> 5) & 0x1F),
                                Scale5To8(c & 0x1F),
                                255);
                            break;
                        }
                    case 24:
                        {
                            palette[i] = new Rgba32(data[pos + 2], data[pos + 1], data[pos], 255);
                            pos += 3;
                            break;
                        }
                    default:
                        {
                            palette[i] = new Rgba32(data[pos + 2], data[pos + 1], data[pos], data[pos + 3]);
                            pos += 4;
                            break;
                        }
                }
            }

            return palette;
        }

        /// <summary>读取 RLE 压缩的像素数据</summary>
        private static void ReadRlePixels(byte[] data, ref int pos, Rgba32[] stored, int total, int pixelDepth,
            Rgba32[] palette, int alphaBits)
        {
            int i = 0;
            while (i < total)
            {
                byte header = data[pos++];
                int count = (header & 0x7F) + 1;

                if ((header & 0x80) != 0)
                {
                    // 重复包：后接 1 个像素，重复 count 次
                    Rgba32 pixel = ReadPixel(data, ref pos, pixelDepth, palette, alphaBits);
                    for (int k = 0; k < count && i < total; k++)
                    {
                        stored[i++] = pixel;
                    }
                }
                else
                {
                    // 原始包：后接 count 个独立像素
                    for (int k = 0; k < count && i < total; k++)
                    {
                        stored[i++] = ReadPixel(data, ref pos, pixelDepth, palette, alphaBits);
                    }
                }
            }
        }

        /// <summary>读取单个像素（TGA 通道顺序为 B、G、R、A）</summary>
        private static Rgba32 ReadPixel(byte[] data, ref int pos, int pixelDepth, Rgba32[] palette, int alphaBits)
        {
            switch (pixelDepth)
            {
                case 8:
                    {
                        byte index = data[pos++];
                        if (palette != null)
                        {
                            return index < palette.Length ? palette[index] : new Rgba32(index, index, index, 255);
                        }

                        return new Rgba32(index, index, index, 255);
                    }
                case 15:
                case 16:
                    {
                        int c = data[pos] | (data[pos + 1] << 8);
                        pos += 2;
                        if (palette != null)
                        {
                            return c < palette.Length ? palette[c] : default(Rgba32);
                        }

                        // 16 位图的最高位：描述符声明有 alpha 时按 alpha 处理（1=不透明），否则视为无效位并置为不透明
                        byte a = alphaBits > 0 ? (byte)((c & 0x8000) != 0 ? 255 : 0) : (byte)255;
                        return new Rgba32(
                            Scale5To8((c >> 10) & 0x1F),
                            Scale5To8((c >> 5) & 0x1F),
                            Scale5To8(c & 0x1F),
                            a);
                    }
                case 24:
                    {
                        byte b = data[pos];
                        byte g = data[pos + 1];
                        byte r = data[pos + 2];
                        pos += 3;
                        return new Rgba32(r, g, b, 255);
                    }
                case 32:
                    {
                        byte b = data[pos];
                        byte g = data[pos + 1];
                        byte r = data[pos + 2];
                        byte a = data[pos + 3];
                        pos += 4;
                        return new Rgba32(r, g, b, a);
                    }
                default:
                    throw new NotSupportedException(string.Format("不支持的 TGA 位深度: {0}", pixelDepth));
            }
        }
    }
}
