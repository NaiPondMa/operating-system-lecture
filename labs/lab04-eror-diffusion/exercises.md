# Lab 4 Exercises: Make Error Diffusion Parallel

> Read [lecture.md](lecture.md) first. Code: [parallel_error_diffusion.py](parallel_error_diffusion.py).
> Given files are in [error-diffusion/error-diffusion/](error-diffusion/error-diffusion/).

---

## Assignment

1. Run the **sequential** reference ([error_diffusion.py](error-diffusion/error-diffusion/error_diffusion.py)) and understand it.
2. Write a **parallel** version that gives the **same** picture, faster.
3. Check your result against the given `*-bw.png` files with [bw_similarity.py](error-diffusion/error-diffusion/bw_similarity.py). The goal is **100%**.
4. Measure the time and explain the speed-up (or the lack of it).

Our answer is `parallel_error_diffusion.py`. The sections below show how to run and test it.

---

## Part 1: Run it

Install once: `pip install numpy pillow matplotlib`. Open a terminal in this folder.

```bash
D=error-diffusion/error-diffusion
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode sequential --check $D/world-bw.png
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode threads    --check $D/world-bw.png
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode processes  --workers 4 --check $D/world-bw.png
python parallel_error_diffusion.py $D/world-grayscale.png out.png --mode wavefront  --check $D/world-bw.png
```

> 🪟 On Windows PowerShell set the folder with `$D = "error-diffusion/error-diffusion"` and write `$D/world-grayscale.png` as `"$D/world-grayscale.png"`.

Every run should print `Similarity ... 100.00%`. Fill in this table:

| Mode | Time (s) | Speed-up vs sequential |
|------|:--------:|:----------------------:|
| sequential | | 1.0× |
| threads | | |
| processes (4) | | |
| wavefront | | |

Then open `out.png` next to `world-grayscale.png`. Look from far away: it should look gray again.

Optional: see the differences in red with the given tool (it is slow, so use a small crop):

```bash
python $D/bw_similarity.py out.png $D/world-bw.png
```

### Making your own picture

```bash
cd $D/to_grayscale
python to_grayscale.py        # edit the two file names at the bottom of the script first
```

---

## Part 2: Experiments

Guess the result *before* you run it.

### Experiment 1: Cut the picture into blocks

Split a picture into 2 halves (left and right), run the sequential algorithm on each half, and glue them together.
Compare with the reference using `bw_similarity.py`.

<details><summary>Answer</summary>

The similarity is **below 100%**. The error that should flow from the left half into the right half is lost at the border (and across the cut in every row), so the pixels near the border change, and the change can spread. A dependency crossing the border cannot just be ignored.
</details>

### Experiment 2: Block size

Run `--mode processes --workers 4` with `--block 16`, `--block 512`, `--block 4096`.

<details><summary>Answer</summary>

Tiny blocks spend much time checking progress counters. Huge blocks make the next row wait a long time before it can start, so the workers are idle at the start. A middle value is best. The result stays 100% in every case, because the lag rule guarantees correctness and block size only changes speed.
</details>

### Experiment 3: Number of workers

Run `--mode processes` with `--workers 1, 2, 4, 8, 16`. Plot or write down the time.

<details><summary>Answer</summary>

Time drops from 1 to about 4 workers and then gets worse. Reasons: polling and waiting for the row above, workers competing for the same CPU cores, and the work that cannot be parallelized (loading the picture, copying to shared memory, saving). This is **Amdahl's law** in practice.
</details>

### Experiment 4: Break the rule

In `parallel_error_diffusion.py` change `LAG = 2` to `LAG = 0`, run `--mode processes --workers 4 --check ...` on the full `world` picture, several times.

<details><summary>Answer</summary>

The similarity is not 100% any more (we saw 99.85% and 99.93%), and it differs from run to run. On a tiny picture you may be lucky and still get 100%, because a race only hurts when the timing is unlucky. Row `y` now reads and updates pixels the row above has not finished, which is a **race condition** (read too early, and lost `+=` updates). The result depends on timing, which is exactly what a race condition is. Put `LAG = 2` back. (`git checkout` the file, or edit it back.)
</details>

---

## Part 3: Self-check questions

1. Why can't we split the picture into independent blocks?
   <details><summary>Answer</summary>

   Every pixel depends on the error of its left and upper neighbours, so blocks depend on each other at their borders. Ignoring this changes the picture.
   </details>

2. What does the progress counter of a row mean, and who reads it?
   <details><summary>Answer</summary>

   It is the number of pixels of that row that are finished. The worker of the **next** row reads it and waits until the row above is at least 2 pixels ahead.
   </details>

3. Why did `threads` not become faster?
   <details><summary>Answer</summary>

   The GIL lets only one thread run Python code at a time, so the threads take turns. We also pay for switching and waiting. Threads help for I/O waiting, not for pure-Python computing.
   </details>

4. Why does `processes` need `SharedMemory`?
   <details><summary>Answer</summary>

   Each process has its own private memory. Without shared memory every worker would change its own copy, and the rows would never see each other's errors.
   </details>

5. Which pixels can the wavefront compute at the same time?
   <details><summary>Answer</summary>

   Pixels with the same `x + 2*y`. All pixels that feed into them have a smaller value, so they were done in an earlier step.
   </details>

6. How many wavefront steps does a 4000×2000 picture need?
   <details><summary>Answer</summary>

   About `W + 2H = 4000 + 4000 = 8000` steps, instead of 8,000,000 pixel steps.
   </details>

7. The parallel picture has 99.8% similarity. What is the first thing you suspect?
   <details><summary>Answer</summary>

   A race condition or a broken dependency (lag too small), or different rounding (for example `int()` instead of floor division `//` for negative errors).
   </details>

8. What is the difference between task parallelism and data parallelism? Which one is the pipeline, which one the wavefront?
   <details><summary>Answer</summary>

   Task parallelism: different workers do different jobs (the pipeline: each worker has its own rows, at different positions). Data parallelism: the same operation on many data at once (the wavefront: one NumPy step for a whole diagonal).
   </details>
