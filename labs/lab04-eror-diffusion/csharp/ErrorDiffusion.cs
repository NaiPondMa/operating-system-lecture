// Lab 4: Error diffusion (Floyd-Steinberg) in C#.
//
// Three versions, all giving EXACTLY the same picture as the Python reference:
//   Sequential  one thread, plain loops                       (baseline)
//   Pipeline    worker threads, rows follow each other        (task parallelism)
//   Wavefront   pixels with equal x + 2*y run in parallel     (data parallelism)
//
// This file does not use any image library: pictures are plain byte arrays
// (one byte per pixel, row after row), so it is easy to test.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ErrorDiffusionLab
{
    public static class ErrorDiffusion
    {
        // How far row y must stay behind row y-1 (in pixels).
        // 2, not 1: pixel (y-1, x+2) also adds error into (y, x+1), and two threads
        // must never add into the same cell at the same time.
        private const int Lag = 2;

        // ---------------------------------------------------------------
        // Memory layout used by Sequential and Pipeline
        //
        // F is a "padded" int array with row length S = w + 2:
        //   pixel (y, x) -> index y*S + x + 1
        // The extra column on each side and the extra row at the bottom swallow the
        // error that would fall outside the picture, so the loop needs no bounds checks.
        // Neighbours of index i:  right i+1 (7/16), below-left i+S-1 (3/16),
        //                         below i+S (5/16), below-right i+S+1 (1/16)
        //
        // Floor division by 16 is written as ">> 4". C#'s "/" rounds toward zero,
        // but the reference (Python //) rounds toward minus infinity, and ">>" does that.
        // ---------------------------------------------------------------
        private static int[] MakePadded(byte[] gray, int w, int h)
        {
            int s = w + 2;
            int[] f = new int[(h + 1) * s];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    f[y * s + x + 1] = gray[y * w + x];
            return f;
        }

        /// <summary>Quantise rows first, first+step, first+2*step, ... in increasing order.</summary>
        private static void ProcessRows(int[] f, byte[] o, int[] progress,
                                        int first, int step, int w, int h, int thr, int block)
        {
            int s = w + 2;
            for (int y = first; y < h; y += step)
            {
                int baseIndex = y * s + 1;
                int outBase = y * w;
                for (int xs = 0; xs < w; xs += block)
                {
                    int xe = Math.Min(xs + block, w);
                    if (y > 0)
                    {
                        // The last pixel of this block (xe-1) needs row y-1 finished up to pixel xe+1.
                        int need = Math.Min(xe + Lag, w);
                        // Busy-wait. The wait is usually a few microseconds, so sleeping
                        // (Thread.Sleep, or SpinWait.SpinOnce which falls back to Sleep(1))
                        // would cost far more than the work itself.
                        int spins = 0;
                        while (Volatile.Read(ref progress[y - 1]) < need)
                        {
                            Thread.SpinWait(20);
                            if (++spins % 100 == 0) Thread.Yield();  // let others run if cores are scarce
                        }
                    }
                    for (int x = xs; x < xe; x++)
                    {
                        int i = baseIndex + x;
                        int old = f[i];
                        int nw = old > thr ? 255 : 0;
                        o[outBase + x] = (byte)nw;
                        int err = old - nw;
                        if (err != 0)
                        {
                            f[i + 1] += (err * 7) >> 4;
                            f[i + s - 1] += (err * 3) >> 4;
                            f[i + s] += (err * 5) >> 4;
                            f[i + s + 1] += err >> 4;
                        }
                    }
                    // "Release": the writes above become visible before the new count does.
                    Volatile.Write(ref progress[y], xe);
                }
            }
        }

        /// <summary>One thread, no waiting. The baseline.</summary>
        public static byte[] Sequential(byte[] gray, int w, int h, int thr)
        {
            int[] f = MakePadded(gray, w, h);
            byte[] o = new byte[w * h];
            ProcessRows(f, o, new int[h], 0, 1, w, h, thr, w);
            return o;
        }

        /// <summary>
        /// Pipeline: worker k handles rows k, k+workers, k+2*workers, ...
        /// Each row has a progress counter (pixels finished). Row y waits until
        /// row y-1 is far enough ahead. No cycle of waiting is possible (row 0 waits for nobody).
        /// </summary>
        public static byte[] Pipeline(byte[] gray, int w, int h, int thr, int workers, int block)
        {
            if (workers < 1) workers = 1;
            if (block < 1) block = 1;
            int[] f = MakePadded(gray, w, h);
            byte[] o = new byte[w * h];
            int[] progress = new int[h];

            Thread[] threads = new Thread[workers];
            for (int k = 0; k < workers; k++)
            {
                int first = k;
                threads[k] = new Thread(() => ProcessRows(f, o, progress, first, workers, w, h, thr, block));
                threads[k].Start();
            }
            foreach (Thread t in threads) t.Join();
            return o;
        }

        /// <summary>
        /// Wavefront. Every feeder of pixel (y, x) has a smaller t = x + 2*y, so all
        /// pixels with the same t are independent.
        ///
        /// Here each pixel PULLS the error from its 4 feeders instead of pushing it to its
        /// neighbours. Then a pixel writes only its own cells, so threads never touch
        /// the same memory (a "push" version would have two pixels of one diagonal
        /// adding into the same cell).
        /// </summary>
        public static byte[] Wavefront(byte[] gray, int w, int h, int thr)
        {
            int s = w + 2;
            // e[(y+1)*s + x+1] = quantisation error of pixel (y, x). Row 0 and the two
            // side columns stay 0, which is the same as "no error from outside the picture".
            int[] e = new int[(h + 1) * s];
            byte[] o = new byte[w * h];

            for (int t = 0; t <= (w - 1) + 2 * (h - 1); t++)
            {
                int y0 = Math.Max(0, (t - w + 2) / 2);   // smallest y with x = t-2y <= w-1
                int y1 = Math.Min(h - 1, t / 2);         // largest  y with x = t-2y >= 0
                int count = y1 - y0 + 1;
                if (count <= 0) continue;

                int tt = t;
                Action<int> doRow = y =>
                {
                    int x = tt - 2 * y;
                    int i = (y + 1) * s + x + 1;
                    int old = gray[y * w + x]
                            + ((e[i - 1] * 7) >> 4)       // left
                            + ((e[i - s + 1] * 3) >> 4)   // upper right
                            + ((e[i - s] * 5) >> 4)       // upper
                            + (e[i - s - 1] >> 4);        // upper left
                    int nw = old > thr ? 255 : 0;
                    o[y * w + x] = (byte)nw;
                    e[i] = old - nw;
                };

                if (count >= 512)
                    Parallel.For(y0, y1 + 1, doRow);     // wide diagonal: worth using all cores
                else
                    for (int y = y0; y <= y1; y++) doRow(y);
            }
            return o;
        }

        /// <summary>Share of pixels that have the same 1-bit value (same metric as bw_similarity.py).</summary>
        public static double Similarity(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("images must have the same size");
            long same = 0;
            for (int i = 0; i < a.Length; i++)
                if ((a[i] > 128) == (b[i] > 128)) same++;
            return (double)same / a.Length;
        }
    }
}
