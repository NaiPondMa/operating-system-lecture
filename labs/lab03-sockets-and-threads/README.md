# Lab 3: Sockets and Threads

**Topic:** how two processes talk to each other (IPC), and how one server serves many clients.
**Status:** ✅ lecture, code and exercises ready

## What you will learn

- A **socket** is an endpoint the OS gives to a process for IPC
- The order of system calls: `socket → bind → listen → accept → recv/send → close`
- Why **blocking** calls need **threads**
- Why shared data needs a **lock** (race condition, mutex)

## Files

| File | What it is |
|------|------------|
| [lecture.md](lecture.md) | Theory with diagrams. Read this first |
| [server.py](server.py) | Multi-threaded TCP server (one thread per client) |
| [client.py](client.py) | TCP client |
| [exercises.md](exercises.md) | How to run it, 5 experiments, and self-check questions |

## Run it

You need Python 3. Use two or three terminals in this folder.

```bash
python3 server.py     # terminal 1  (on Windows: python server.py)
python3 client.py     # terminal 2, then type: TIME, PID, UPPER hello, BYE
python3 client.py     # terminal 3, while client 1 is still connected
```

| Command | What the server replies |
|---------|-------------------------|
| `TIME` | Current date and time |
| `PID` | Server process ID and the name of the thread serving you |
| `UPPER <text>` | Your text in capital letters |
| `BYE` | Says goodbye and closes the connection |

Stop the server with **Ctrl+C**.

## Key words

IPC, socket, system call, blocking, thread, race condition, critical section, mutex.
See the [glossary](../../docs/glossary.md).
