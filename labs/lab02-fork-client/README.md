# Lab 2: Process Creation (fork, exec, wait)

**Topic:** how a process creates another process: parent and child, pids, `fork()`, `exec()`, `wait()`, and the Windows equivalent.
**Status:** ✅ lecture, code and exercises ready (Linux only for the C programs)

## What you will learn

- **Parent / child / pid**, and how to see them with `ps` and stop them with `kill`
- **Concurrent vs. parallel** execution, and why the output order is not fixed
- **`fork()` / `exec()`**: copy a process, then replace the copy with another program
- **`wait()`** on Linux and **`WaitForSingleObject()`** (Win32 API) on Windows
- Which `#include` each function needs, and how to find it with `man`

## Files

| File | What it is |
|------|------------|
| [lecture.md](lecture.md) | Theory, the `#include` table, `ps`/`kill`, concurrent vs. parallel, the Win32 version. Read this first |
| [exercises.md](exercises.md) | Set-up (WSL), the assignment, experiments and self-check questions |
| [fork10.c](fork10.c) | **Our solution to activity #2:** the parent creates 10 children, each prints its number and pid |
| [fork_exec.c](fork_exec.c) | The example from the slide (`fork` + `execlp("ls")` + `wait`) with the `#include` lines filled in |
| [fork10_ps.c](fork10_ps.c) | Like `fork10.c`, but children stay alive 30 s and print their parent pid, so you can practise `ps` and `kill` |

## Run it

You need a Linux shell with `gcc`. On Windows use WSL (steps in [exercises.md](exercises.md), Part 0).

```bash
cd labs/lab02-fork-client

gcc -Wall -Wextra fork10.c -o fork10 && ./fork10        # the assignment
gcc fork_exec.c -o fork_exec && ./fork_exec             # the slide example
gcc fork10_ps.c -o fork10_ps && ./fork10_ps &           # then: ps -f, pstree -p <pid>, kill <pid>
```

Expected output of `./fork10` (the pids differ, and the children may swap places):

```
I'm the child number 0 (pid 25580)
I'm the child number 1 (pid 25581)
...
I'm the child number 9 (pid 25589)
Parent terminates (pid 25579)
```

> The `#include` lines were missing from the slide. The correct list is in the [lecture, section 3](lecture.md#3-the-include-lines-the-part-that-is-easy-to-get-wrong).

## Key words

fork, exec, wait, pid, ppid, parent, child, zombie, orphan, concurrent, parallel, `CreateProcess`, `WaitForSingleObject`.
See the [glossary](../../docs/glossary.md).
