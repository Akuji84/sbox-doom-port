using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ManagedDoom
{
    public static class PngPatchLoader
    {
        public static Patch Load(string name, byte[] pngBytes, byte[] playpal, int targetHeight = 16)
        {
            var (width, height, rgba) = DecodePng(pngBytes);
            var targetWidth = (int)Math.Round((double)width / height * targetHeight);
            if (targetWidth < 1) targetWidth = 1;
            var scaled = ScaleRgba(rgba, width, height, targetWidth, targetHeight);
            return BuildPatch(name, targetWidth, targetHeight, scaled, playpal);
        }

        private static Patch BuildPatch(string name, int w, int h, byte[] rgba, byte[] playpal)
        {
            var columns = new Column[w][];

            for (var x = 0; x < w; x++)
            {
                var cols = new List<Column>();
                var y = 0;

                while (y < h)
                {
                    while (y < h && rgba[(y * w + x) * 4 + 3] < 128)
                        y++;

                    if (y >= h) break;

                    var spanStart = y;
                    var spanPixels = new List<byte>();

                    while (y < h && rgba[(y * w + x) * 4 + 3] >= 128)
                    {
                        var idx = (y * w + x) * 4;
                        spanPixels.Add(FindNearestColor(rgba[idx], rgba[idx + 1], rgba[idx + 2], playpal));
                        y++;
                    }

                    var data = spanPixels.ToArray();
                    cols.Add(new Column(spanStart, data, 0, data.Length));
                }

                columns[x] = cols.ToArray();
            }

            return new Patch(name, w, h, 0, 0, columns);
        }

        private static byte FindNearestColor(byte r, byte g, byte b, byte[] playpal)
        {
            var bestIdx = 0;
            var bestDist = int.MaxValue;

            for (var i = 0; i < 256; i++)
            {
                var pr = playpal[i * 3];
                var pg = playpal[i * 3 + 1];
                var pb = playpal[i * 3 + 2];
                var dr = r - pr;
                var dg = g - pg;
                var db = b - pb;
                var dist = dr * dr + dg * dg + db * db;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return (byte)bestIdx;
        }

        private static byte[] ScaleRgba(byte[] src, int srcW, int srcH, int dstW, int dstH)
        {
            var dst = new byte[dstW * dstH * 4];

            for (var y = 0; y < dstH; y++)
            {
                var sy = Math.Min(y * srcH / dstH, srcH - 1);
                for (var x = 0; x < dstW; x++)
                {
                    var sx = Math.Min(x * srcW / dstW, srcW - 1);
                    var srcIdx = (sy * srcW + sx) * 4;
                    var dstIdx = (y * dstW + x) * 4;
                    dst[dstIdx] = src[srcIdx];
                    dst[dstIdx + 1] = src[srcIdx + 1];
                    dst[dstIdx + 2] = src[srcIdx + 2];
                    dst[dstIdx + 3] = src[srcIdx + 3];
                }
            }

            return dst;
        }

        private static (int width, int height, byte[] rgba) DecodePng(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            br.ReadBytes(8); // PNG signature

            var width = 0;
            var height = 0;
            var bitDepth = 0;
            var colorType = 0;
            var idatBytes = new List<byte>();

            while (ms.Position < ms.Length)
            {
                var chunkLen = ReadBigEndianInt32(br);
                var typeBytes = br.ReadBytes(4);
                var chunkType = System.Text.Encoding.ASCII.GetString(typeBytes);

                if (chunkType == "IHDR")
                {
                    width = ReadBigEndianInt32(br);
                    height = ReadBigEndianInt32(br);
                    bitDepth = br.ReadByte();
                    colorType = br.ReadByte();
                    br.ReadBytes(3); // compression, filter, interlace
                    br.ReadBytes(4); // CRC
                }
                else if (chunkType == "IDAT")
                {
                    var chunk = br.ReadBytes(chunkLen);
                    idatBytes.AddRange(chunk);
                    br.ReadBytes(4); // CRC
                }
                else if (chunkType == "IEND")
                {
                    break;
                }
                else
                {
                    br.ReadBytes(chunkLen + 4); // skip data + CRC
                }
            }

            // Decompress zlib data (skip 2-byte zlib header)
            var compressed = idatBytes.ToArray();
            byte[] decompressed;
            using (var cms = new MemoryStream(compressed, 2, compressed.Length - 2))
            using (var ds = new DeflateStream(cms, CompressionMode.Decompress))
            using (var oms = new MemoryStream())
            {
                ds.CopyTo(oms);
                decompressed = oms.ToArray();
            }

            var bpp = colorType == 6 ? 4 : 3;
            var stride = width * bpp;
            var pixels = new byte[height * stride];
            var srcPos = 0;

            for (var y = 0; y < height; y++)
            {
                var filterType = decompressed[srcPos++];
                var rowStart = y * stride;
                var prevRowStart = (y - 1) * stride;

                for (var x = 0; x < stride; x++)
                {
                    var raw = decompressed[srcPos++];
                    var a = (x >= bpp) ? pixels[rowStart + x - bpp] : (byte)0;
                    var b = (y > 0) ? pixels[prevRowStart + x] : (byte)0;
                    var c = (x >= bpp && y > 0) ? pixels[prevRowStart + x - bpp] : (byte)0;

                    switch (filterType)
                    {
                        case 0: pixels[rowStart + x] = raw; break;
                        case 1: pixels[rowStart + x] = (byte)(raw + a); break;
                        case 2: pixels[rowStart + x] = (byte)(raw + b); break;
                        case 3: pixels[rowStart + x] = (byte)(raw + (a + b) / 2); break;
                        case 4: pixels[rowStart + x] = (byte)(raw + PaethPredictor(a, b, c)); break;
                    }
                }
            }

            // Convert to RGBA if RGB
            if (colorType == 6)
            {
                return (width, height, pixels);
            }

            var rgba = new byte[width * height * 4];
            for (var i = 0; i < width * height; i++)
            {
                rgba[i * 4] = pixels[i * 3];
                rgba[i * 4 + 1] = pixels[i * 3 + 1];
                rgba[i * 4 + 2] = pixels[i * 3 + 2];
                rgba[i * 4 + 3] = 255;
            }

            return (width, height, rgba);
        }

        private static int ReadBigEndianInt32(BinaryReader br)
        {
            var bytes = br.ReadBytes(4);
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }

        private static byte PaethPredictor(byte a, byte b, byte c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }
    }
}
