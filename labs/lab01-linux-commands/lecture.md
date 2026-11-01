# Lab 1 / Assignment #1: Linux Commands Study Guide

> Course: 2301274 Computer Systems (OS), based on the Chapter 1 slides
> Goal: be ready for the **Linux commands quiz**

---

## 0. How the quiz works

- The quiz picks **5 random tasks** from the lists below.
- You get **10 minutes**.
- Open **5 terminals** and do **one task per terminal**, so the teacher can check each one.
- The quiz runs in rounds (9:00, 9:15, 9:30, …). You may **not** borrow a computer from a friend in the same round.

> 💡 **Tip:** 10 minutes for 5 tasks means about **2 minutes per task**.
> You must know these commands *from memory*. Practice each one at least twice.

### Before the quiz, install these packages (Ubuntu/Debian)

```bash
sudo apt update
sudo apt install wamerican openssh-server ftp at zip unzip build-essential ncal plocate
```

| Package | Needed for |
|---------|------------|
| `wamerican` | the file `/usr/share/dict/american-english` |
| `openssh-server` | `ssh` / `sftp` to your own machine |
| `ftp` | the `ftp` command |
| `at` | scheduling a download for 1:00 AM |
| `zip`, `unzip` | making `.zip` files |
| `build-essential` | `gcc` to compile C |
| `ncal` | the `cal` command |
| `plocate` | the `locate` command (optional) |

---

## 1. Background theory (from the slides)

### 1.1 What is an operating system?

```
 User program   User program   User program
        \             |             /
         ┌──────────────────────────┐
         │     Operating System     │  → manages resources
         │                          │  → provides services
         └──────────────────────────┘
        /        |          |        \
     CPU      Memory     Storage    I/O devices
```

The OS sits between **user programs** and the **hardware**. It has two jobs:
1. **Manage resources**: CPU, memory, storage, I/O devices
2. **Provide services** to programs (for example, system calls)

### 1.2 Types of operating systems

| Type | Idea | Example |
|------|------|---------|
| **No OS** | Load the program into memory, set the program counter (PC), run, halt | A small robot or microcontroller |
| **Batch processing** | Jobs (input + job control file + output) wait in a **job queue** and run one after another | IBM mainframes (banks, airlines) |
| **Single-tasking** | Only one program runs at a time | DOS |
| **Multi-tasking (cooperative)** | Programs must **voluntarily give up** the CPU | Windows 3.x |
| **Time-sharing (preemptive)** | The OS **takes the CPU back** using a **timer interrupt** | Windows 95+, Linux |

### 1.3 Cooperative vs. preemptive multi-tasking

| | Cooperative | Preemptive (time sharing) |
|---|---|---|
| Who switches processes? | The **program** gives up the CPU | The **OS** takes the CPU back |
| How? | The program checks a message queue and yields | A **timer interrupt** after each **time slice** |
| ✅ Advantage | Reduces time wasted on I/O waiting | Quick response for all users/processes |
| ❌ Disadvantage | One long program can **block everyone** | Needs **timer interrupt hardware** in the CPU |
| Example | Windows 3.1 | Windows 95, Linux |

**How a timer interrupt works:**
1. The OS sets a timer interrupt, then jumps to process P1.
2. The timer fires, and the OS **takes the CPU back**.
3. The OS sets the timer again, then jumps to process P2, and so on.

> Fun fact from the slides: early iOS was **not** time-sharing, so music stopped when you switched to the web browser.

### 1.4 OS history on PCs

| OS | Key points |
|----|------------|
| **DOS** (Disk Operating System) | Single-user, single-task, text only (no GUI), small enough to fit on a floppy disk |
| **Windows 3.0 / 3.1 / 3.11** | GUI + **cooperative** multi-tasking |
| **Windows 95 and later** | **Preemptive** multi-tasking (time sharing) |
| **Unix / Linux** | Preemptive multi-tasking; GUI is **optional**. GNU = "GNU is Not Unix" |

### 1.5 Process vs. thread

- **Process**: a program that is **loaded into memory and being executed**.
  One program can create **many processes** (for example, opening Word several times).
- **Thread**: a **light-weight process** that runs *inside* a process.
  For example, a web browser downloading several files at the same time.
- ⚠️ There is **no memory protection between threads** of the same process. They share memory.

```
 ┌──────── Process P1 ────────┐        Memory
 │   T1        T2        T3   │      ┌──────────┐
 │  (threads share memory)    │      │  free    │
 └────────────────────────────┘      │ process B│
                                     │ process A│
                                     │  kernel  │
                                     └──────────┘
```

