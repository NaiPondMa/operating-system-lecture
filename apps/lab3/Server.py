"""
server.py  -  A multi-threaded TCP server (Operating Systems lab example)

OS concepts shown:
  * Sockets      : the OS's endpoint for inter-process communication (IPC)
  * System calls : socket(), bind(), listen(), accept(), recv(), send(), close()
  * Threads      : one thread per client, so many clients are served concurrently
  * Mutex (Lock) : protects shared data from a race condition

Run:  python3 server.py
"""

import socket
import threading
import os
import time

HOST = "127.0.0.1"   # localhost: only this machine can connect
PORT = 5000          # any free port above 1024

# ---- Shared data between threads (critical section!) ----
client_count = 0
count_lock = threading.Lock()


def handle_client(conn, addr):
    """Runs inside its own thread, one per connected client."""
    global client_count

    # Critical section: only one thread may change client_count at a time
    with count_lock:
        client_count += 1
        #time.sleep(0.1)
        my_number = client_count

    thread_name = threading.current_thread().name
    print(f"[CONNECT] {addr} -> handled by {thread_name} (client #{my_number})")

    conn.sendall(f"Welcome client #{my_number}! Commands: TIME, PID, UPPER <text>, BYE\n".encode())

    while True:
        data = conn.recv(1024)          # blocks until data arrives (blocking I/O)
        if not data:                    # empty bytes = client closed the connection
            break

        message = data.decode().strip()
        print(f"[{thread_name}] received: {message!r}")

        # A tiny "protocol": the rules both sides agree on
        if message == "TIME":
            reply = time.ctime()
        elif message == "PID":
            reply = f"Server process ID = {os.getpid()}, thread = {thread_name}"
        elif message.startswith("UPPER "):
            reply = message[6:].upper()
        elif message == "BYE":
            conn.sendall(b"Goodbye!\n")
            break
        else:
            reply = f"Unknown command: {message}"

        conn.sendall((reply + "\n").encode())

    conn.close()                        # release the OS resource (file descriptor)
    print(f"[DISCONNECT] {addr}")


def main():
    # 1. socket(): ask the OS for a new endpoint. AF_INET = IPv4, SOCK_STREAM = TCP
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Let us restart the server right away without "Address already in use"
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    # 2. bind(): attach the socket to an address + port
    server_sock.bind((HOST, PORT))

    # 3. listen(): tell the OS we accept connections; 5 = waiting-queue size
    server_sock.listen(5)
    print(f"[LISTENING] Server PID {os.getpid()} on {HOST}:{PORT}")

    try:
        while True:
            # 4. accept(): block until a client connects; returns a NEW socket for it
            conn, addr = server_sock.accept()

            # 5. Create a thread so the main loop can accept the next client right away
            t = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
            handle_client(conn, addr)
            print(f"[ACTIVE THREADS] {threading.active_count() - 1}")
    except KeyboardInterrupt:
        print("\n[SHUTDOWN] Server stopping.")
    finally:
        server_sock.close()


if __name__ == "__main__":
    main()