# Lab 4: Error Diffusion (Parallel Image Dithering)

**Topic:** turning a grayscale picture into pure black and white, and making it faster with threads and processes even though every pixel depends on the pixels before it.
**Status:** ✅ lecture, code and exercises ready

## What you will learn

- **Error diffusion** (Floyd–Steinberg): how a picture with 256 grays becomes a 1-bit picture
- A **data dependency** and why it makes the algorithm look impossible to parallelize
- Two ways around it: a **pipeline** (rows follow each other) and a **wavefront** (diagonals)
- Why Python **threads** give no speed-up (the **GIL**) but **processes** with **shared memory** do
- How to **check** a parallel result against a reference (100% similarity)

## Files

| File | What it is |
|------|------------|
| [lecture.md](lecture.md) | Theory with diagrams. Read this first |
| [parallel_error_diffusion.py](parallel_error_diffusion.py) | **Our solution in Python:** 4 modes (`sequential`, `threads`, `processes`, `wavefront`) |
| [csharp/](csharp/) | **Our solution in C#:** [ErrorDiffusion.cs](csharp/ErrorDiffusion.cs) (the algorithm), [PngGray.cs](csharp/PngGray.cs) (PNG reader/writer), [Program.cs](csharp/Program.cs) (command line), [ErrorDiffusion.csproj](csharp/ErrorDiffusion.csproj) |
| [exercises.md](exercises.md) | How to run it, experiments, and self-check questions |
| [error-diffusion/error-diffusion/error_diffusion.py](error-diffusion/error-diffusion/error_diffusion.py) | **Given:** the sequential reference |
| [error-diffusion/error-diffusion/bw_similarity.py](error-diffusion/error-diffusion/bw_similarity.py) | **Given:** compares two 1-bit images and shows the differences in red |
| [error-diffusion/error-diffusion/to_grayscale/to_grayscale.py](error-diffusion/error-diffusion/to_grayscale/to_grayscale.py) | **Given:** converts a color picture to grayscale |
| `error-diffusion/error-diffusion/*-grayscale.png` | Input pictures (`world`, `star`) |
| `error-diffusion/error-diffusion/*-bw.png` | **Expected answers** to check against |

## Run it

You need Python 3 with `numpy` and `pillow` (`pip install numpy pillow matplotlib`) for the Python version, or the .NET SDK for the C# version.
Run from this folder (on Linux/macOS use `python3`).

```bash
# (bash/Git Bash; in PowerShell run `$D = "error-diffusion/error-diffusion"` and use "$D/...")
D=error-diffusion/error-diffusion

# fastest: wavefront (about 0.3 s for the 4000x2000 world picture)
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode wavefront --check $D/world-bw.png

# the OS-style versions
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode sequential --check $D/world-bw.png
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode threads   --workers 4 --check $D/world-bw.png
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode processes --workers 4 --check $D/world-bw.png
```

`--check` prints the similarity with the expected picture and exits with an error if it is not 100%.
To *see* the differences, use the given script:
`python $D/bw_similarity.py out.png $D/world-bw.png` (slow on big pictures; try `star` only if you are patient).

### Run the C# version (sequential vs parallel comparison)

You need the .NET **SDK** (8.0 or newer), not only the runtime. There are **no NuGet packages**: `PngGray.cs` is a small PNG reader/writer, so it works offline.

> If you see `No .NET SDKs were found`, you only have the runtime. Check with `dotnet --list-sdks` (it must print a version). Install the SDK with `winget install Microsoft.DotNet.SDK.8` (Windows) or from <https://aka.ms/dotnet/download>, then open a **new** terminal.

The program runs **both** versions on each picture and prints one comparison table. Run it from the `csharp` folder.
The pictures are one folder up, so the paths start with `../` (it works the same in PowerShell, cmd and bash):

```bash
cd csharp
dotnet run -c Release -- ../error-diffusion/error-diffusion/world-grayscale.png ../error-diffusion/error-diffusion/star-grayscale.png
```

Example output (16 logical cores, .NET 8):

```
Error diffusion: sequential vs parallel (pipeline, 8 threads of 16 cores), median of 5 runs, times in milliseconds

Input               |      Size | Sequential (ms) | Parallel (ms) | Speed-up | Accuracy vs sequential | Sequential vs given -bw
-------------------------------------------------------------------------------------------------------------------------------
world-grayscale.png | 4000x2000 |           28.57 |         16.60 |    1.72x |                100.00% |                 100.00%
star-grayscale.png  | 9028x3790 |          160.97 |         50.30 |    3.20x |                100.00% |                 100.00%

Result: the parallel picture is identical to the sequential one.
```

