# Lab 2 Exercises: Create Processes with fork()

> Read [lecture.md](lecture.md) first. Code: [fork10.c](fork10.c), [fork_exec.c](fork_exec.c), [fork10_ps.c](fork10_ps.c).

---

## Assignment

**Activity #2 (Linux only).** Change the slide program so that it creates **10 children**. Each child prints:

```
I'm the child number 0 (pid 25580)
I'm the child number 1 (pid 25581)
...
I'm the child number 9 (pid 25589)
Parent terminates (pid 25579)
```

Rules from the slide:

- The parent creates **all 10 children first**, then waits until all 10 have terminated. Only then does the parent terminate.
- The first child is number 0, then 1, 2, ... in creation order.
- The children may print **out of order** (whichever process gets the CPU first). That is allowed.
- Hints: `sleep(1)` waits 1 second, `getpid()` returns the pid, `getppid()` returns the parent's pid.
- Extra: Linux creates processes with `fork`, so run `ps` and look at the pid of `bash`, then write a program that prints the pid of its **parent**.

---

## Part 0: Set up (once)

The programs need Linux. On Windows use **WSL**:

```powershell
wsl --install            # once, in an administrator PowerShell; restart if asked
wsl                      # open the Ubuntu shell
```

Then inside Linux install the compiler and the manual pages:

```bash
sudo apt update
sudo apt install gcc man-db manpages-dev psmisc     # psmisc gives you pstree
gcc --version                                         # must print a version
```

Windows drives are under `/mnt/`, so this folder is at `/mnt/d/Github/Operating_System/labs/lab02-fork-client` (change the drive letter if yours differs):

```bash
cd /mnt/d/Github/Operating_System/labs/lab02-fork-client
```

---

## Part 1: Run it

### 1a. The slide program (fork + exec + wait)

```bash
gcc fork_exec.c -o fork_exec
./fork_exec
```

Expected: the file list of the folder (that is `ls`), then `Child Complete`.

Questions: Which process runs `ls`? Why does `Child Complete` come **after** the list?

<details><summary>Answer</summary>

The **child** runs `ls` (the `pid == 0` branch calls `execlp`). The parent is in `wait(NULL)` and prints `Child Complete` only after the child has terminated.
</details>

### 1b. The assignment: 10 children

```bash
gcc -Wall -Wextra fork10.c -o fork10
./fork10
./fork10        # run it again
```

Run it **several times**. Fill in:

| Run | Same child order as run 1? | Same pids? |
|:---:|:--------------------------:|:----------:|
| 1 | n/a | n/a |
| 2 | | |
| 3 | | |

<details><summary>Answer</summary>

The pids are **different every run** (the OS hands out new numbers). The order of the children is often 0, 1, 2, ... but sometimes not (in our test child 2 printed before child 1, and child 5 before child 4). `Parent terminates` is **always last**, because the parent waits for all 10 children first.
</details>

### 1c. Look at the processes with `ps` and `kill`

`fork10` ends too quickly to see. [fork10_ps.c](fork10_ps.c) keeps each child alive for 30 seconds and also prints the parent's pid.

```bash
gcc -Wall -Wextra fork10_ps.c -o fork10_ps
./fork10_ps &          # & = run in the background; it prints the parent's pid first
echo $$                # the pid of your bash
ps -f                  # look at the PID and PPID columns
ps --ppid <parent pid> # only the 10 children (replace <parent pid>)
pstree -p <parent pid> # the family tree
kill <one child pid>   # kill one child; run ps --ppid again: 9 left
pkill -P <parent pid>  # kill the rest; the parent then terminates by itself
```

Fill in:

| Question | Your answer |
|----------|-------------|
| pid of `bash` (`echo $$`) | |
| `Parent started` line: its parent pid | |
| PPID of the 10 children | |

<details><summary>Answer</summary>

The parent's parent pid equals the pid of `bash` (the shell started the program). The 10 children all have the **parent's pid** as PPID. When you `kill` one child, the parent's `wait()` returns for it, and the parent keeps waiting for the other 9. After the last child is gone, the parent prints `Parent terminates`.
</details>

---

## Part 2: Experiments

Guess the result **before** you run it.

### Experiment 1: Forget `exit(0)` (use n = 3, not 10!)

Copy `fork10.c` to a new file, set `int n = 3;`, delete the `exit(0);` line, and add `fflush(stdout);` after the `printf`. Run it.
How many `I'm the child` lines do you expect? How many processes exist at the end?

> ⚠️ Keep `n` small. With `n = 10` and no `exit` you would create 1024 processes.

<details><summary>Answer</summary>

**7 lines** and **8 processes** (2³). Each child continues the loop and forks its own children, so the number of processes doubles with every iteration. With `n = 10` it would be 1024 processes. This is why a child must `exit` (or `exec`) when it is done.
</details>

### Experiment 2: Without the `fflush`

