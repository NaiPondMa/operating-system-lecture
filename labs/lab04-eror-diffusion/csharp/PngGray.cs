// A tiny PNG reader/writer, so the lab needs no NuGet package.
//
//   Read:  8-bit PNG, not interlaced, gray / gray+alpha / RGB / RGBA  -> 8-bit gray bytes
//   Write: 8-bit gray PNG
//
// A PNG file = 8-byte signature + chunks. Each chunk = length, type, data, CRC.
// The pixels are in IDAT chunks as a zlib stream. Every row starts with a "filter" byte
// that says how the row is stored relative to its neighbours (None, Sub, Up, Average, Paeth).
// Color is converted to gray with the same formula as Python's PIL (L = 0.299 R + 0.587 G + 0.114 B).

using System;
using System.IO;
using System.IO.Compression;

namespace ErrorDiffusionLab
{
    public static class PngGray
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        // ------------------------------------------------------------ read
        public static byte[] Read(string path, out int width, out int height)
        {
            byte[] file = File.ReadAllBytes(path);
            if (file.Length < 8)
                throw new InvalidDataException("Not a PNG file: " + path);
            for (int i = 0; i < 8; i++)
                if (file[i] != Signature[i]) throw new InvalidDataException("Not a PNG file: " + path);

            int w = 0, h = 0, channels = 0;
            MemoryStream idat = new MemoryStream();
            int pos = 8;
            while (pos + 8 <= file.Length)
            {
                int length = (int)ReadUInt32(file, pos);
                string type = "" + (char)file[pos + 4] + (char)file[pos + 5] + (char)file[pos + 6] + (char)file[pos + 7];
                int data = pos + 8;
                if (type == "IHDR")
                {
                    w = (int)ReadUInt32(file, data);
                    h = (int)ReadUInt32(file, data + 4);
                    int depth = file[data + 8], colorType = file[data + 9], interlace = file[data + 12];
                    if (depth != 8 || interlace != 0)
                        throw new NotSupportedException("Only 8-bit, non-interlaced PNG files are supported.");
                    if (colorType == 0) channels = 1;        // gray
                    else if (colorType == 4) channels = 2;   // gray + alpha
                    else if (colorType == 2) channels = 3;   // RGB
                    else if (colorType == 6) channels = 4;   // RGBA
                    else throw new NotSupportedException(
                        "PNG color type " + colorType + " (palette) is not supported. Convert it to grayscale first.");
                }
                else if (type == "IDAT")
                {
                    idat.Write(file, data, length);
                }
                else if (type == "IEND")
                {
                    break;
                }
                pos = data + length + 4;                      // skip the CRC
            }
            if (w <= 0 || h <= 0 || channels == 0)
                throw new InvalidDataException("PNG has no valid IHDR chunk: " + path);

            // IDAT is a zlib stream: 2 header bytes + deflate data + 4 checksum bytes.
            // DeflateStream reads the raw deflate part (it ignores the trailing checksum).
            int stride = w * channels;
            long total = (long)h * (stride + 1);
            if (total > int.MaxValue)
                throw new NotSupportedException("Picture is too large (more than 2 GB of pixel data).");
            byte[] raw = new byte[(int)total];
            idat.Position = 2;
            using (DeflateStream inflate = new DeflateStream(idat, CompressionMode.Decompress))
            {
                int got = 0;
                while (got < raw.Length)
                {
                    int n = inflate.Read(raw, got, raw.Length - got);
                    if (n <= 0) throw new InvalidDataException("PNG pixel data is too short.");
                    got += n;
                }
            }

            Unfilter(raw, w, h, channels);

            byte[] gray = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * (stride + 1) + 1;
                for (int x = 0; x < w; x++)
                {
                    int p = row + x * channels;
                    if (channels <= 2)
                        gray[y * w + x] = raw[p];
                    else
                        gray[y * w + x] = (byte)((raw[p] * 19595 + raw[p + 1] * 38470 + raw[p + 2] * 7471 + 0x8000) >> 16);
                }
            }
            width = w;
            height = h;
            return gray;
        }

        private static void Unfilter(byte[] raw, int w, int h, int bpp)
        {
            int stride = w * bpp;
            for (int y = 0; y < h; y++)
            {
                int row = y * (stride + 1);
                int filter = raw[row];
                int cur = row + 1;
                int up = y > 0 ? row - (stride + 1) + 1 : -1;
                for (int i = 0; i < stride; i++)
                {
                    int a = i >= bpp ? raw[cur + i - bpp] : 0;           // left
                    int b = up >= 0 ? raw[up + i] : 0;                   // above
                    int c = (up >= 0 && i >= bpp) ? raw[up + i - bpp] : 0; // above-left
                    int add;
                    switch (filter)
                    {
                        case 0: add = 0; break;
                        case 1: add = a; break;
                        case 2: add = b; break;
                        case 3: add = (a + b) / 2; break;
                        case 4: add = Paeth(a, b, c); break;
                        default: throw new InvalidDataException("Bad PNG filter type " + filter);
                    }
                    raw[cur + i] = (byte)(raw[cur + i] + add);
                }
            }
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            return pb <= pc ? b : c;
        }

        // ------------------------------------------------------------ write
        public static void Write(string path, byte[] gray, int width, int height)
        {
            // raw rows: filter byte 0 (None) + pixels
            byte[] raw = new byte[height * (width + 1)];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(gray, y * width, raw, y * (width + 1) + 1, width);

            // zlib stream = 0x78 0x9C + deflate data + Adler-32 of the raw bytes (big-endian)
            MemoryStream z = new MemoryStream();
            z.WriteByte(0x78);
            z.WriteByte(0x9C);
            using (DeflateStream deflate = new DeflateStream(z, CompressionLevel.Optimal, true))
                deflate.Write(raw, 0, raw.Length);
            WriteUInt32(z, Adler32(raw));

            using (FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                f.Write(Signature, 0, 8);

                byte[] ihdr = new byte[13];
                PutUInt32(ihdr, 0, (uint)width);
                PutUInt32(ihdr, 4, (uint)height);
                ihdr[8] = 8;   // bit depth
                ihdr[9] = 0;   // color type 0 = gray
                WriteChunk(f, "IHDR", ihdr, 13);

                byte[] zdata = z.ToArray();
                WriteChunk(f, "IDAT", zdata, zdata.Length);
                WriteChunk(f, "IEND", new byte[0], 0);
            }
        }

        private static void WriteChunk(Stream s, string type, byte[] data, int length)
        {
            byte[] head = new byte[8];
            PutUInt32(head, 0, (uint)length);
            for (int i = 0; i < 4; i++) head[4 + i] = (byte)type[i];
            s.Write(head, 0, 8);
            s.Write(data, 0, length);

            uint crc = 0xFFFFFFFF;
            crc = Crc(crc, head, 4, 4);
            crc = Crc(crc, data, 0, length);
            byte[] tail = new byte[4];
            PutUInt32(tail, 0, crc ^ 0xFFFFFFFF);
            s.Write(tail, 0, 4);
        }

        // ------------------------------------------------------------ helpers
        private static uint[] crcTable;

        private static uint Crc(uint crc, byte[] buf, int offset, int count)
        {
            if (crcTable == null)
            {
                uint[] t = new uint[256];
                for (uint n = 0; n < 256; n++)
                {
                    uint c = n;
                    for (int k = 0; k < 8; k++)
                        c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                    t[n] = c;
                }
                crcTable = t;
            }
            for (int i = offset; i < offset + count; i++)
                crc = crcTable[(crc ^ buf[i]) & 0xFF] ^ (crc >> 8);
            return crc;
        }

        private static uint Adler32(byte[] data)
        {
            uint a = 1, b = 0;
            int i = 0;
            while (i < data.Length)
            {
                int end = Math.Min(i + 5552, data.Length);   // 5552 keeps the sums from overflowing
                for (; i < end; i++)
                {
                    a += data[i];
                    b += a;
                }
                a %= 65521;
                b %= 65521;
            }
            return (b << 16) | a;
        }

        private static uint ReadUInt32(byte[] b, int pos)
        {
            return ((uint)b[pos] << 24) | ((uint)b[pos + 1] << 16) | ((uint)b[pos + 2] << 8) | b[pos + 3];
        }

        private static void PutUInt32(byte[] b, int pos, uint v)
        {
            b[pos] = (byte)(v >> 24);
            b[pos + 1] = (byte)(v >> 16);
            b[pos + 2] = (byte)(v >> 8);
            b[pos + 3] = (byte)v;
        }

        private static void WriteUInt32(Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }
    }
}