| Column | Meaning |
|--------|---------|
| Input, Size | File name and pixels |
| Sequential (ms), Parallel (ms) | Median of 5 timed runs (after one warm-up run), in **milliseconds**. Only the dithering is timed, not loading or saving the PNG |
| Speed-up | Sequential ÷ parallel |
| Accuracy vs sequential | Share of pixels where the parallel picture equals the sequential picture. It must be **100.00%** |
| Sequential vs given -bw | The sequential picture compared with the given `<name>-bw.png` answer (shown only if that file is next to `<name>-grayscale.png`) |

The exit code is 0 when the accuracy is 100% and 2 when it is not.

| Option | Meaning |
|--------|---------|
| `--mode pipeline` (default) / `wavefront` | Which parallel version to test |
| `--workers N` | Number of threads. Limited to the number of cores. Default: half of the logical cores |
| `--runs R` | Timed runs per version (default 5) |
| `--block B` | Pipeline: pixels between progress updates (default 512) |
| `--threshold T` | Black/white threshold (default 128) |
| `--save DIR` | Also save `<name>-sequential.png` and `<name>-parallel.png` into `DIR` |

| C# parallel mode | Idea |
|------------------|------|
| `pipeline` | `n` worker threads, each row stays 2 pixels behind the row above. Same idea as Python `threads`/`processes`, but C# threads **do** run in parallel (there is no GIL) and share memory directly |
| `wavefront` | Pixels with equal `x + 2*y` run with `Parallel.For`. Each pixel *pulls* the error from its 4 feeders instead of pushing it, so threads never write to the same cell. Slower than the pipeline here (thousands of tiny parallel calls) |

> ⚠️ The Python commands above are written for the lab folder (`error-diffusion/error-diffusion/...`). In `csharp/` you need the extra `../`, or you get `DirectoryNotFoundException`.

**Why the speed-up is not 8x with 8 threads:** the work per pixel is only a few nanoseconds, so waiting for the row above, thread start-up and memory speed matter. A bigger picture (`star`) gains more than a small one (`world`). Too many threads is worse: on the test machine 8 threads gave 3.1x on `star`, 16 threads only 1.8x.

#### The five implementation rules and where they are in the code

| Rule | In [ErrorDiffusion.cs](csharp/ErrorDiffusion.cs) |
|------|--------------------------------------------------|
| 1. Add dummy pixels on the edge | `MakePadded`: one extra column on each side and one extra row at the bottom. The error that would leave the picture falls into the dummy pixels, so the inner loop has no `if x+1 < width` checks (the wavefront uses the same trick with its error array) |
| 2. Use `byte`, not `float`/`double` | The picture and the result are `byte[]` (one byte per pixel); no floating point anywhere. The running sum of errors is an `int`, because it can go below 0 or above 255 before the pixel is decided, and the reference code keeps it unclipped too |
| 3. Integer arithmetic (50 / 16 = 3) | Error x 7, 3, 5, 1 divided by 16 with `>> 4`. It rounds *down* like Python's `//`, so negative errors match the reference (C#'s `/` would round toward zero and give a different picture) |
| 4. `n` threads, `n <= #cores` | `--workers` is limited to `Environment.ProcessorCount` (the program prints a note when it lowers your value) |
| 5. Create `n` threads and reuse them | `Pipeline` creates the `n` threads **once**; thread `k` then handles rows `k, k+n, k+2n, ...`, so one thread serves many rows (no thread per row or per pixel). `Parallel.For` in the wavefront reuses the .NET thread pool |

### Python results (4000×2000 `world`, 16 logical cores, Python 3.11, Windows)

| Mode | Time | Same as reference? |
|------|-----:|:------------------:|
| `sequential` | ≈ 8–9 s | ✅ 100% |
| `threads` (4 workers) | ≈ 8 s (no gain) | ✅ 100% |
| `processes` (4 workers) | ≈ 3 s | ✅ 100% |
| `wavefront` (NumPy) | ≈ 0.3–0.4 s | ✅ 100% |

The 9028×3790 `star` picture also matches `star-bw.png` at 100% (wavefront, about 1.7 s).
Times change from machine to machine. Compare the order, not the exact numbers.

## Key words

Error diffusion, dithering, data dependency, pipeline, wavefront, speed-up, GIL, shared memory, race condition, memory visibility (`Volatile`).
See the [glossary](../../docs/glossary.md).
