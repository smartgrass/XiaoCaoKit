using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorAssetsTools
{
    /// <summary>
    /// 纹理源文件读写、备份恢复与按百分比重采样。
    /// 只处理 png / jpg / tga：像素直接从源文件字节解码，不经过 importer，
    /// 因此不会带进平台压缩格式或 maxTextureSize 的影响。
    /// </summary>
    public static class TextureFileCodec
    {
        /// <summary>缩放后的最小边长，避免算出 0</summary>
        public const int MinSide = 4;

        /// <summary>JPG 写回质量</summary>
        private const int JpgQuality = 92;

        /// <summary>是否支持重新写回的文件格式</summary>
        public static bool IsSupported(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga";
        }

        /// <summary>
        /// 只读文件头解析源图尺寸，不加载像素。
        /// importer 的 width/height 会被 maxTextureSize 截断，这里读的是源文件真实尺寸。
        /// </summary>
        public static bool TryReadInfo(string filePath, out int width, out int height, out bool hasAlpha)
        {
            width = 0;
            height = 0;
            hasAlpha = false;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".tga")
                {
                    byte[] header = new byte[18];
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        if (stream.Read(header, 0, 18) < 18)
                        {
                            return false;
                        }
                    }

                    width = header[12] | (header[13] << 8);
                    height = header[14] | (header[15] << 8);
                    int depth = header[16];
                    int descriptor = header[17];
                    hasAlpha = depth == 32 || (depth == 16 && (descriptor & 0x0F) > 0);
                    return width > 0 && height > 0;
                }

                byte[] bytes = File.ReadAllBytes(filePath);
                if (ext == ".png")
                {
                    return TryReadPngInfo(bytes, out width, out height, out hasAlpha);
                }

                if (ext == ".jpg" || ext == ".jpeg")
                {
                    return TryReadJpegInfo(bytes, out width, out height);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("读取图片信息失败: {0}\n{1}", filePath, e.Message));
            }

            return false;
        }

        /// <summary>解析 PNG 的 IHDR 块</summary>
        private static bool TryReadPngInfo(byte[] bytes, out int width, out int height, out bool hasAlpha)
        {
            width = 0;
            height = 0;
            hasAlpha = false;

            // 8 字节签名 + 4 字节长度 + 4 字节类型 = 16，之后依次是宽、高、位深、颜色类型
            if (bytes.Length < 26 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47)
            {
                return false;
            }

            width = ReadInt32Be(bytes, 16);
            height = ReadInt32Be(bytes, 20);
            byte colorType = bytes[25];
            hasAlpha = colorType == 4 || colorType == 6;
            return width > 0 && height > 0;
        }

        /// <summary>扫描 JPEG 的 SOFn 段取尺寸</summary>
        private static bool TryReadJpegInfo(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            {
                return false;
            }

            int pos = 2;
            while (pos + 9 < bytes.Length)
            {
                if (bytes[pos] != 0xFF)
                {
                    pos++;
                    continue;
                }

                byte marker = bytes[pos + 1];
                // 无长度的独立标记
                if (marker == 0xFF)
                {
                    pos++;
                    continue;
                }

                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD8))
                {
                    pos += 2;
                    continue;
                }

                int segmentLength = (bytes[pos + 2] << 8) | bytes[pos + 3];
                bool isStartOfFrame = marker >= 0xC0 && marker <= 0xCF &&
                                      marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                if (isStartOfFrame)
                {
                    height = (bytes[pos + 5] << 8) | bytes[pos + 6];
                    width = (bytes[pos + 7] << 8) | bytes[pos + 8];
                    return width > 0 && height > 0;
                }

                pos += 2 + segmentLength;
            }

            return false;
        }

        /// <summary>
        /// 按百分比缩小源文件并以原格式写回。
        /// 返回 false 时 error 说明原因，文件不会被改动。
        /// </summary>
        public static bool TryResize(string filePath, int percent, out int newWidth, out int newHeight, out string error)
        {
            newWidth = 0;
            newHeight = 0;
            error = null;

            if (!IsSupported(filePath))
            {
                error = "不支持的格式（仅支持 png / jpg / tga）";
                return false;
            }

            Rgba32[] pixels;
            int width;
            int height;
            bool hasAlpha;
            if (!TryLoadPixels(filePath, out pixels, out width, out height, out hasAlpha, out error))
            {
                return false;
            }

            float scale = Mathf.Clamp(percent, 1, 100) / 100f;
            int targetWidth = Mathf.Max(MinSide, Mathf.RoundToInt(width * scale));
            int targetHeight = Mathf.Max(MinSide, Mathf.RoundToInt(height * scale));
            if (targetWidth >= width && targetHeight >= height)
            {
                error = "目标尺寸不小于原尺寸，无需处理";
                return false;
            }

            Rgba32[] resized = ResizeBox(pixels, width, height, targetWidth, targetHeight);

            byte[] encoded;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".tga")
            {
                encoded = EncodeTga(resized, targetWidth, targetHeight, hasAlpha);
            }
            else if (ext == ".png")
            {
                TgaImage image = new TgaImage
                {
                    Width = targetWidth,
                    Height = targetHeight,
                    Pixels = resized,
                    HasAlpha = hasAlpha
                };
                encoded = PngEncoder.Encode(image, hasAlpha);
            }
            else
            {
                encoded = EncodeJpg(resized, targetWidth, targetHeight);
            }

            File.WriteAllBytes(filePath, encoded);
            newWidth = targetWidth;
            newHeight = targetHeight;
            return true;
        }

        /// <summary>从源文件解码出 RGBA 像素，行序统一为首行在顶部</summary>
        private static bool TryLoadPixels(string filePath, out Rgba32[] pixels, out int width, out int height,
            out bool hasAlpha, out string error)
        {
            pixels = null;
            width = 0;
            height = 0;
            hasAlpha = false;
            error = null;

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            try
            {
                if (ext == ".tga")
                {
                    TgaImage image = TgaDecoder.Decode(File.ReadAllBytes(filePath));
                    pixels = image.Pixels;
                    width = image.Width;
                    height = image.Height;
                    hasAlpha = image.HasAlpha;
                    return true;
                }

                // PNG / JPG 交给 Unity 解码：linear 纹理保证 GetPixels32 返回文件里的原始字节
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!texture.LoadImage(File.ReadAllBytes(filePath), false))
                    {
                        error = "图片解码失败";
                        return false;
                    }

                    width = texture.width;
                    height = texture.height;
                    Color32[] colors = texture.GetPixels32();
                    pixels = FlipVertical(ToRgba32(colors), width, height);
                    hasAlpha = ext == ".png" && ReadPngHasAlpha(filePath);
                    return true;
                }
                finally
                {
                    SafeDestroy(texture);
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>单独读一次 PNG 头部判断是否带 alpha（GetPixels32 的 alpha 恒为解析后的值，无法区分原图是否启用）</summary>
        private static bool ReadPngHasAlpha(string filePath)
        {
            int width;
            int height;
            bool hasAlpha;
            return TryReadInfo(filePath, out width, out height, out hasAlpha) && hasAlpha;
        }

        /// <summary>
        /// 面积加权盒式重采样：每个目标像素取其覆盖的源像素区域均值。
        /// RGB 按 alpha 加权平均，避免透明边缘出现黑边。
        /// </summary>
        private static Rgba32[] ResizeBox(Rgba32[] source, int sourceWidth, int sourceHeight, int targetWidth,
            int targetHeight)
        {
            Rgba32[] target = new Rgba32[targetWidth * targetHeight];
            float scaleX = (float)sourceWidth / targetWidth;
            float scaleY = (float)sourceHeight / targetHeight;

            for (int dy = 0; dy < targetHeight; dy++)
            {
                float sy0 = dy * scaleY;
                float sy1 = sy0 + scaleY;
                int iy0 = Mathf.FloorToInt(sy0);
                int iy1 = Mathf.Min(sourceHeight, Mathf.CeilToInt(sy1));

                for (int dx = 0; dx < targetWidth; dx++)
                {
                    float sx0 = dx * scaleX;
                    float sx1 = sx0 + scaleX;
                    int ix0 = Mathf.FloorToInt(sx0);
                    int ix1 = Mathf.Min(sourceWidth, Mathf.CeilToInt(sx1));

                    float r = 0f;
                    float g = 0f;
                    float b = 0f;
                    float a = 0f;
                    float alphaWeight = 0f;
                    float weightSum = 0f;

                    for (int y = iy0; y < iy1; y++)
                    {
                        float wy = Mathf.Min(sy1, y + 1) - Mathf.Max(sy0, y);
                        if (wy <= 0f)
                        {
                            continue;
                        }

                        int row = y * sourceWidth;
                        for (int x = ix0; x < ix1; x++)
                        {
                            float wx = Mathf.Min(sx1, x + 1) - Mathf.Max(sx0, x);
                            if (wx <= 0f)
                            {
                                continue;
                            }

                            float weight = wx * wy;
                            Rgba32 pixel = source[row + x];
                            float alpha = pixel.A / 255f * weight;
                            r += pixel.R * alpha;
                            g += pixel.G * alpha;
                            b += pixel.B * alpha;
                            a += pixel.A * weight;
                            alphaWeight += alpha;
                            weightSum += weight;
                        }
                    }

                    byte outR = 0;
                    byte outG = 0;
                    byte outB = 0;
                    byte outA = 0;
                    if (weightSum > 0f)
                    {
                        if (alphaWeight > 0f)
                        {
                            outR = ToByte(r / alphaWeight);
                            outG = ToByte(g / alphaWeight);
                            outB = ToByte(b / alphaWeight);
                        }

                        outA = ToByte(a / weightSum);
                    }

                    target[dy * targetWidth + dx] = new Rgba32(outR, outG, outB, outA);
                }
            }

            return target;
        }

        /// <summary>写入未压缩 TGA（来源多为 24/32 位真彩，无 RLE，尺寸减半后体积必然下降）</summary>
        private static byte[] EncodeTga(Rgba32[] pixels, int width, int height, bool withAlpha)
        {
            int bytesPerPixel = withAlpha ? 4 : 3;
            byte[] data = new byte[18 + width * height * bytesPerPixel];
            data[2] = 2; // 图像类型：未压缩真彩
            WriteUInt16(data, 12, width);
            WriteUInt16(data, 14, height);
            data[16] = (byte)(bytesPerPixel * 8);
            // bit5 置 1 表示原点在左上，与我们的首行在顶部一致；低 4 位声明 alpha 位数
            data[17] = (byte)(withAlpha ? 0x28 : 0x20);

            int pos = 18;
            for (int i = 0; i < pixels.Length; i++)
            {
                Rgba32 pixel = pixels[i];
                data[pos++] = pixel.B;
                data[pos++] = pixel.G;
                data[pos++] = pixel.R;
                if (withAlpha)
                {
                    data[pos++] = pixel.A;
                }
            }

            return data;
        }

        /// <summary>JPG 无 alpha，写回时统一按不透明编码</summary>
        private static byte[] EncodeJpg(Rgba32[] pixels, int width, int height)
        {
            Color32[] colors = new Color32[pixels.Length];
            // EncodeToJPG 要求 bottom-up 行序，这里把首行在顶部的数据翻回去
            for (int y = 0; y < height; y++)
            {
                int sourceRow = (height - 1 - y) * width;
                int targetRow = y * width;
                for (int x = 0; x < width; x++)
                {
                    Rgba32 pixel = pixels[sourceRow + x];
                    colors[targetRow + x] = new Color32(pixel.R, pixel.G, pixel.B, 255);
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(colors);
                texture.Apply(false, false);
                return texture.EncodeToJPG(JpgQuality);
            }
            finally
            {
                SafeDestroy(texture);
            }
        }

        /// <summary>备份路径：保持 Assets 下的相对结构，便于回拷</summary>
        public static string GetBackupPath(string assetPath, string backupRoot)
        {
            string relative = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? assetPath.Substring("Assets/".Length)
                : assetPath;
            return Path.Combine(backupRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// 把源文件复制到备份目录。已存在同名备份时默认不覆盖，
        /// 保证二次压缩不会用「已缩小的图」覆盖真正的原图。
        /// </summary>
        public static bool Backup(string assetPath, string backupRoot, bool overwrite, out string error)
        {
            error = null;
            try
            {
                string source = Path.GetFullPath(assetPath);
                string target = GetBackupPath(assetPath, backupRoot);
                if (File.Exists(target) && !overwrite)
                {
                    return true;
                }

                string folder = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.Copy(source, target, true);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>是否已有备份文件</summary>
        public static bool HasBackup(string assetPath, string backupRoot)
        {
            return File.Exists(GetBackupPath(assetPath, backupRoot));
        }

        /// <summary>用备份覆盖回源文件</summary>
        public static bool Restore(string assetPath, string backupRoot, out string error)
        {
            error = null;
            try
            {
                string source = GetBackupPath(assetPath, backupRoot);
                if (!File.Exists(source))
                {
                    error = "备份不存在";
                    return false;
                }

                File.Copy(source, Path.GetFullPath(assetPath), true);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>把 Unity 的 bottom-up 像素翻成首行在顶部</summary>
        private static Rgba32[] FlipVertical(Rgba32[] pixels, int width, int height)
        {
            Rgba32[] flipped = new Rgba32[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
            }

            return flipped;
        }

        private static Rgba32[] ToRgba32(Color32[] colors)
        {
            Rgba32[] pixels = new Rgba32[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Color32 color = colors[i];
                pixels[i] = new Rgba32(color.r, color.g, color.b, color.a);
            }

            return pixels;
        }

        private static void SafeDestroy(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static byte ToByte(float value)
        {
            if (value <= 0f)
            {
                return 0;
            }

            return value >= 255f ? (byte)255 : (byte)(value + 0.5f);
        }

        private static int ReadInt32Be(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        private static void WriteUInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xFF);
            data[offset + 1] = (byte)((value >> 8) & 0xFF);
        }
    }
}
