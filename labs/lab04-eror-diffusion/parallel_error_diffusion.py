"""
Lab 4: Error diffusion (Floyd-Steinberg) -- sequential and parallel versions.

All four modes give EXACTLY the same picture as the sequential reference
(error-diffusion/error-diffusion/error_diffusion.py):

    sequential  plain Python loop, one process, one thread    (baseline)
    threads     worker threads, rows run as a pipeline
    processes   same pipeline, but real processes + shared memory (uses all CPU cores)
    wavefront   NumPy: all pixels with the same x + 2*y are independent -> one vector step

Usage:
    python parallel_error_diffusion.py <input> <output> [--mode MODE] [--workers N]
                                       [--threshold T] [--block B] [--check REFERENCE.png]
"""
import argparse
import sys
import time
from multiprocessing import Process, cpu_count
from multiprocessing.shared_memory import SharedMemory
from threading import Thread

import numpy as np
from PIL import Image

# ---------------------------------------------------------------------------
# Memory layout (shared by every mode)
#
# The image is stored in a "padded" 1-D int32 array F with row length S = W + 2:
#   pixel (y, x)  ->  index  y*S + x + 1
# The padding column on each side and one extra row at the bottom swallow the
# error that would fall outside the picture, so the inner loop needs no
# "if x+1 < width" checks.  The four neighbours of index i are:
#   right = i+1 (7/16)   below-left = i+S-1 (3/16)
#   below = i+S (5/16)   below-right = i+S+1 (1/16)
# Python's // rounds toward minus infinity, exactly like the reference code.
# ---------------------------------------------------------------------------


def make_padded(gray):
    """gray: 2-D uint8 array -> padded flat int32 array."""
    h, w = gray.shape
    padded = np.zeros((h + 1, w + 2), dtype=np.int32)
    padded[:h, 1:w + 1] = gray
    return padded.reshape(-1)


# ---------------------------------------------------------------------------
# Modes 1-3: the row pipeline
#
# Pixel (y, x) can be quantised only after its four "feeders" are finished:
# (y, x-1) in the same row and (y-1, x-1), (y-1, x), (y-1, x+1) in the row above.
# So row y may run ahead as long as it stays 2 pixels behind row y-1.
# Each row has a progress counter = how many pixels of that row are finished.
# (Two pixels, not one, because pixel x+2 of the row above also adds error into
#  our pixel x+1, and two workers must never add into the same cell at once.)
# ---------------------------------------------------------------------------
LAG = 2


def process_rows(F, O, progress, rows, w, thr, block, wait):
    """Quantise the given rows (in increasing order). F, O, progress are indexable buffers."""
    S = w + 2
    for y in rows:
        base = y * S + 1
        obase = y * w
        for xs in range(0, w, block):
            xe = min(xs + block, w)
            if y > 0:
                # last pixel of this block (xe-1) needs row y-1 finished up to pixel xe+1
                wait(progress, y - 1, min(xe + LAG, w))
            for x in range(xs, xe):
                i = base + x
                old = F[i]
                new = 255 if old > thr else 0
                O[obase + x] = new
                err = old - new
                if err:
                    F[i + 1] += (err * 7) // 16
                    F[i + S - 1] += (err * 3) // 16
                    F[i + S] += (err * 5) // 16
                    F[i + S + 1] += err // 16
            progress[y] = xe


def spin_wait(progress, row, need):
    """Sleep-poll until row `row` has finished at least `need` pixels."""
    while progress[row] < need:
        time.sleep(0.0001)


def run_sequential(gray, thr):
    h, w = gray.shape
    F = memoryview(make_padded(gray))
    O = np.zeros(h * w, dtype=np.uint8)
    process_rows(F, memoryview(O), [0] * h, range(h), w, thr, w, spin_wait)
    return O.reshape(h, w)


def run_threads(gray, thr, workers, block):
    h, w = gray.shape
    F = memoryview(make_padded(gray))
    O = np.zeros(h * w, dtype=np.uint8)
    Om = memoryview(O)
    progress = [0] * h
    threads = [Thread(target=process_rows,
                      args=(F, Om, progress, range(k, h, workers), w, thr, block, spin_wait))
               for k in range(workers)]
    for t in threads:
        t.start()
    for t in threads:
        t.join()
    return O.reshape(h, w)


def _proc_worker(names, w, thr, rows, block):
    shms = [SharedMemory(name=n) for n in names]
    F = shms[0].buf.cast('i')
    O = shms[1].buf.cast('B')
    progress = shms[2].buf.cast('i')
    try:
        process_rows(F, O, progress, rows, w, thr, block, spin_wait)
    finally:
        for m in (F, O, progress):
            m.release()
        for s in shms:
            s.close()