### 1.6 Virtual machines (VirtualBox)

```
 Guest OS:   Linux (OS1)   Linux (OS2)   Windows (OS3)
               VM1           VM2           VM3
          ┌───────────────────────────────────────┐
          │      VirtualBox (a user program)      │   user program   user program
          └───────────────────────────────────────┘
          ┌───────────────────────────────────────────────────────────────┐
          │              Host OS (Windows / Mac)                          │
          └───────────────────────────────────────────────────────────────┘
                                   Hardware
```

- **Host OS**: the real OS on your computer.
- **Guest OS**: the OS running inside a virtual machine.
- If you don't use VirtualBox, you can use **WSL** (Windows Subsystem for Linux) or **UTM** (Mac).
- A **distro** (distribution) is a packaged version of Linux, such as Ubuntu, Debian, Fedora or Mint.

---

## 2. Process management

| Task | Command(s) |
|------|------------|
| Show Linux distro (distributor, description, release, codename) | `lsb_release -a` |
| Show kernel version | `uname -r` (or `uname -a` for everything) |
| Show kernel file and its size | `ls -lh /boot/vmlinuz-$(uname -r)` |
| Show CPU information | `lscpu` or `cat /proc/cpuinfo` |
| Run a process with **lowest** priority | `nice -n 19 sleep 100 &` |
| Run a process (> 60 s), pause, and resume | `sleep 100` → **Ctrl+Z** (pause) → `fg` (resume) |
| Run a process (> 60 s) in background, and terminate | `sleep 100 &` → `kill %1` |
| Run 3 processes in background, bring the 2nd to foreground | `sleep 100 &` (×3) → `jobs` → `fg %2` |
| Run 3 processes in background, terminate the 1st | `sleep 100 &` (×3) → `jobs` → `kill %1` |
| Connect two processes with a pipe | `cat /etc/passwd \| grep home` |
| Show uptime | `uptime` (or `uptime -p` for "up 2 hours") |

### Explanations

**Priority with `nice`**
- The nice value goes from **-20** (highest priority, most favorable) to **19** (lowest priority, least favorable).
- A **normal user** can only make a process *nicer* (higher number). Going below 0 needs `sudo`.
- Check it with `ps -l` (look at the **NI** column) or `top`.

```bash
nice -n 19 sleep 100 &
ps -l          # NI column shows 19
```

**Job control**

| Key / command | Meaning |
|---------------|---------|
| `command &` | Run in the **background** |
| **Ctrl+Z** | **Pause** (stop) the foreground process |
| **Ctrl+C** | **Kill** the foreground process |
| `jobs` | List background jobs with numbers `[1]`, `[2]`, `[3]` |
| `fg %n` | Bring job *n* to the **foreground** (also resumes it) |
| `bg %n` | Resume job *n* in the **background** |
| `kill %n` | Terminate job *n* |
| `kill PID` | Terminate by process ID (find it with `ps` or `jobs -l`) |
| `kill -STOP PID` / `kill -CONT PID` | Pause / resume by PID (another way) |

```bash
sleep 100 &
sleep 200 &
sleep 300 &
jobs            # [1] sleep 100   [2] sleep 200   [3] sleep 300
fg %2           # sleep 200 now runs in the foreground
```

**Pipe `|`**: the output of the left command becomes the input of the right command.
`cat /etc/passwd | grep home` prints `/etc/passwd`, but only the lines containing "home".

---

## 3. Memory management

| Task | Command |
|------|---------|
| Show memory information | `cat /proc/meminfo` |
| Show amount of free and used memory | `free -h` |

> `-h` = **human-readable** (shows K, M, G instead of raw numbers). You will see `-h` in many commands.

---

## 4. Storage management

| Task | Command(s) |
|------|------------|
| Create a file, rename, delete | `touch a.txt` → `mv a.txt b.txt` → `rm b.txt` |
| Create a folder, rename, delete | `mkdir dir1` → `mv dir1 dir2` → `rmdir dir2` (or `rm -r dir2` if not empty) |
| File readable **only by the owner** | `touch secret.txt` → `chmod 400 secret.txt` → `ls -l secret.txt` |
| Change owner **and** group to root | `touch f.txt` → `sudo chown root:root f.txt` → `ls -l f.txt` |
| Folder + symbolic link to it | `mkdir mydir` → `ln -s mydir mylink` → `ls -l` |
| Disk space usage, human-readable | `df -h` |
| File **space** usage of the dictionary | `du -h /usr/share/dict/american-english` |
| Check and repair a file system | `sudo fsck /dev/sdXN` (the partition must be **unmounted**) |
| Search for a filename | `find / -name "filename" 2>/dev/null` (or `locate filename`) |

