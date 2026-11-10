# Glossary

Every key term from the labs in one place, in A–Z order. The **Lab** column shows where to read more.

| Term | Meaning | Lab |
|------|---------|:---:|
| **Blocking** | A call (like `accept()` or `recv()`) that sleeps until something happens | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Busy-wait (spin)** | Waiting by checking a condition again and again instead of sleeping; good for very short waits | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Child process** | A process created by another process (its parent), for example with `fork()` | [2](../labs/lab02-fork-client/lecture.md) |
| **Concurrent** | Several tasks are in progress at the same time; they may take turns on one core | [2](../labs/lab02-fork-client/lecture.md) |
| **Cooperative multi-tasking** | Programs must give up the CPU by themselves (Windows 3.x) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Critical section** | Code that touches shared data | [3](../labs/lab03-sockets-and-threads/lecture.md), [5](../labs/lab05-readers-writers/lecture.md) |
| **Data dependency** | A step needs the result of a~n earlier step, so the order matters (limits parallelism) | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Distro** | A packaged version of Linux (Ubuntu, Debian, Fedora, ...) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Dithering** | Showing a many-gray picture with only black and white dots | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Error diffusion** | Dithering that passes each pixel's rounding error to its neighbours (Floyd–Steinberg) | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **exec()** | Replaces the running program of a process with another program (same pid) | [2](../labs/lab02-fork-client/lecture.md) |
| **fork()** | Creates a child process that is a copy of the caller; returns the child's pid in the parent and 0 in the child | [2](../labs/lab02-fork-client/lecture.md) |
| **GIL** | Global Interpreter Lock: only one Python thread runs Python code at a time | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Guest OS** | The OS running inside a virtual machine | [1](../labs/lab01-linux-commands/lecture.md) |
| **Host OS** | The real OS running on the hardware | [1](../labs/lab01-linux-commands/lecture.md) |
| **IPC** | Inter-Process Communication: ways processes talk to each other through the OS | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Memory visibility (`Volatile`)** | Making sure a write by one thread is seen by others in the right order; C# `Volatile.Read/Write` | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Mutex** | A lock that lets only one thread into the critical section | [3](../labs/lab03-sockets-and-threads/lecture.md), [5](../labs/lab05-readers-writers/lecture.md) |
| **Mutual exclusion** | Only one process in the critical section at a time | [5](../labs/lab05-readers-writers/lecture.md) |
| **Nice value** | Process priority from -20 (highest) to 19 (lowest) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Orphan process** | A child whose parent ended first; the OS gives it a new parent | [2](../labs/lab02-fork-client/lecture.md) |
| **Parallel** | Several tasks run at the very same instant (needs two or more cores) | [2](../labs/lab02-fork-client/lecture.md) |
| **Parent process** | The process that created another process | [2](../labs/lab02-fork-client/lecture.md) |
| **pid / ppid** | Process id / the process id of the parent (`getpid()`, `getppid()`, shown by `ps -f`) | [2](../labs/lab02-fork-client/lecture.md) |
| **Pipe (`\|`)** | Sends the output of one command into the input of the next | [1](../labs/lab01-linux-commands/lecture.md) |
| **Pipeline** | Workers handle different stages or rows at the same time, each following the one before | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Preemptive multi-tasking** | The OS takes the CPU back with a timer interrupt (time sharing) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Process** | A running program with its own private memory | [1](../labs/lab01-linux-commands/lecture.md), [2](../labs/lab02-fork-client/lecture.md), [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Race condition** | The result depends on timing, so it can be wrong | [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-eror-diffusion/lecture.md), [5](../labs/lab05-readers-writers/lecture.md) |
| **Semaphore** | An OS tool that controls access to a shared resource | [5](../labs/lab05-readers-writers/lecture.md) |
| **Shared memory** | RAM that several processes can read and write | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Socket** | An OS endpoint for sending and receiving data | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Speed-up** | Sequential time divided by parallel time | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Starvation** | A process waits forever because others keep getting priority | [5](../labs/lab05-readers-writers/lecture.md) |
| **Symbolic link** | A shortcut to a file or folder (`ln -s`) | [1](../labs/lab01-linux-commands/lecture.md) |
| **System call** | A request from a program to the OS | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Thread** | A light-weight process inside a process; threads share memory | [1](../labs/lab01-linux-commands/lecture.md), [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Time sharing** | Giving each process a short time slice of the CPU in turn | [1](../labs/lab01-linux-commands/lecture.md) |
| **Timer interrupt** | Hardware signal that lets the OS take the CPU back | [1](../labs/lab01-linux-commands/lecture.md) |
| **wait() / WaitForSingleObject()** | The parent sleeps until a child terminates (Linux / Win32 API) | [2](../labs/lab02-fork-client/lecture.md) |
| **Wavefront** | A slanted line of independent items that sweeps over data, so each line can run in parallel | [4](../labs/lab04-eror-diffusion/lecture.md) |
| **Zombie** | A child that has terminated but whose parent has not called `wait()` yet (`Z` in `ps`) | [2](../labs/lab02-fork-client/lecture.md) |
