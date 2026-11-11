// Lab 5: the readers-writers problem (fair version: nobody starves).
//
//   dotnet run -c Release                        asks how many writers you want (Enter = 5)
//   dotnet run -c Release -- --writers 8         no question, 8 writers
//
//   --writers W    number of writer threads, 0..threads        (default: ask, Enter = 5)
//   --threads N    total number of threads                     (default: 100)
//   --seed S       fixed random seed, to repeat the same run   (default: random)
//
// Rules of the assignment
//   - x starts at 0. N threads are created, W of them (chosen at random) are writers, the rest readers.
//   - Threads must not start together: each one waits a random 0.00-1.00 s and is then started,
//     so all threads are started within 1 second.
//   - A reader reads x for 1 second. A writer does x = x + 1, which also takes 1 second.
//   - Only writers print:  "no = <order of entering the critical section>"  then  "x = <new value>".
//   - Correct synchronization: final x == W, and the run takes about 6-11 s (readers share the
//     data, writers go one by one).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace ReadersWritersLab
{
    internal static class Program
    {
        private const int DefaultThreads = 100;
        private const int DefaultWriters = 5;
        private const int WorkMs = 1000;        // reading or writing x takes 1 second
        private const int StartWindowMs = 1000; // every thread is started within the first second

        // ---- the shared data and the three tools from the lecture --------------------------------
        private static int x = 0;               // the shared data
        private static int readCount = 0;       // how many readers are inside right now
        private static readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);   // protects readCount
        // rw_lock keeps writers alone. The FIRST reader takes it and the LAST reader gives it back, and
        // those are different threads, so it must be a semaphore (a Mutex or lock() must be released by
        // the thread that took it).
        private static readonly SemaphoreSlim rwLock = new SemaphoreSlim(1, 1);
        // turnstile = one waiting line for everybody. With only mutex + rwLock (the "readers first" version)
        // new readers keep slipping in, so 95 readers that arrive within 1 second keep the writers out
        // until the very end (writer starvation). Everybody passes the turnstile before asking for the
        // data, so a waiting writer blocks the readers behind it and gets in as soon as the readers
        // already inside leave. This is the fair version: nobody starves, and the order of entering
        // follows the order of arrival.
        private static readonly SemaphoreSlim turnstile = new SemaphoreSlim(1, 1);

        // ---- only for the output and for checking our own work -----------------------------------
        private static int entryNo = 0;         // order of entering the critical section (all threads)
        private static int activeReaders = 0;
        private static int activeWriters = 0;
        private static int violations = 0;      // how many times a rule was broken (must stay 0)

        private static int Main(string[] args)
        {
            int threads = DefaultThreads;
            int writers = -1;                   // -1 = not given, ask the user
            int seed = Environment.TickCount;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--writers": writers = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        case "--threads": threads = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        case "--seed": seed = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        default: throw new ArgumentException("unknown option " + args[i]);
                    }
                }
                if (threads < 1) throw new ArgumentException("--threads must be at least 1");
                if (args.Length > 0 && writers != -1 && (writers < 0 || writers > threads))
                    throw new ArgumentException("--writers must be between 0 and " + threads);
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException || ex is FormatException || ex is ArgumentException)
            {
                Console.Error.WriteLine("Bad arguments: " + ex.Message);
                Console.Error.WriteLine("Usage: dotnet run -c Release -- [--writers W] [--threads N] [--seed S]");
                return 1;
            }

            if (writers == -1) writers = AskWriters(threads);

            Random random = new Random(seed);

            // 1. Choose the writers at random: shuffle the numbers 0..N-1, the first W become writers.
            int[] order = new int[threads];
            for (int i = 0; i < threads; i++) order[i] = i;
            for (int i = threads - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
            }
            bool[] isWriter = new bool[threads];
            for (int i = 0; i < writers; i++) isWriter[order[i]] = true;

            // 2. Create the threads (not started yet) and give each one a random waiting time 0..1 s.
            Thread[] pool = new Thread[threads];
            double[] startAtMs = new double[threads];
            int[] startOrder = new int[threads];
            for (int i = 0; i < threads; i++)
            {
                pool[i] = new Thread(isWriter[i] ? (ThreadStart)Writer : Reader);
                startAtMs[i] = random.NextDouble() * StartWindowMs;
                startOrder[i] = i;
            }
            Array.Sort(startOrder, (a, b) => startAtMs[a].CompareTo(startAtMs[b]));

            // 3. Start the threads: the main thread waits until each thread's random time, then starts it.
            //    Starting in time order means the main thread never waits twice for the same second.
            Stopwatch clock = Stopwatch.StartNew();
            foreach (int id in startOrder)
            {
                double wait = startAtMs[id] - clock.Elapsed.TotalMilliseconds;
                if (wait > 0) Thread.Sleep((int)Math.Ceiling(wait));
                pool[id].Start();
            }

            // 4. Wait for every thread, then report.
            foreach (Thread t in pool) t.Join();
            clock.Stop();

            Console.WriteLine("elapsed time = " + clock.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture) + " seconds");

            bool xOk = x == writers;
            Console.WriteLine("final x = " + x + " (expected " + writers + ")  " + (xOk ? "OK" : "WRONG"));
            Console.WriteLine("rule violations = " + violations + "  " + (violations == 0 ? "OK" : "WRONG"));
            return (xOk && violations == 0) ? 0 : 2;
        }

        /// <summary>Asks for the number of writers until a valid answer comes. Enter alone = the default.</summary>
        private static int AskWriters(int threads)
        {
            while (true)
            {
                Console.Write("Number of writers (0-" + threads + ", Enter = " + Math.Min(DefaultWriters, threads) + "): ");
                string line = Console.ReadLine();
                if (line == null) return Math.Min(DefaultWriters, threads);            // no keyboard input (input is closed)
                line = line.Trim().Trim('﻿', ' ');         // a piped input can start with an invisible BOM character
                if (line.Length == 0) return Math.Min(DefaultWriters, threads);

                int value;
                if (int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0 && value <= threads)
                    return value;
                Console.WriteLine("Please type a whole number from 0 to " + threads + ".");
            }
        }

        /// <summary>Writer: has the data alone for 1 second and adds 1 to x.</summary>
        private static void Writer()
        {
            turnstile.Wait();                                   // join the line: from now on nobody can overtake me
            rwLock.Wait();                                      // wait until nobody (reader or writer) is inside
            turnstile.Release();                                // the line moves on (and waits behind me while I write)

            // ---- critical section ----
            int no = Interlocked.Increment(ref entryNo);
            if (Interlocked.Increment(ref activeWriters) != 1 || Volatile.Read(ref activeReaders) != 0)
                Interlocked.Increment(ref violations);          // somebody else is inside: the lock failed

            Console.WriteLine("no = " + no);
            int old = x;                                        // read ...
            Thread.Sleep(WorkMs);                               // ... 1 second of "work" ...
            x = old + 1;                                        // ... write. Without the lock two writers would lose an update here
            Console.WriteLine("x = " + x);

            Interlocked.Decrement(ref activeWriters);
            // ---- end of critical section ----

            rwLock.Release();
        }

        /// <summary>Reader: reads x for 1 second. Many readers may be inside at the same time.</summary>
        private static void Reader()
        {
            turnstile.Wait();                                   // join the line (a waiting writer in front of me blocks me here)
            mutex.Wait();                                       // entry: only one reader changes readCount at a time
            readCount++;
            if (readCount == 1) rwLock.Wait();                  // first reader keeps the writers out
            mutex.Release();
            turnstile.Release();                                // the next one in line (reader or writer) may go

            // ---- critical section ----
            Interlocked.Increment(ref entryNo);
            Interlocked.Increment(ref activeReaders);
            if (Volatile.Read(ref activeWriters) != 0)
                Interlocked.Increment(ref violations);          // a writer is inside while we read

            int seen = Volatile.Read(ref x);
            Thread.Sleep(WorkMs);
            if (Volatile.Read(ref x) != seen)
                Interlocked.Increment(ref violations);          // x changed while we were reading it

            Interlocked.Decrement(ref activeReaders);
            // ---- end of critical section ----

            mutex.Wait();                                       // exit
            readCount--;
            if (readCount == 0) rwLock.Release();               // last reader lets the writers in again
            mutex.Release();
        }
    }
}
