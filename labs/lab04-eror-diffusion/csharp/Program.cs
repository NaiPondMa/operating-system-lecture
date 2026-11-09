// Compares SEQUENTIAL and PARALLEL error diffusion on one or more pictures.
//
//   dotnet run -c Release -- <input.png> [more inputs...] [options]
//
//   --mode pipeline|wavefront   which parallel version to test        (default: pipeline)
//   --workers N                 number of threads, limited to #cores  (default: #cores / 2)
//   --runs R                    timed runs per version, median shown  (default: 5)
//   --threshold T               black/white threshold                 (default: 128)
//   --block B                   pipeline: pixels between progress updates (default: 512)
//   --save DIR                  also save sequential.png and parallel.png for each input into DIR
//
// Accuracy = share of pixels where the parallel picture equals the sequential picture.
// If a file called <name>-bw.png sits next to <name>-grayscale.png (the given answers),
// the sequential picture is also compared with it.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace ErrorDiffusionLab
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            List<string> inputs = new List<string>();
            string mode = "pipeline", saveDir = null;
            // Half of the logical cores: they are usually 2 per physical core, and our waiting
            // threads spin, so two spinning threads on one physical core slow each other down.
            int workers = Math.Max(1, Environment.ProcessorCount / 2), runs = 5, threshold = 128, block = 512;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--mode": mode = args[++i]; break;
                        case "--workers": workers = int.Parse(args[++i]); break;
                        case "--runs": runs = int.Parse(args[++i]); break;
                        case "--threshold": threshold = int.Parse(args[++i]); break;
                        case "--block": block = int.Parse(args[++i]); break;
                        case "--save": saveDir = args[++i]; break;
                        default:
                            if (args[i].StartsWith("--")) throw new ArgumentException("unknown option " + args[i]);
                            inputs.Add(args[i]);
                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is FormatException || ex is ArgumentException)
            {
                Console.Error.WriteLine("Bad arguments: " + ex.Message);
                inputs.Clear();
            }

            if (inputs.Count == 0 || (mode != "pipeline" && mode != "wavefront") || runs < 1)
            {
                Console.Error.WriteLine(
                    "Usage: dotnet run -c Release -- <input.png> [more inputs...] [--mode pipeline|wavefront]\n" +
                    "                                [--workers N] [--runs R] [--threshold T] [--block B] [--save DIR]");
                return 1;
            }

            // Rule: n threads with n <= number of cores.
            int cores = Environment.ProcessorCount;
            if (mode == "pipeline")
            {
                if (workers < 1) workers = 1;
                if (workers > cores)
                {
                    Console.WriteLine("Note: --workers " + workers + " is more than the " + cores + " cores; using " + cores + ".");
                    workers = cores;
                }
            }

            Console.WriteLine("Error diffusion: sequential vs parallel (" + mode +
                              (mode == "pipeline" ? ", " + workers + " threads of " + cores + " cores" : ", all cores via Parallel.For") +
                              "), median of " + runs + " runs, times in milliseconds");
            Console.WriteLine();

            string[] header = { "Input", "Size", "Sequential (ms)", "Parallel (ms)", "Speed-up", "Accuracy vs sequential", "Sequential vs given -bw" };
            List<string[]> rows = new List<string[]>();
            bool allExact = true;

            foreach (string input in inputs)
            {
                int w, h;
                byte[] gray = PngGray.Read(input, out w, out h);

                Func<byte[]> seq = () => ErrorDiffusion.Sequential(gray, w, h, threshold);
                Func<byte[]> par = mode == "pipeline"
                    ? (Func<byte[]>)(() => ErrorDiffusion.Pipeline(gray, w, h, threshold, workers, block))
                    : () => ErrorDiffusion.Wavefront(gray, w, h, threshold);

                byte[] seqResult, parResult;
                double seqMs = TimeMedian(seq, runs, out seqResult);
                double parMs = TimeMedian(par, runs, out parResult);

                double accuracy = ErrorDiffusion.Similarity(parResult, seqResult);
                if (accuracy != 1.0) allExact = false;

                string vsGiven = "n/a";
                string given = GivenAnswerPath(input);
                if (given != null)
                {
                    int rw, rh;
                    byte[] answer = PngGray.Read(given, out rw, out rh);
                    vsGiven = (rw == w && rh == h) ? Percent(ErrorDiffusion.Similarity(seqResult, answer)) : "size differs";
                }

                if (saveDir != null)
                {
                    Directory.CreateDirectory(saveDir);
                    string name = Path.GetFileNameWithoutExtension(input);
                    PngGray.Write(Path.Combine(saveDir, name + "-sequential.png"), seqResult, w, h);
                    PngGray.Write(Path.Combine(saveDir, name + "-parallel.png"), parResult, w, h);
                }

                rows.Add(new[]
                {
                    Path.GetFileName(input),
                    w + "x" + h,
                    Ms(seqMs),
                    Ms(parMs),
                    (seqMs / parMs).ToString("F2", CultureInfo.InvariantCulture) + "x",
                    Percent(accuracy),
                    vsGiven
                });
            }

            PrintTable(header, rows);
            Console.WriteLine();
            Console.WriteLine(allExact
                ? "Result: the parallel picture is identical to the sequential one."
                : "Result: the parallel picture DIFFERS from the sequential one (race condition or wrong dependency).");
            return allExact ? 0 : 2;
        }

        /// <summary>One untimed warm-up run (JIT, caches), then the median of `runs` timed runs.</summary>
        private static double TimeMedian(Func<byte[]> work, int runs, out byte[] result)
        {
            result = work();
            double[] times = new double[runs];
            for (int r = 0; r < runs; r++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                result = work();
                sw.Stop();
                times[r] = sw.Elapsed.TotalMilliseconds;
            }
            Array.Sort(times);
            return times[runs / 2];
        }

        private static string GivenAnswerPath(string input)
        {
            const string suffix = "-grayscale.png";
            if (!input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return null;
            string candidate = input.Substring(0, input.Length - suffix.Length) + "-bw.png";
            return File.Exists(candidate) ? candidate : null;
        }

        private static string Ms(double ms) { return ms.ToString("F2", CultureInfo.InvariantCulture); }

        private static string Percent(double share) { return (share * 100).ToString("F2", CultureInfo.InvariantCulture) + "%"; }

        private static void PrintTable(string[] header, List<string[]> rows)
        {
            int[] width = new int[header.Length];
            for (int c = 0; c < header.Length; c++)
            {
                width[c] = header[c].Length;
                foreach (string[] row in rows) width[c] = Math.Max(width[c], row[c].Length);
            }
            PrintRow(header, width);
            Console.WriteLine(new string('-', Sum(width) + 3 * (width.Length - 1)));
            foreach (string[] row in rows) PrintRow(row, width);
        }

        private static void PrintRow(string[] cells, int[] width)
        {
            for (int c = 0; c < cells.Length; c++)
            {
                if (c > 0) Console.Write(" | ");
                // text is left-aligned (first column), numbers right-aligned
                Console.Write(c == 0 ? cells[c].PadRight(width[c]) : cells[c].PadLeft(width[c]));
            }
            Console.WriteLine();
        }

        private static int Sum(int[] a) { int s = 0; foreach (int x in a) s += x; return s; }
    }
}
