# Lab 5: The Readers–Writers Problem

**Topic:** process synchronization. Many readers may share data, but a writer needs it alone.
**Status:** ✅ lecture, assignment and C# code ready

## What you will learn

- The rules: many readers at once, only one writer, and no readers while a writer writes
- The first version (readers first) and the second version (writers first)
- **Starvation**, and which side can starve in each version
- How `read_count`, a `mutex` and an `rw_lock` solve the problem

## Files

| File | What it is |
|------|------------|
| [lecture.md](lecture.md) | Theory with a real-life picture, a timeline, and the solution in plain words |
| [exercises.md](exercises.md) | The assignment (100 threads, writers and readers) and what output to expect |
| [main.cs](main.cs) | **Our solution in C#** (fair version with a turnstile) |
| [ReadersWriters.csproj](ReadersWriters.csproj) | Project file, so `dotnet run` works in this folder |

## How to study

1. Read [lecture.md](lecture.md) sections 1–6 to understand the problem.
2. Answer the questions in section 7 *before* you write code.
3. Use the plan in section 8 for your solution.

## Run it

You need the .NET SDK (8.0 or newer). Run from this folder:

```bash
dotnet run -c Release                  # asks: Number of writers (0-100, Enter = 5):
dotnet run -c Release -- --writers 8   # no question, 8 writers
```

| Option | Meaning |
|--------|---------|
| `--writers W` | Number of writers, from 0 to the number of threads. Without it the program asks (Enter = 5) |
| `--threads N` | Total number of threads (default 100) |
| `--seed S` | Fixed random seed, to repeat the same run |

Example output (5 writers, 95 readers):

```
no = 11
x = 1
no = 35
x = 2
no = 37
x = 3
no = 54
x = 4
no = 87
x = 5
elapsed time = 11.21 seconds
final x = 5 (expected 5)  OK
rule violations = 0  OK
```

Only writers print. `no` is the order in which a thread entered the critical section (counting all 100 threads), not the order in which it was created or started. The last two lines are our own self-check: `x` must equal the number of writers, and no rule may be broken (a writer inside together with another writer or a reader, or `x` changing while a reader reads). The exit code is 0 when both are fine and 2 when not.

## Why a turnstile?

With only `read_count`, `mutex` and `rw_lock` (the *first*, readers-first version) all 5 writers wait for the readers, and the 95 readers arrive within 1 second, so there is always a reader inside. The writers get in at the very end (`no = 96 ... 100`): **writer starvation**. The code adds one more semaphore, `turnstile`: everybody passes it before asking for the data, so a waiting writer blocks the readers behind it. The readers already inside finish, the writer goes in, and nobody starves. To see the starvation yourself, delete the `turnstile` lines in `Writer()` and `Reader()` and run again.

## Key words

Critical section, mutual exclusion, race condition, semaphore, mutex, starvation.
See the [glossary](../../docs/glossary.md).
