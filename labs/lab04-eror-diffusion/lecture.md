# Lecture: Error Diffusion, and How to Parallelize It

> Companion notes for `parallel_error_diffusion.py`

---

## 1. The problem

A grayscale picture has 256 levels (0 = black, 255 = white). A printer or a 1-bit screen can show only **black or white**.

- **Simple threshold:** pixel > 128 → white, else black. Fast, but a smooth gray becomes a flat block. Detail is lost.
- **Error diffusion:** do the same threshold, but **remember the mistake** and give it to the neighbours. The picture looks gray again when you step back.

> 🪙 **Analogy:** you must pay in whole coins only. If you owe 0.4 and pay 0, you "owe" 0.4 to the next payment. Over many payments the total is right.

---

## 2. The algorithm (Floyd–Steinberg)

For each pixel, left to right, top to bottom:

```
old   = value of the pixel (it already contains errors from earlier pixels)
new   = 255 if old > 128 else 0
error = old - new
spread the error to 4 neighbours:

            X     7/16
   3/16   5/16    1/16
```

```python
img[y, x+1]   += error * 7 // 16
img[y+1, x-1] += error * 3 // 16
img[y+1, x]   += error * 5 // 16
img[y+1, x+1] += error * 1 // 16
```

Pixels outside the picture are skipped (the error is lost there).
The given reference is [error_diffusion.py](error-diffusion/error-diffusion/error_diffusion.py).
Note `//` rounds **down** (also for negative numbers). Our parallel code must do the same, or the pictures will differ.

---

## 3. Why is it hard to parallelize?

Pixel `(y, x)` cannot be decided until it has received error from **four** earlier pixels:

```
 (y-1, x-1)  (y-1, x)  (y-1, x+1)       ← row above
 (y,   x-1)  [ y, x ]                   ← same row
```

This is a **data dependency**. Splitting the picture into 4 equal blocks and running them separately **gives a wrong picture** (the error does not cross the block border).

> 🧠 **Key idea:** a dependency does not mean "no parallelism". It means we must respect the **order**. We need to find work that does not depend on each other.

---

## 4. Idea A: a pipeline of rows

Row `y` only needs row `y-1` to be a little **ahead** (about 2 pixels), not finished.

```
time →
row 0:  ████████████████████████
row 1:     ░░██████████████████        ← starts when row 0 is 2 pixels ahead
row 2:        ░░████████████
row 3:           ░░██████
         (each row follows the row above like a conga line)
```

Each row has a **progress counter** (how many pixels are done). Row `y` **waits** until `progress[y-1]` is large enough, then continues.
We give rows to workers in turns: worker `k` takes rows `k, k+N, k+2N, ...` (N = number of workers).

### Why the lag is 2 pixels and not 1

Pixel `(y-1, x+2)` also adds error into `(y, x+1)`. If row `y` already worked on `(y, x+1)` at the same time, two workers would do `+=` on the same cell. That is a **race condition** (a lost update). With a lag of 2, all writers of a cell are finished before its reader starts.

### Does it deadlock?

No. Row `y` only waits for row `y-1`, and row 0 waits for nobody. A worker handles its rows in increasing order, so there is never a circle of waiting.

### Blocks

Checking the counter after every pixel is slow. We check once per **block** (default 512 pixels). Bigger blocks = less waiting overhead but a longer "start-up" before the next row can begin.

---

## 5. Threads vs processes in Python

The pipeline code is the same. Only the workers differ.

| | `threads` | `processes` |
|--|-----------|-------------|
| Worker | `threading.Thread` | `multiprocessing.Process` |
| Memory | shared automatically | must be shared on purpose: `SharedMemory` |
| Python code runs in parallel? | ❌ **No**, the **GIL** lets only one thread run Python bytecode at a time | ✅ Yes, each process has its own interpreter and its own GIL |
| Result | about the same time as sequential | real speed-up |

> 🔑 **GIL (Global Interpreter Lock):** a mutex inside CPython. Threads are good for waiting (I/O, sockets, as in Lab 3) but not for heavy computing in pure Python.

With processes, the picture lives in **shared memory** (one block of RAM mapped into every process). The progress counters are in shared memory too, so every process sees them. Workers poll with a tiny `sleep` when they must wait.

> ⚠️ This simple version relies on the CPU writing memory in order (true for x86). On other CPUs a real program would use a lock or a semaphore for the counters.

**Why more workers is not always faster:** only ~(rows ÷ lag) pixels can be in flight, the workers poll and compete for cores, and the shared memory is slower than local variables. In our test, 4 workers were better than 16.

---

## 6. Idea B: the wavefront

Look for pixels that are **independent**. Give each pixel the number `t = x + 2*y`.
Every feeder of a pixel has a **smaller** `t`:

| Feeder | t of the feeder |
|--------|-----------------|
| `(y, x-1)` | t − 1 |
| `(y-1, x+1)` | t − 1 |
| `(y-1, x)` | t − 2 |
| `(y-1, x-1)` | t − 3 |

So **all pixels with the same `t` can be done at the same time**. They form a slanted line (a wavefront) that sweeps over the picture:

```
 t:  0 1 2 3 4 5 6 7
y=0: 0 1 2 3 4 5 6 7
y=1: 2 3 4 5 6 7 8 9      ← same number = same step
y=2: 4 5 6 7 8 9 ...
```

The number of steps is `W + 2H` instead of `W × H` pixels. Each step is one NumPy operation on a whole slanted line (a strided slice of the padded array), so it runs in C, with SIMD. This is **data parallelism** (the same operation on many data at once), while the pipeline is **task parallelism** (different workers do different rows).

---

## 7. Checking the result

A parallel program that is **fast but wrong** is useless. The reference answer comes from the sequential code.

- `bw_similarity.py` converts both images to 1 bit and prints the percentage of equal pixels. Differences are drawn in **red**.
- `--check reference.png` does the same without opening a window.

Our algorithm does exactly the same additions in a different order. Integer additions can be reordered, and every error is rounded **before** it is added, so the answer is **exactly 100%** the same. If you get 99.9%, you have a bug (usually a race condition or different rounding).

---

## 8. Cheat sheet

| Concept | One line |
|---------|----------|
| Error diffusion | Threshold a pixel, give the error to 4 neighbours (7/16, 3/16, 5/16, 1/16) |
| Data dependency | Pixel needs results of earlier pixels, so order matters |
| Pipeline | Row `y` follows row `y-1` at a 2-pixel distance |
| Wavefront | Pixels with equal `x + 2y` are independent |
| Speed-up | `T_sequential / T_parallel` |
| GIL | Only one Python thread computes at a time |
| Shared memory | RAM that several processes can read and write |
| Race condition | Two workers update the same cell, so an update is lost |
