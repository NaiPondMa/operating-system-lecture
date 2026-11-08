# Lab 5: The Readers–Writers Problem

**Topic:** process synchronization. Many readers may share data, but a writer needs it alone.
**Status:** 🚧 lecture ready. Assignment text ([exercises.md](exercises.md)) and code ([main.cs](main.cs)) are not added yet

## What you will learn

- The rules: many readers at once, only one writer, and no readers while a writer writes
- The first version (readers first) and the second version (writers first)
- **Starvation**, and which side can starve in each version
- How `read_count`, a `mutex` and an `rw_lock` solve the problem

## Files

| File | What it is |
|------|------------|
| [lecture.md](lecture.md) | Theory with a real-life picture, a timeline, and the solution in plain words |
| [exercises.md](exercises.md) | The assignment instructions (to be added) |
| [main.cs](main.cs) | C# starter/solution file (to be added) |

## How to study

1. Read [lecture.md](lecture.md) sections 1–6 to understand the problem.
2. Answer the questions in section 7 *before* you write code.
3. Use the plan in section 8 for your solution.

## Key words

Critical section, mutual exclusion, race condition, semaphore, mutex, starvation.
See the [glossary](../../docs/glossary.md).
