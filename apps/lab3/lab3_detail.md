Lab: Let’s play

Open three terminal windows.

Terminal 1: run python3 server.py
Terminal 2: run python3 client.py, then type TIME, PID, UPPER hello
Terminal 3: run python3 client.py again, while the first client is still connected

What to notice:

When you type PID, both clients show the same process ID but different thread names. That proves one process is serving both clients with different threads.
The server terminal shows [ACTIVE THREADS] going up and down as clients join and leave.
Experiments to try (the “homework”)
Break concurrency. In server.py, replace t.start() with a direct call: handle_client(conn, addr). Connect two clients. The second one gets stuck until the first types BYE. Question: why?
Watch blocking. Start a client but don’t type anything. The server thread is sleeping inside recv(). On Linux, run ps -eLf | grep server.py to see the threads (L = light-weight processes).
See the race condition. Remove with count_lock: and add time.sleep(0.1) between reading and writing client_count. Connect several clients quickly. Do some get the same number?
Port conflict. Start a second server while the first is running. You’ll get Address already in use. Only one socket can bind a given port.
Threads vs. processes. Swap threading.Thread for multiprocessing.Process. Now PID shows a different process ID for each client. Question: what are the trade-offs in memory and in sharing data?
Exam-style questions to check yourself
What’s the difference between the socket from socket() and the one returned by accept()?
Why does recv() returning empty bytes mean the client disconnected?
TCP (SOCK_STREAM) or UDP (SOCK_DGRAM): which one needs listen()/accept(), and why?

If you try an experiment and get stuck, paste what you see and I’ll go through it with you. I can also make a UDP version or a fork()-based version to compare.