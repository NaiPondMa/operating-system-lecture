# Lecture: Process Creation (fork, exec, wait)

> Linux only for the C programs. On Windows use WSL (see [exercises.md](exercises.md), Part 0).

## 1. The big idea

A **process** is a running program. Every process (except the very first one) is **created by another process**. The creator is the **parent**, the new one is the **child**. Each process has a number, its **pid** (process id), and knows the pid of its parent (**ppid**).

Analogy: `fork()` is a **photocopier**. You put one worker in, and two identical workers come out, with the same notes in their hands. Only one thing is different: each one is told "you are the original" or "you are the copy". `exec()` is **handing the copy a different job description**. `wait()` is the original **waiting at the door** until the copy comes back.

On Linux every program you start from the terminal is a child of `bash`. Try it: `echo $$` prints the pid of your bash, and `ps -f` shows that your commands have that number as their PPID.

## 2. How it works

### 2.1 `fork()`: one process becomes two

`fork()` makes a **duplicate** of the calling process: same code, same variables (a *copy* of the memory), same next line to run. It is called **once** but returns **twice**, once in each process:

| Where | What `fork()` returns | Meaning |
|-------|:---------------------:|---------|
| Parent | the child's pid (> 0) | "here is your new child" |
| Child | `0` | "you are the new copy" |
| Parent (error) | `-1` | no child was created (for example, too many processes) |

```
          pid_t pid = fork();
                 │
     ┌───────────┴───────────┐
     │ parent                │ child (new process)
     │ pid = 25580           │ pid = 0
     ▼                       ▼
 else-branch            else-if (pid == 0) branch
 wait(NULL);            execlp("/bin/ls", ...);
 printf("Child Complete")
```

The lab slide shows the same code three times, in three colours: the **parent** runs the `else` branch, the **child** runs the `pid == 0` branch, and the **error** case runs the `pid < 0` branch. It is *one program*, and the `if` decides who does what.

> Do not confuse the two numbers. The variable `pid` in the parent holds the **child's** pid. To get your **own** pid, call `getpid()`. To get your parent's pid, call `getppid()`.

### 2.2 `exec()`: load a new program over this one

`execlp("/bin/ls", "ls", NULL)` **replaces** the running program with `/bin/ls`. The process keeps its pid, but its code and memory are thrown away and `ls` starts from the beginning. If `exec` works, it **never returns** (the old code is gone). It returns only on failure (it gives `-1`).

| | `fork()` | `exec()` |
|---|----------|----------|
| Creates a new process? | **Yes** | No |
| New program? | No (same program, copied) | **Yes** (a new one replaces the old one) |
| pid | new pid for the child | same pid |

Why does Linux use two steps instead of one "run this program" call? Because it is simple and flexible: between `fork` and `exec` the child can change things (open files, redirect output) before the new program starts. This is how a shell does `ls > file.txt`.

### 2.3 `wait()`: the parent waits for its child

`wait(NULL)` makes the parent **sleep until one child has terminated**. It returns the pid of the child that finished (or `-1` if there are no children left). To wait for **10** children, call it **10 times** (or loop until it returns `-1`).

Why wait at all?

1. The parent often needs the child's result before it continues.
2. A child that has finished but was never waited for stays in the process table as a **zombie** (`Z` or `<defunct>` in `ps`). `wait()` lets the OS clean it up.
3. If the **parent** ends first, the child becomes an **orphan** and is adopted by another process (often pid 1, `init`/`systemd`).

### 2.4 Life of a child

```
fork() ──► ready/running ──► exit() ──► zombie ──► parent calls wait() ──► gone
                                            ▲
                                  (stays here if the parent never waits)
```

## 3. The `#include` lines (the part that is easy to get wrong)

The slide lost its `#include` lines when it was exported. You need to know **which function lives in which header**. The compiler cannot guess it.

| You call | Header | Why |
|----------|--------|-----|
| `printf`, `perror` | `<stdio.h>` | Standard input/output |
| `exit` | `<stdlib.h>` | Standard library (exit, malloc, ...) |
| `fork`, `getpid`, `getppid`, `sleep`, `execlp` | `<unistd.h>` | POSIX: the Unix system-call wrappers |
| `pid_t` (the type) | `<sys/types.h>` | System data types |
| `wait` | `<sys/wait.h>` | Waiting for child processes |