def run_processes(gray, thr, workers, block):
    h, w = gray.shape
    padded = make_padded(gray)
    shm_f = SharedMemory(create=True, size=padded.nbytes)
    shm_o = SharedMemory(create=True, size=h * w)
    shm_p = SharedMemory(create=True, size=h * 4)
    try:
        np.ndarray(padded.shape, np.int32, shm_f.buf)[:] = padded
        np.ndarray(h * w, np.uint8, shm_o.buf)[:] = 0
        np.ndarray(h, np.int32, shm_p.buf)[:] = 0
        names = (shm_f.name, shm_o.name, shm_p.name)
        procs = [Process(target=_proc_worker, args=(names, w, thr, range(k, h, workers), block))
                 for k in range(workers)]
        for p in procs:
            p.start()
        for p in procs:
            p.join()
        if any(p.exitcode != 0 for p in procs):
            raise RuntimeError("a worker process failed")
        return np.ndarray((h, w), np.uint8, shm_o.buf).copy()
    finally:
        for s in (shm_f, shm_o, shm_p):
            s.close()
            s.unlink()


# ---------------------------------------------------------------------------
# Mode 4: wavefront with NumPy
#
# Every dependency of (y, x) has a smaller value of t = x + 2*y, so all pixels
# with the same t are independent. In the padded flat array those pixels are
# exactly F[p0 :: W] (a strided slice), so one diagonal = a few NumPy calls.
# ---------------------------------------------------------------------------
def run_wavefront(gray, thr):
    h, w = gray.shape
    S = w + 2
    F = make_padded(gray)
    O = np.zeros(h * w, dtype=np.uint8)
    for t in range(w + 2 * h - 2):
        y0 = max(0, (t - w + 2) // 2)      # smallest y with x = t-2y <= w-1
        y1 = min(h - 1, t // 2)            # largest  y with x = t-2y >= 0
        n = y1 - y0 + 1
        if n <= 0:
            continue
        p0 = y0 * S + (t - 2 * y0) + 1     # padded index of the first pixel
        span = (n - 1) * w + 1

        def view(off):
            return F[p0 + off:p0 + off + span:w]

        old = view(0).copy()
        new = np.where(old > thr, 255, 0).astype(np.int32)
        err = old - new
        ys = np.arange(y0, y1 + 1)
        O[ys * w + (t - 2 * ys)] = new
        view(1)[:] += (err * 7) // 16
        view(S - 1)[:] += (err * 3) // 16
        view(S)[:] += (err * 5) // 16
        view(S + 1)[:] += err // 16
    return O.reshape(h, w)


# ---------------------------------------------------------------------------
def similarity(a, b):
    """Same metric as bw_similarity.py: share of pixels whose 1-bit value is equal."""
    return float(np.mean((a > 128) == (b > 128)))


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--mode", choices=["sequential", "threads", "processes", "wavefront"],
                    default="wavefront")
    ap.add_argument("--workers", type=int, default=cpu_count(), help="threads/processes to use")
    ap.add_argument("--threshold", type=int, default=128)
    ap.add_argument("--block", type=int, default=512, help="pixels between progress updates")
    ap.add_argument("--check", metavar="REFERENCE", help="compare the result with this 1-bit image")
    args = ap.parse_args()

    gray = np.array(Image.open(args.input).convert("L"))
    print(f"{args.input}: {gray.shape[1]}x{gray.shape[0]} pixels, mode={args.mode}"
          + (f", workers={args.workers}" if args.mode in ("threads", "processes") else ""))

    start = time.perf_counter()
    if args.mode == "sequential":
        out = run_sequential(gray, args.threshold)
    elif args.mode == "threads":
        out = run_threads(gray, args.threshold, args.workers, args.block)
    elif args.mode == "processes":
        out = run_processes(gray, args.threshold, args.workers, args.block)
    else:
        out = run_wavefront(gray, args.threshold)
    elapsed = time.perf_counter() - start
    print(f"Time: {elapsed:.2f} s")

    Image.fromarray(out).save(args.output)
    print(f"Saved {args.output}")

    if args.check:
        ref = np.array(Image.open(args.check).convert("L"))
        if ref.shape != out.shape:
            print(f"Cannot compare: reference is {ref.shape}, result is {out.shape}")
            sys.exit(1)
        s = similarity(out, ref)
        print(f"Similarity with {args.check}: {s * 100:.2f}%")
        sys.exit(0 if s == 1.0 else 2)


if __name__ == "__main__":
    main()
