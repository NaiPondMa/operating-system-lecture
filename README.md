# Operating System Lab Library

My notes, code and exercises for the Operating Systems course (2301274 Computer Systems).
Each lab has a folder with a short lecture, runnable code, and practice questions with hidden answers.

## Labs

| Lab | Topic | Main ideas | Status |
|:---:|-------|------------|:------:|
| [1](labs/lab01-linux-commands/) | Linux commands | OS basics, processes, files, users, network, utilities | ✅ Ready |
| 2 | *(coming)* | | ⏳ |
| [3](labs/lab03-sockets-and-threads/) | Sockets and threads | IPC, system calls, blocking, threads, locks | ✅ Ready |
| [4](labs/lab04-eror-diffusion/) | Error diffusion (parallel dithering) | Data dependency, pipeline, wavefront, threads vs processes, GIL, shared memory | ✅ Ready |
| [5](labs/lab05-readers-writers/) | Readers–writers problem | Synchronization, mutex, semaphore, starvation | 🚧 Lecture only |

Need a word explained? See the [glossary](docs/glossary.md).

## How to use a lab

Every lab folder has the same layout, so you always know where to look:

| File | Read it to... |
|------|---------------|
| `README.md` | See the goal, the files, and how to run the code |
| `lecture.md` | Learn the theory (start here) |
| `exercises.md` | Practice: run the code, do the experiments, check yourself |
| code (`.py`, `.cs`, ...) | Run it and change it |

## Folder layout

```
Operating_System/
├── README.md              ← you are here
├── docs/
│   └── glossary.md        ← all key terms, A–Z
└── labs/
    ├── _template/         ← copy this to start a new lab
    ├── lab01-linux-commands/
    ├── lab03-sockets-and-threads/
    ├── lab04-eror-diffusion/
    └── lab05-readers-writers/
```

## What you need

| For | Install |
|-----|---------|
| Lab 1 | Ubuntu/Debian Linux (a VM or WSL works). The package list is in the lab's lecture |
| Lab 3 | Python 3 |
| Lab 4 | Python 3 with `numpy` and `pillow` (`pip install numpy pillow matplotlib`) |
| Lab 5 | .NET SDK (to run C# code) |

## Adding a new lab

1. Copy `labs/_template/` to `labs/labNN-short-name/` (two-digit number, for example `lab02-processes`).
2. Fill in `README.md`, `lecture.md` and `exercises.md`. Put the code in the same folder.
3. Add a row to the table above and new terms to [docs/glossary.md](docs/glossary.md).
