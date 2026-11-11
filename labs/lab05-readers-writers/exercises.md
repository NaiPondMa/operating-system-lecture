# Lab 5 Exercises

Code goes in [main.cs](main.cs). Theory is in [lecture.md](lecture.md). How to run it is in the [README](README.md).

## Assignment

เขียนโปรแกรมดังนี้

- เริ่มต้น `x = 0`
- สร้าง thread มา N = 100 ตัว สุ่ม n = 5 ตัว ให้เป็น **writer** ที่เหลืออีก 95 ตัว ให้เป็น **reader**
- อย่าเพิ่ง start thread พร้อมกัน แต่ละ thread ให้สุ่มรอ 0.00 - 1.00 วินาที แล้วค่อย start thread
  (หาวิธีเขียนโปรแกรมเอง วัตถุประสงค์คือให้สุ่ม start thread ไม่ให้ start พร้อมกัน แต่ภายใน 1 วินาที ต้อง start thread ให้หมด)
- Reader อ่านค่า `x` ใช้เวลา 1 วินาที (sleep หรือ spinlock รอให้เวลาผ่านไป 1 วินาที)
- Writer เขียนค่า `x = x + 1` ใช้เวลา 1 วินาที (sleep หรือ spinlock รอให้เวลาผ่านไป 1 วินาที)
- ถ้าทำ synchronization ถูก โปรแกรมควรจะใช้เวลารัน 6 - 11 วินาทีเศษ และค่าสุดท้ายของ `x` คือ 5
  (ถ้ามี writer 2 ตัวเข้า critical section ติด ๆ กัน ก็จะใช้เวลาลดลง)
- เฉพาะ Writer ให้ print ข้อความดังนี้

```
no = 7
x = 1
no = 15
x = 2
no = 16
x = 3
no = 70
x = 4
no = 93
x = 5
elapsed time = 10.78 seconds
```

`no` คือลำดับที่เข้า critical section จาก 100 threads (ไม่ใช่ลำดับที่ create/start thread)

The program also asks for the **number of writers** (or takes `--writers W`), so you can change it without editing the code.

## Practice

1. **Run it with 5 writers a few times.** The times differ between runs. Why? What are the shortest and the longest time you can get with 5 writers? (Hint: think about when the 5 writers enter, one after another or one between two groups of readers.)
2. **Run with `--writers 0`, then `--threads 10 --writers 10`.** Predict the elapsed time before you run. Explain both numbers.
3. **Starvation.** Delete the `turnstile` lines in `Writer()` and `Reader()` and run with 5 writers. What are the `no` values now, and why? Which version of the problem did you just build (first or second)?
4. **Remove the lock on purpose.** Make `Writer()` skip `rwLock.Wait()` / `rwLock.Release()`. What happens to the final `x`, and which line of the self-check tells you?
5. **Why a semaphore?** Replace `rwLock` with `Mutex` or `lock`. What error do you get, and why? (Which thread takes `rwLock`, and which thread releases it?)

<details>
<summary>Answers</summary>

1. Shortest: all 5 writers enter one after another (5 s) with at least one group of readers (1 s) before or after: about 6-7 s. Longest: each writer sits between two groups of readers, so every writer costs 1 s and every reader group costs 1 s: about 11 s.
2. `--writers 0`: all readers start within 1 s and read together for 1 s, so about 2 s. 10 writers: they enter one by one, 10 x 1 s, so about 10 s.
3. Writers get `no` close to the end (for example 96-100) because there is always a reader inside while the readers arrive, so the writers starve. That is the first version (readers first).
4. Two writers read the same `x` (both see 3), both sleep, both write 4, so one `+1` is lost and `final x` is smaller than the number of writers. The line `final x = ... WRONG` and `rule violations > 0` show it.
5. The first reader takes `rwLock` and the last reader (another thread) releases it. `Mutex` and `lock` must be released by the thread that took them, so you get an exception (`SynchronizationLockException` / `ApplicationException`). A semaphore has no owner.

</details>