### Explanations

**File permissions (`chmod`)**

```
-rw-r--r--  1 user user  0 Aug 19 10:00 file.txt
 │└┬┘└┬┘└┬┘
 │ │  │  └── others
 │ │  └───── group
 │ └──────── owner (user)
 └────────── type: - = file, d = directory, l = symbolic link
```

| Number | Permission |
|:------:|------------|
| 4 | **r** = read |
| 2 | **w** = write |
| 1 | **x** = execute |

Add the numbers for each group: `chmod 400` = owner **r--**, group **---**, others **---**.
So only the owner can read the file. (`chmod 600` = owner read + write. `chmod 755` = rwxr-xr-x.)

**Space vs. size (`du` vs. `ls -l`)**
- `ls -l` shows the file **size** (number of bytes of data).
- `du` shows the **disk space used**. Disk space is given out in **blocks** (often 4 KB), so the space used is usually a bit **larger** than the size.

**Symbolic link**: like a **shortcut**. `ls -l` shows `mylink -> mydir`.

**`fsck` safely for practice**: you should not run `fsck` on a mounted disk. To practice, make a fake disk inside a file:

```bash
dd if=/dev/zero of=disk.img bs=1M count=100   # 100 MB empty file
mkfs.ext4 disk.img                             # put a file system on it
fsck.ext4 -f disk.img                          # check (and repair) it
```

On a real disk, check a partition that isn't mounted, for example `sudo fsck /dev/sdb1`.

**`find` vs. `locate`**
- `find` searches the disk live. It is slow but always up to date. `2>/dev/null` hides "Permission denied" errors.
- `locate` searches a database. It is fast, but run `sudo updatedb` first to refresh it.

---

## 5. User management

| Task | Command |
|------|---------|
| Change password | `passwd` |
| Show on-line users | `who` (or `users`) |
| Show users and what they are doing | `w` |
| Enable the root (superuser) account | `sudo passwd root` → then log in with `su -` |
| Add a new user, then remove it | `sudo adduser bob` → `sudo deluser --remove-home bob` |

> Ubuntu **locks** the root account by default. Setting a password with `sudo passwd root` enables it.
> Alternatives: `sudo useradd -m bob` and `sudo userdel -r bob` (`-m` creates the home folder, `-r` removes it).

---

## 6. Network management

| Task | Command(s) |
|------|------------|
| Remote login (to your own machine is OK) | `ssh username@localhost` |
| Transfer a file | `sftp username@localhost` → `put file.txt` / `get file.txt` |
| Show your IP address | `ip addr` (or `hostname -I`) |
| Show the IP address of google.com | `host google.com` (or `nslookup google.com`, `dig google.com`) |
| Show your MAC address | `ip link` → look at `link/ether xx:xx:xx:xx:xx:xx` |
| Show connection speed to google.com | `ping -c 4 google.com` (look at `time=… ms`) |
| Download a file at 1:00 AM tomorrow | `echo "wget https://cache111.com/test.zip" \| at 01:00 tomorrow` |

### Explanations

**SSH / SFTP setup**

```bash
sudo apt install openssh-server
ssh $USER@localhost          # 1. login with password
exit
sftp $USER@localhost
sftp> put myfile.txt         # upload
sftp> get myfile.txt         # download
sftp> bye
```

**SSH with a key file** (the slides say to study this yourself):

```bash
ssh-keygen                   # make a key pair (press Enter for defaults)
ssh-copy-id $USER@localhost  # copy the public key to the server
ssh $USER@localhost          # now logs in without a password
```

**FTP exercise from the slides** (download only):

```bash
ftp -p ftp.broadinstitute.org        # -p = passive mode (this server needs it)
Name: gsapubftp-anonymous
Password: a@
ftp> cd /bundle/hg38
ftp> bi                              # bi = binary mode (you can type "binary")
ftp> get Homo_sapiens_assembly38.dict
ftp> bye
```

**Scheduling with `at`**

| Command | Meaning |
|---------|---------|
| `echo "cmd" \| at 01:00 tomorrow` | Run `cmd` at 1:00 AM tomorrow |
| `atq` | List scheduled jobs |
| `atrm N` | Remove job number N |

**`ping`**: measures the **round-trip time** (latency) in milliseconds. A lower number means a faster connection. Stop with **Ctrl+C**, or use `-c 4` to send only 4 pings.

