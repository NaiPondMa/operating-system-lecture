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
| [parallel_error_diffusion.py](parallel_error_diffusion.py) | **Our solution:** 4 modes (`sequential`, `threads`, `processes`, `wavefront`) |
| [exercises.md](exercises.md) | How to run it, experiments, and self-check questions |
| [error-diffusion/error-diffusion/error_diffusion.py](error-diffusion/error-diffusion/error_diffusion.py) | **Given:** the sequential reference |
| [error-diffusion/error-diffusion/bw_similarity.py](error-diffusion/error-diffusion/bw_similarity.py) | **Given:** compares two 1-bit images and shows the differences in red |
| [error-diffusion/error-diffusion/to_grayscale/to_grayscale.py](error-diffusion/error-diffusion/to_grayscale/to_grayscale.py) | **Given:** converts a color picture to grayscale |
| `error-diffusion/error-diffusion/*-grayscale.png` | Input pictures (`world`, `star`) |
| `error-diffusion/error-diffusion/*-bw.png` | **Expected answers** to check against |

## Run it

You need Python 3 with `numpy` and `pillow` (`pip install numpy pillow matplotlib`).
Run from this folder (on Linux/macOS use `python3`).

```bash
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

### Results (4000×2000 `world`, 16 logical cores, Python 3.11, Windows)

| Mode | Time | Same as reference? |
|------|-----:|:------------------:|
| `sequential` | ≈ 8–9 s | ✅ 100% |
| `threads` (4 workers) | ≈ 8 s (no gain) | ✅ 100% |
| `processes` (4 workers) | ≈ 3 s | ✅ 100% |
| `wavefront` (NumPy) | ≈ 0.3–0.4 s | ✅ 100% |

The 9028×3790 `star` picture also matches `star-bw.png` at 100% (wavefront, about 1.7 s).
Times change from machine to machine. Compare the order, not the exact numbers.

## Key words

Error diffusion, dithering, data dependency, pipeline, wavefront, speed-up, GIL, shared memory, race condition.
See the [glossary](../../docs/glossary.md).
