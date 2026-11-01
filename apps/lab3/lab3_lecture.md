# Lecture: How Do Two Processes Talk to Each Other?

> Companion notes for `server.py` and `client.py`

---

## 1. The big idea

- Every running program is a **process**.
- The OS keeps processes **separated**, so one process **cannot read another's memory**.
- To talk, processes must **ask the OS for help**.
  This is called **IPC (Inter-Process Communication)**.

### What is a socket?

A **socket** is one kind of IPC. It is an endpoint the OS gives to a process.

> 📞 **Analogy:** A socket is like a **phone the OS lends you**.
> Each process holds one phone, and they talk through it.
> It works on the **same machine** or **across a network**.

```
 ┌───────────┐                         ┌───────────┐
 │ Process A │                         │ Process B │
 │ (Client)  │                         │ (Server)  │
 └─────┬─────┘                         └─────┬─────┘
       │ socket                       socket │
       └──────────────►  OS  ◄───────────────┘
                   (carries data)
```

---

## 2. The order of system calls

This is the diagram professors almost always draw on the board:

```
        SERVER                          CLIENT
      ──────────                      ──────────
      socket()                        socket()
      bind()     ← choose port
      listen()   ← open for calls
      accept()   ← waits (blocks)  ◄── connect()   (TCP 3-way handshake)
      recv()     ◄──────────────────── send()
      send()     ────────────────────► recv()
      close()                         close()
```

### What each call does

| Step | System call | Who uses it | Meaning |
|:----:|-------------|-------------|---------|
| 1 | `socket()`  | Both   | Ask the OS for a new endpoint |
| 2 | `bind()`    | Server | Attach the socket to an address and port |
| 3 | `listen()`  | Server | Start accepting connections (set up a waiting queue) |
| 4 | `accept()`  | Server | Wait for a client, then get a **new socket** for that client |
| – | `connect()` | Client | Call the server (TCP 3-way handshake: SYN → SYN-ACK → ACK) |
| 5 | `send()` / `recv()` | Both | Exchange data |
| 6 | `close()`   | Both   | Give the socket back to the OS |

> 💡 The comments in the code are numbered **1, 2, 3…** to match this diagram.

---

## 3. Why threads?

### The problem: blocking calls

`accept()` and `recv()` are **blocking** calls.
The process **sleeps** until something happens (a client connects or data arrives).

With **one thread only**:

```
Client 1 connects → server waits in recv() for Client 1
Client 2 connects → ❌ stuck, nobody is calling accept()
```

### The solution: one thread per client

For every client, the server creates a new thread:

```python
t = threading.Thread(target=handle_client, args=(conn, addr))
t.start()
```

```
                 ┌──────────────── ONE PROCESS ────────────────┐
                 │                                             │
                 │  Main thread ── accept() ── accept() ── ... │
                 │        │             │                      │
                 │        ▼             ▼                      │
 Client 1 ◄────► │    Thread-1      Thread-2  ◄────────────────┼────► Client 2
                 │                                             │
                 │        shared memory (client_count)         │
                 └─────────────────────────────────────────────┘
```

- All threads live **inside the same process**.
- All threads **share the same memory**.

---

## 4. Why a lock?

### The problem: race condition

Every thread changes the shared variable `client_count`.
`client_count += 1` looks like one step, but it is really **three**:

1. **Read** the value
2. **Add** 1
3. **Write** it back

If two threads do this at the same moment:

| Time | Thread-1 | Thread-2 | `client_count` |
|:----:|----------|----------|:--------------:|
| t1 | read → 0 |          | 0 |
| t2 |          | read → 0 | 0 |
| t3 | write 1  |          | 1 |
| t4 |          | write 1  | **1** ❌ (should be 2) |

One update is **lost**. This is a **race condition**.

### The solution: a mutex

```python
with count_lock:          # 🔒 only one thread at a time
    client_count += 1     # critical section
                          # 🔓 lock released automatically
```

- The code inside `with count_lock:` is the **critical section**.
- The lock is a **mutex** (mutual exclusion), so **only one thread** can be inside at a time.

---

## 5. Key words

| Term | Meaning |
|------|---------|
| **Process** | A running program with its own private memory |
| **IPC** | Ways processes communicate through the OS |
| **Socket** | An OS endpoint for sending and receiving data |
| **System call** | A request from a program to the OS |
| **Blocking** | The call sleeps until something happens |
| **Thread** | A unit of execution inside a process; threads share memory |
| **Race condition** | The result depends on timing, so it can be wrong |
| **Critical section** | Code that touches shared data |
| **Mutex** | A lock that lets only one thread into the critical section |

---

## 6. Summary

> 1. Processes can't share memory, so they talk through the OS using **sockets**.
> 2. The server follows **socket → bind → listen → accept**, and the client follows **socket → connect**.
> 3. **Threads** let the server handle many clients at the same time.
> 4. A **lock** protects shared data from **race conditions**.