---

## 7. Utilities (1)

| Task | Command(s) |
|------|------------|
| Manual page of `man` | `man man` (press **q** to quit) |
| First 10 lines of /etc/passwd | `head /etc/passwd` (default is 10; same as `head -n 10`) |
| Last 10 lines of /etc/passwd | `tail /etc/passwd` |
| First page of the dictionary, then scroll | `more /usr/share/dict/american-english` (**Space** = next page) or `less` |
| Lines containing "sys" | `grep sys /etc/passwd` |
| Lines **ending** with "land" | `grep 'land$' /usr/share/dict/american-english` |
| Count lines | `wc -l /usr/share/dict/american-english` |
| Open in an editor and search for "microcomputer" | `vi /usr/share/dict/american-english` → type `/microcomputer` → **Enter** |
| Show each character of a file in hex | `echo "Somchai" > name.txt` → `xxd name.txt` (or `hexdump -C name.txt`) |
| Encrypt a file with a password | `gpg -c name.txt` → creates `name.txt.gpg` (decrypt: `gpg name.txt.gpg`) |
| Hello world in C | `nano hello.c` → `gcc hello.c -o hello` → `./hello` |
| Process that keeps running after logout | `nohup sleep 1000 &` |
| Shut down in 10 minutes | `sudo shutdown +10` |
| Cancel the shutdown | `sudo shutdown -c` |

### Explanations

**Regular expressions in `grep`**

| Pattern | Meaning | Example |
|---------|---------|---------|
| `^abc` | line **starts** with abc | `grep '^root' /etc/passwd` |
| `abc$` | line **ends** with abc | `grep 'land$' words` |
| `-i` | ignore upper/lower case | `grep -i SYS /etc/passwd` |
| `-c` | count matching lines | `grep -c home /etc/passwd` |

**`vi` survival kit**

| Key | Action |
|-----|--------|
| `/word` + Enter | Search forward |
| `n` / `N` | Next / previous match |
| `i` | Insert mode (start typing) |
| **Esc** | Back to command mode |
| `:wq` | Save and quit |
| `:q!` | Quit without saving |

(In `nano`, search is **Ctrl+W**, save is **Ctrl+O**, and exit is **Ctrl+X**.)

**hello.c**

```c
#include <stdio.h>

int main(void) {
    printf("Hello, world!\n");
    return 0;
}
```

**`nohup`** = "no hang-up". Normally, when you log out, your processes get the **SIGHUP** signal and stop. `nohup` ignores that signal, and output goes to `nohup.out`.

---

## 8. Utilities (2)

| Task | Command(s) |
|------|------------|
| Folder with a file → `foldername.tar.gz` | `mkdir foldername` → `touch foldername/a.txt` → `tar -czvf foldername.tar.gz foldername` |
| Checksum of the .tar.gz | `md5sum foldername.tar.gz` (or `sha256sum`) |
| Folder with a file → `foldername.zip` | `zip -r foldername.zip foldername` |
| Checksum of the .zip | `md5sum foldername.zip` |
| Concatenate with `cat`, `>` and `>>` | see below |
| Split the dictionary into 10 files x00–x09 | `split -n l/10 -d /usr/share/dict/american-english` |
| Show only the first column of /etc/passwd | `cut -d: -f1 /etc/passwd` |
| Show printer queue | `lpq` (or `lpstat`) |
| Show current date and time | `date` |
| Show calendar | `cal` |

### Explanations

**`tar` options**

| Option | Meaning |
|:------:|---------|
| `-c` | **c**reate an archive |
| `-x` | e**x**tract an archive |
| `-z` | compress with g**z**ip (.gz) |
| `-v` | **v**erbose (list the files) |
| `-f` | the next word is the archive **f**ile name |

Extract: `tar -xzvf foldername.tar.gz`. Unzip: `unzip foldername.zip`.

**`>` vs. `>>`**

```bash
echo "hello" > a.txt
echo "world" > b.txt
cat a.txt b.txt > c.txt     # >  = create or OVERWRITE c.txt
cat a.txt >> c.txt          # >> = APPEND to the end of c.txt
cat c.txt                   # hello / world / hello
```

**`split` options**
- `-n l/10`: split into **10 files** without cutting a line in half (`l` = lines).
- `-d`: use **numbers** (x00, x01, …) instead of letters (xaa, xab, …).

**`cut`**: `/etc/passwd` uses `:` between fields (`root:x:0:0:root:/root:/bin/bash`).
`-d:` sets the separator to `:`, and `-f1` takes field 1, which is the username.