Do Experiment 1 again **without** the `fflush(stdout)` and run `./a.out | wc -l`.

<details><summary>Answer</summary>

You get **more** than 7 lines (we got 12). When stdout is a pipe, it is fully buffered, so the text of earlier children is still waiting in the buffer when the next `fork()` copies it. Each copy then prints it again. The lesson: buffered output + `fork()` can duplicate text. Flush before you fork.
</details>

### Experiment 3: Remove the waiting

In `fork10.c` delete the second loop (the 10 `wait(NULL)` calls) and add `sleep(1);` before `exit(0)` in the child. Run it.

<details><summary>Answer</summary>

The parent prints `Parent terminates` **first**, and the shell prompt comes back before the children print. The children become **orphans**: when they print, their parent is no longer the original parent (on many systems it is pid 1; in our WSL test it was another system process). Check it by also printing `getppid()` in the children.
</details>

### Experiment 4: A zombie

Write a program where the child calls `exit(0)` immediately, and the parent calls `sleep(30)` **before** `wait(NULL)`. In a second terminal run `ps -f` (or `ps -o pid,ppid,stat,cmd --ppid <parent pid>`).

<details><summary>Answer</summary>

The child shows `Z` in the STAT column (and `<defunct>` as the command). It has finished, but the parent has not collected its exit status yet. When the parent calls `wait()`, the zombie disappears.
</details>

### Experiment 5: Make the order visible

Put `sleep(1)` in the **parent's** loop, after each `fork()`. Then compare the order of the output with the version without it.

<details><summary>Answer</summary>

The children now print in order 0, 1, 2, ... because every child has a full second to run before the next one is created. Without `sleep` all 10 are created almost at once, and they compete for the CPU. This shows that the order depends on scheduling, not on the program.
</details>

### Experiment 6: Exec in the loop

Change the child so that, instead of printing, it runs `execlp("/bin/echo", "echo", "hello from child", NULL);`.

<details><summary>Answer</summary>

You see 10 lines `hello from child`. The child process is replaced by `/bin/echo`, so the `exit(0)` after it is never reached when `exec` works (keep it anyway for the case that `exec` fails).
</details>

---

## Part 3: Self-check questions

1. `fork()` is called once. How many times does it return?
   <details><summary>Answer</summary>

   **Twice**: once in the parent (the child's pid) and once in the child (0).
   </details>

2. After `pid_t pid = fork();`, what does `pid` hold in the parent? In the child?
   <details><summary>Answer</summary>

   Parent: the **child's** pid. Child: **0**. (If `fork` failed: `-1`, in the parent only.)
   </details>

3. How does a process get its own pid and its parent's pid?
   <details><summary>Answer</summary>

   `getpid()` and `getppid()`.
   </details>

4. What is the difference between `fork()` and `exec()`?
   <details><summary>Answer</summary>

   `fork()` creates a **new process** that is a copy of the old one. `exec()` creates **no** process; it replaces the program of the **current** process with a new program (same pid).
   </details>

5. Why do we write `exit(0)` in the child of `fork10.c`?
   <details><summary>Answer</summary>

   Otherwise the child would continue the `for` loop and create its own children, so you would get 2ⁿ processes instead of n + 1.
   </details>

6. Why must the parent call `wait` 10 times?
   <details><summary>Answer</summary>

   One `wait()` waits for **one** child. For 10 children you need 10 calls (or a loop that stops when `wait` returns -1).
   </details>

7. Why do the children not always print in the order 0, 1, 2, ...?
   <details><summary>Answer</summary>

   The children run concurrently, and the scheduler decides which one gets a CPU core first. On a multi-core machine they can even print at the same time.
   </details>

8. What is the difference between concurrent and parallel? Which one are your 10 children on a 1-core machine?
   <details><summary>Answer</summary>

   Concurrent = several tasks in progress, possibly taking turns. Parallel = several tasks running at the same instant (needs several cores). On 1 core they are concurrent but **not** parallel.
   </details>

9. gcc says `implicit declaration of function 'wait'`. What did you forget?
   <details><summary>Answer</summary>

   `#include <sys/wait.h>`. Find it with `man 2 wait` (see SYNOPSIS).
   </details>

10. What is the Windows replacement for `fork()` + `exec()` and for `wait()`?
    <details><summary>Answer</summary>

    `CreateProcess()` creates the process and loads the program in one call. `WaitForSingleObject(handle, INFINITE)` waits for it (the slide spells it "WaitForSignalObject").
    </details>

11. What is a zombie process, and how do you get rid of it?
    <details><summary>Answer</summary>

    A child that has terminated but whose parent has not called `wait()`. It disappears when the parent calls `wait()` (or when the parent terminates).
    </details>

12. Which `ps` option shows the parent pid of every process?
    <details><summary>Answer</summary>

    `ps -f` (the PPID column), or `ps -ef`. To see only the children of one process: `ps --ppid <pid>`.
    </details>
