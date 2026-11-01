# Glossary

Every key term from the labs in one place, in A–Z order. The **Lab** column shows where to read more.

| Term | Meaning | Lab |
|------|---------|:---:|
| **Blocking** | A call (like `accept()` or `recv()`) that sleeps until something happens | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Cooperative multi-tasking** | Programs must give up the CPU by themselves (Windows 3.x) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Critical section** | Code that touches shared data | [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-readers-writers/lecture.md) |
| **Distro** | A packaged version of Linux (Ubuntu, Debian, Fedora, ...) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Guest OS** | The OS running inside a virtual machine | [1](../labs/lab01-linux-commands/lecture.md) |
| **Host OS** | The real OS running on the hardware | [1](../labs/lab01-linux-commands/lecture.md) |
| **IPC** | Inter-Process Communication: ways processes talk to each other through the OS | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Mutex** | A lock that lets only one thread into the critical section | [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-readers-writers/lecture.md) |
| **Mutual exclusion** | Only one process in the critical section at a time | [4](../labs/lab04-readers-writers/lecture.md) |
| **Nice value** | Process priority from -20 (highest) to 19 (lowest) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Pipe (`\|`)** | Sends the output of one command into the input of the next | [1](../labs/lab01-linux-commands/lecture.md) |
| **Preemptive multi-tasking** | The OS takes the CPU back with a timer interrupt (time sharing) | [1](../labs/lab01-linux-commands/lecture.md) |
| **Process** | A running program with its own private memory | [1](../labs/lab01-linux-commands/lecture.md), [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Race condition** | The result depends on timing, so it can be wrong | [3](../labs/lab03-sockets-and-threads/lecture.md), [4](../labs/lab04-readers-writers/lecture.md) |
| **Semaphore** | An OS tool that controls access to a shared resource | [4](../labs/lab04-readers-writers/lecture.md) |
| **Socket** | An OS endpoint for sending and receiving data | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Starvation** | A process waits forever because others keep getting priority | [4](../labs/lab04-readers-writers/lecture.md) |
| **Symbolic link** | A shortcut to a file or folder (`ln -s`) | [1](../labs/lab01-linux-commands/lecture.md) |
| **System call** | A request from a program to the OS | [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Thread** | A light-weight process inside a process; threads share memory | [1](../labs/lab01-linux-commands/lecture.md), [3](../labs/lab03-sockets-and-threads/lecture.md) |
| **Time sharing** | Giving each process a short time slice of the CPU in turn | [1](../labs/lab01-linux-commands/lecture.md) |
| **Timer interrupt** | Hardware signal that lets the OS take the CPU back | [1](../labs/lab01-linux-commands/lecture.md) |