**Checksum**: a "fingerprint" of a file. If even one byte changes, the checksum changes. It is used to check that a download is not corrupted.

---

## 9. Quick cheat sheet

```
SYSTEM INFO     lsb_release -a | uname -r | lscpu | uptime | free -h | df -h
PROCESSES       cmd & | Ctrl+Z | fg %n | bg %n | jobs | kill %n | nice -n 19 cmd | nohup cmd &
FILES           touch | mv | rm | mkdir | rmdir | rm -r | ln -s | chmod | chown | du -h | find
USERS           passwd | who | w | adduser | deluser | sudo passwd root
NETWORK         ssh | sftp | ftp -p | ip addr | ip link | host | ping | at
TEXT            man | head | tail | more | less | grep | wc -l | cut | split | xxd
ARCHIVES        tar -czvf | zip -r | md5sum | sha256sum
OTHER           gcc | gpg -c | shutdown +10 | shutdown -c | date | cal | lpq
```

---

## 10. Practice quiz

Try to answer without looking, then open the answer.

### Part A: Commands

1. Run `sleep 300` with the **lowest** priority.
   <details><summary>Answer</summary>

   `nice -n 19 sleep 300 &`
   </details>

2. Start 3 background `sleep` processes and bring the **second** one to the foreground.
   <details><summary>Answer</summary>

   `sleep 100 & sleep 200 & sleep 300 &` → `jobs` → `fg %2`
   </details>

3. Make `secret.txt` readable **only by the owner**.
   <details><summary>Answer</summary>

   `chmod 400 secret.txt`
   </details>

4. Show the lines in the dictionary that **end** with "land".
   <details><summary>Answer</summary>

   `grep 'land$' /usr/share/dict/american-english`
   </details>

5. Show only the **usernames** in `/etc/passwd`.
   <details><summary>Answer</summary>

   `cut -d: -f1 /etc/passwd`
   </details>

6. Schedule a download of `https://cache111.com/test.zip` at 1:00 AM tomorrow.
   <details><summary>Answer</summary>

   `echo "wget https://cache111.com/test.zip" | at 01:00 tomorrow`
   </details>

7. Shut down in 10 minutes, then cancel it.
   <details><summary>Answer</summary>

   `sudo shutdown +10` → `sudo shutdown -c`
   </details>

8. Show your firstname file in hex.
   <details><summary>Answer</summary>

   `xxd name.txt` (or `hexdump -C name.txt`, `od -x name.txt`)
   </details>

9. What is the difference between `cat a b > c` and `cat a b >> c`?
   <details><summary>Answer</summary>

   `>` overwrites `c`. `>>` appends to the end of `c`.
   </details>

10. Which command shows **file space** used, and why is it different from the file size?
    <details><summary>Answer</summary>

    `du -h file`. Disk space is given out in whole blocks, so the space used is usually bigger than the real size shown by `ls -l`.
    </details>

### Part B: Theory

11. What are the two main jobs of an operating system?
    <details><summary>Answer</summary>

    Manage resources (CPU, memory, storage, I/O) and provide services to programs.
    </details>

12. What is the difference between cooperative and preemptive multi-tasking?
    <details><summary>Answer</summary>

    In cooperative multi-tasking, a program must give up the CPU by itself, so one long program can block everyone (Windows 3.1). In preemptive multi-tasking, the OS takes the CPU back with a timer interrupt after each time slice (Windows 95, Linux).
    </details>

13. What hardware does the CPU need for time sharing?
    <details><summary>Answer</summary>

    A **timer interrupt**.
    </details>

14. What is the difference between a process and a thread?
    <details><summary>Answer</summary>

    A process is a program loaded into memory and executing, with its own memory. A thread is a light-weight process inside a process. Threads of the same process share memory, with no memory protection between them.
    </details>

15. Was DOS multi-tasking? Did it have a GUI?
    <details><summary>Answer</summary>

    No and no. DOS was single-user, single-task and text-only.
    </details>

16. What do "host OS" and "guest OS" mean?
    <details><summary>Answer</summary>

    The host OS runs on the real hardware (for example, Windows or Mac). The guest OS runs inside a virtual machine (for example, Linux in VirtualBox).
    </details>

17. What does GNU stand for?
    <details><summary>Answer</summary>

    "GNU is Not Unix" (a recursive acronym).
    </details>

---

> 📖 Reading from the slides: **textbook pages 3–35** (Chapter 1).