For the slide program you need: `<stdio.h>`, `<unistd.h>`, `<sys/types.h>`, `<sys/wait.h>`.
For [fork10.c](fork10.c) you also need `<stdlib.h>` (for `exit`).

**Always put the headers in angle brackets** (`<...>`). Quotes (`"..."`) are for your own header files.

### How to find the right header yourself

Ask the manual. The header is in the **SYNOPSIS** section at the top:

```bash
man 2 fork     # press q to quit; look for:  #include <unistd.h>
man 2 wait     # look for:  #include <sys/wait.h>
man 3 exit     # look for:  #include <stdlib.h>
man 3 exec     # execlp is documented here
```

The number is the manual **section**: `2` = system calls, `3` = C library functions. (`man fork` alone also works; the number is only needed when a name exists in two sections.)
If `man` is missing: `sudo apt install man-db manpages-dev`.

### What a missing `#include` looks like

These are real messages from `gcc` (version 15). Older gcc versions show some of them as *warnings* instead of *errors*.

| If you forget | Message |
|---------------|---------|
| `<unistd.h>` | `error: implicit declaration of function 'fork'` |
| `<sys/types.h>` | `error: unknown type name 'pid_t'` |
| `<sys/wait.h>` | `error: implicit declaration of function 'wait'` |
| `<stdlib.h>` | `error: implicit declaration of function 'exit'` |

Rule: **"implicit declaration of function X"** means *you forgot the header of X*. Look up X with `man`.

## 4. Looking at processes: `ps` and `kill`

| Command | What it does |
|---------|--------------|
| `echo $$` | pid of the current shell (bash) |
| `ps` | processes of this terminal |
| `ps -f` | adds **PPID** (parent pid) and the command line |
| `ps -ef` / `ps aux` | every process on the machine |
| `ps --ppid 1234` | only the children of pid 1234 |
| `pstree -p 1234` | the family tree of pid 1234 (`sudo apt install psmisc` if missing) |
| `kill 1234` | ask pid 1234 to terminate (signal `SIGTERM`) |
| `kill -9 1234` | force it to die (signal `SIGKILL`; it cannot be ignored) |
| `pkill -P 1234` | kill all children of pid 1234 |

Example from our test (`fork10_ps` running in the background):

```
$ ps --ppid 1941 -o pid,ppid,stat,cmd
    PID    PPID STAT CMD
   1943    1941 S+   ./fork10_ps
   1944    1941 S+   ./fork10_ps
   ...
$ pstree -p 1941
fork10_ps(1941)-+-fork10_ps(1943)
                |-fork10_ps(1944)
                |-fork10_ps(1945)
```

All children have PPID = the parent's pid. The `S` in STAT means *sleeping* (they are in `sleep(30)`). A `Z` means *zombie*.

## 5. Concurrent vs. parallel

| | Concurrent | Parallel |
|---|-----------|----------|
| Meaning | Several tasks are **in progress** at the same time (they may take turns) | Several tasks **run at the same instant** |
| Needs | One CPU core is enough (time sharing) | **Two or more** cores |
| Analogy | One cook switching between 3 pots | 3 cooks, one pot each |

Parallel is one way of being concurrent. The 10 children of `fork10` are **always concurrent**; they run **in parallel** only if the CPU has more than one core (`nproc` shows how many).

This is why the output order is not fixed. The OS scheduler decides who gets a core first. Our test run printed child 4 after child 5, and child 1 after child 2. The task says exactly this: *"a child created later may print first, depending on which process is lucky enough to get the CPU first."* **That is not a bug.** Only `Parent terminates` must always be last, because the parent waits for all children.

## 6. Windows: `CreateProcess` and `WaitForSingleObject`

Windows has **no `fork()`**. It creates a process in one step with `CreateProcess` (it does `fork` and `exec` together: new process, new program). The waiting call is **`WaitForSingleObject`** (the slide writes "WaitForSignalObject", but the real API name is `WaitForSingleObject`).

| Idea | Linux | Windows (Win32) |
|------|-------|-----------------|
| Create a process | `fork()` then `exec...()` | `CreateProcess()` |
| Wait for a child | `wait()` | `WaitForSingleObject(handle, INFINITE)` |
| Identify a process | pid (a number) | a **handle** (`HANDLE`) and a process id |
| Clean up | `wait()` reaps the zombie | `CloseHandle()` on both handles |

