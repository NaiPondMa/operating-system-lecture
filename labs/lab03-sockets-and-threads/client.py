"""
client.py  -  A TCP client for server.py (Operating Systems lab example)

System calls used: socket(), connect(), send(), recv(), close()

Run (after the server is running):  python3 client.py
"""

import socket

HOST = "127.0.0.1"   # must match the server
PORT = 5000


def main():
    # 1. socket(): create an endpoint
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # 2. connect(): the TCP 3-way handshake happens here (SYN, SYN-ACK, ACK)
    sock.connect((HOST, PORT))

    # Read the welcome message
    print(sock.recv(1024).decode(), end="")

    while True:
        msg = input("You> ").strip()
        if not msg:
            continue

        # 3. send(): data goes into the OS's send buffer, then over the network
        sock.sendall(msg.encode())

        # 4. recv(): wait for the server's reply
        reply = sock.recv(1024).decode()
        print("Server>", reply, end="")

        if msg == "BYE" or not reply:
            break

    # 5. close(): free the socket (file descriptor)
    sock.close()


if __name__ == "__main__":
    main()