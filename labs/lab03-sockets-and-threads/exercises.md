# Lab 3 Exercises: Let's Play with the Server

> Read [lecture.md](lecture.md) first. Code: [server.py](server.py) and [client.py](client.py).

---

## Part 1: Run it

Open **three terminal windows** in this folder.

| Terminal | Command | What to do |
|:--------:|---------|------------|
| 1 | `python3 server.py` | Leave it running |
| 2 | `python3 client.py` | Type `TIME`, `PID`, `UPPER hello`, then `BYE` |
| 3 | `python3 client.py` | Start it **while client 1 is still connected** |

> 🪟 On Windows, use `python` instead of `python3`.

### What to notice

- Type `PID` in **both** clients. You get the **same process ID** but **different thread names**.
  This shows that **one process** serves both clients using **different threads**.
- The server terminal prints `[ACTIVE THREADS]` as clients join.

---

## Part 2: Experiments (the "homework")

Change the code, run it, and watch what happens. Try to guess the result *before* you run it.
Put the code back after each experiment (`git checkout server.py` undoes your changes).

### Experiment 1: Break concurrency

In `server.py`, replace `t.start()` with a direct call: `handle_client(conn, addr)`.
Connect two clients. The second one gets **stuck** until the first one types `BYE`.

**Question:** why?

<details><summary>Answer</summary>

Without a thread, the **main thread** runs `handle_client()` itself and sits inside `recv()` for client 1.
It never goes back to `accept()`, so nobody can pick up client 2.
Threads let the main thread return to `accept()` right away.
</details>

### Experiment 2: Watch blocking

Start a client but **don't type anything**. The server thread is sleeping inside `recv()`.

On Linux, run `ps -eLf | grep server.py` to see the threads (`L` = light-weight processes).
(On Windows, use WSL for this command.)

<details><summary>What you should see</summary>

One line per thread, all with the **same PID** but different **LWP** numbers.
The thread waiting in `recv()` uses no CPU. It is **blocked**, and the OS wakes it when data arrives.
</details>

### Experiment 3: See the race condition

1. Remove the `with count_lock:` line (keep the code inside, and un-indent it).
2. Un-comment `time.sleep(0.1)` between reading and writing `client_count`.
3. Connect several clients quickly.

**Question:** do some clients get the **same number**?

<details><summary>Answer</summary>

Yes. `client_count += 1` is really **read → add → write**. With the sleep, several threads read the old value
before any of them writes, so they all write the same new value and updates are **lost**.
This is a **race condition**. The lock makes the three steps happen as one unit. See
[lecture.md, section 4](lecture.md#4-why-a-lock).
</details>

### Experiment 4: Port conflict

Start a **second server** while the first is still running. You will get `Address already in use`.

<details><summary>Why?</summary>

Only **one socket** can `bind()` a given address and port. The OS refuses the second one.
</details>

### Experiment 5: Threads vs. processes

Swap `threading.Thread` for `multiprocessing.Process`. Now `PID` shows a **different process ID** for each client.

**Question:** what are the trade-offs in memory and in sharing data?

<details><summary>Answer</summary>

| | Threads | Processes |
|---|---------|-----------|
| Memory | Light: share one address space | Heavier: each has its own copy |
| Sharing data | Easy (but needs locks) | Hard: needs IPC (pipes, shared memory, ...) |
| Safety | One crash can kill all threads | One crash stays inside that process |

With processes, `client_count` is **not shared** any more, so each client may see "client #1".
</details>

---

## Part 3: Self-check questions

1. What is the difference between the socket from `socket()` and the one returned by `accept()`?
   <details><summary>Answer</summary>

   The socket from `socket()` is the **listening socket**. It only waits for new clients.
   `accept()` returns a **new socket** connected to **one specific client**, and the listening socket stays open for the next one.
   </details>

2. Why does `recv()` returning empty bytes mean the client disconnected?
   <details><summary>Answer</summary>

   When the other side closes a TCP connection, there will never be more data. `recv()` reports this as **0 bytes** (end of stream).
   A client that is just quiet makes `recv()` **block**; it does not return empty.
   </details>

3. TCP (`SOCK_STREAM`) or UDP (`SOCK_DGRAM`): which one needs `listen()` / `accept()`, and why?
   <details><summary>Answer</summary>

   **TCP.** It is connection-oriented, so the server must accept a connection (the 3-way handshake) before data is sent.
   UDP is connectionless: you just `bind()` and then `recvfrom()` / `sendto()`.
   </details>

---

## Part 4: Go further (optional)

- Write a **UDP version** of the server and client.
- Write a **`fork()`-based version** (one process per client) and compare it with the thread version.
