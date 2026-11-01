# The Readers–Writers Problem

## 1. The problem in one sentence

Many processes share **one piece of data**. Some only **read** it, and some **change** it.
How do we let them work at the same time **without corrupting the data**?

---

## 2. A simple real-life picture

Think of a **whiteboard in a classroom**.

- 👀 **Readers** are students who only *look* at the board.
  Twenty students can look at once, and nobody gets hurt.
- ✍️ **Writers** are teachers who *erase and write* on the board.
  While a teacher is writing, nobody else should touch the board. Students reading
  half-erased text would get wrong information, and two teachers writing at once
  would make a mess.

---

## 3. The two kinds of processes

| Type | What it does | Can share with readers? | Can share with writers? |
|------|--------------|-------------------------|-------------------------|
| **Reader** | Only reads the data | ✅ Yes, any number at once | ❌ No |
| **Writer** | Reads **and** modifies the data | ❌ No | ❌ No |

### The rules (what must always be true)

1. **Many readers** may read at the same time.
2. **Only one writer** may write at a time.
3. While a writer is writing, **no readers** are allowed in.

> In short: a writer needs **exclusive access**. Readers only need to keep writers out.

---

## 4. Why is this hard?

If we used one simple lock for everybody, it would be **safe but slow**.
Readers would wait for each other even though reading together is harmless.

If we used no lock at all, it would be **fast but unsafe**.
A reader might see data while a writer is halfway through changing it, which
gives an **inconsistent or corrupted** result (a *race condition*).

The goal is the middle ground: **readers share and writers are exclusive**.

---

## 5. The variations: who gets priority?

When readers and writers both want the data, someone has to wait.
Which side wins gives us different versions of the problem.

### First readers–writers problem (readers have priority)

- A reader **never waits** just because a writer is waiting.
- A reader waits only if a writer is **already writing**.
- ⚠️ **Risk:** if readers keep arriving, the writer may wait **forever**.
  This is called **writer starvation**.

### Second readers–writers problem (writers have priority)

- Once a writer is **waiting**, **no new readers** may start.
- The writer gets in as soon as the current readers finish.
- ⚠️ **Risk:** if writers keep arriving, readers may wait **forever**.
  This is called **reader starvation**.

| Version | Who is favored | Who can starve |
|---------|----------------|----------------|
| First | Readers | Writers |
| Second | Writers | Readers |

> There is also a fair (third) version, where nobody starves. Requests are served
> roughly in arrival order.

---

## 6. Timeline example

```
Time →
R1: [====read====]
R2:    [====read====]          ← OK: readers overlap
W1:                  [==write==]          ← waits until R1 and R2 finish
R3:                             [==read==] ← waits until W1 finishes
```

---

## 7. Questions to answer before coding

Before you write the solution, be sure you can answer these:

1. **What is shared?** (for example, a variable, a file, or a list in memory)
2. **How do I know how many readers are inside right now?**
   → I need a counter, such as `read_count`.
3. **That counter is also shared.** How do I protect it?
   → I need a small lock (a mutex) just for the counter.
4. **How do I keep writers out while readers are inside?**
   → The **first** reader to enter locks out writers.
   The **last** reader to leave lets writers back in.
5. **How does a writer get exclusive access?**
   → I need a lock or semaphore that both writers and the "reader group" use.
6. **Which version am I solving?** Do readers or writers get priority?
   Is starvation acceptable for my program?

---

## 8. The tools you will usually need

| Tool | Purpose |
|------|---------|
| `read_count` (integer) | How many readers are currently reading |
| `mutex` (lock) | Protects `read_count` when readers update it |
| `rw_lock` (semaphore or lock) | Gives writers exclusive access; the first reader takes it and the last reader releases it |

### The idea in plain words (first readers–writers version)

**Writer**
1. Lock `rw_lock`
2. Write the data
3. Unlock `rw_lock`

**Reader**
1. Lock `mutex` and add 1 to `read_count`. If I am the **first** reader, lock `rw_lock` to keep writers out. Unlock `mutex`.
2. Read the data. Other readers can be reading too.
3. Lock `mutex` and subtract 1 from `read_count`. If I am the **last** reader, unlock `rw_lock` so writers can come in. Unlock `mutex`.

---

## 9. Key words to remember

- **Critical section**: code that touches shared data.
- **Mutual exclusion**: only one process in the critical section at a time.
- **Race condition**: the result depends on the timing of processes, so it can be wrong.
- **Semaphore / Mutex**: tools the OS provides to control access.
- **Starvation**: a process waits forever because others keep getting priority.

---

## 10. Summary

> **Readers can share. Writers must be alone.**
> The challenge is to enforce that rule **safely** and **efficiently**,
> and to decide **who gets priority** without letting anyone wait forever.