```c
// Windows only. Build in a Visual Studio "x64 Native Tools" prompt:  cl win_create.c
#include <windows.h>
#include <stdio.h>

int main(void) {
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));  si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    char cmd[] = "C:\\Windows\\System32\\notepad.exe";   // must be writable memory

    if (!CreateProcess(NULL, cmd, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) {
        printf("CreateProcess failed (%lu)\n", GetLastError());
        return 1;
    }

    WaitForSingleObject(pi.hProcess, INFINITE);   // like wait(): block until notepad is closed
    printf("Child Complete\n");

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return 0;
}
```

> ⚠️ This Windows example was **not compiled** while writing this lab (the lab is Linux only). It follows the standard Microsoft pattern, but check it if you run it.

## 7. Walk through `fork10.c`

[fork10.c](fork10.c) creates 10 children. Each child prints its number and pid. The parent waits for all of them, then prints `Parent terminates`.

```c
for (int i = 0; i < n; i++) {
    pid_t pid = fork();
    if (pid < 0)        { perror("Fork failed"); return 1; }
    else if (pid == 0)  { printf("I'm the child number %d (pid %d)\n", i, getpid());
                          exit(0); }          // ← the child must stop here!
    // the parent continues the loop and creates the next child
}
for (int i = 0; i < n; i++) wait(NULL);       // 10 children → 10 waits
printf("Parent terminates (pid %d)\n", getpid());
```

| Detail | Why |
|--------|-----|
| `i` is the **child number** | The child is a copy, so it has its own `i`: the value at the time of the `fork`. The first child gets 0, the next 1, and so on |
| `exit(0)` in the child | Without it the child **continues the loop** and forks its own children. With `n = 10` you get 2¹⁰ = **1024** processes instead of 11 |
| Create all 10 first, **then** wait | The task says the parent creates all children first, so the children run concurrently. If you called `wait()` inside the first loop, the children would run one after another |
| `getpid()` | Returns the pid of the process that calls it, so each child prints its own pid |

Expected output (pids differ on every run; children may be out of order):

```
I'm the child number 0 (pid 25580)
I'm the child number 1 (pid 25581)
...
I'm the child number 9 (pid 25589)
Parent terminates (pid 25579)
```

### Common mistakes

| Mistake | What happens |
|---------|--------------|
| No `exit(0)` in the child | Process explosion: 2ⁿ processes (use a small `n` to test!) |
| `wait(NULL)` only once | Parent ends while 9 children are still running |
| Printing text **without** `\n` just before `fork()` | The text is still in the stdout buffer, so the child gets a **copy** and prints it twice. When output goes to a pipe or file, stdout is fully buffered, so this happens even with `\n`. Use `fflush(stdout)` before `fork()`, or end lines with `\n` |
| Using `pid` as your own pid | In the parent it is the child's pid. Use `getpid()` |
| Printing `Child Complete` without `\n` (as on the slide) | The shell prompt appears on the same line. Add `\n` |

## 8. Key words

| Term | Meaning |
|------|---------|
| **fork()** | Creates a child process that is a copy of the caller. Returns twice |
| **exec()** | Replaces the current program with another one (same pid) |
| **wait()** | Parent sleeps until a child terminates |
| **pid / ppid** | Process id / parent process id |
| **Parent / child** | The creator / the created process |
| **Zombie** | A finished child that its parent has not waited for yet |
| **Orphan** | A child whose parent ended first; the OS gives it a new parent |
| **Concurrent** | Several tasks in progress (can be on one core, taking turns) |
| **Parallel** | Several tasks running at the very same instant (needs several cores) |
| **CreateProcess / WaitForSingleObject** | Win32 API for creating a process and waiting for it |

## 9. Summary

> `fork()` copies the running process, and the return value (0 or the child's pid) tells you which copy you are. `exec()` replaces a process with a new program, and `wait()` lets the parent sleep until a child ends. Children run concurrently (in parallel on a multi-core CPU), so the print order is not fixed. Windows does it differently: `CreateProcess` plus `WaitForSingleObject`.
