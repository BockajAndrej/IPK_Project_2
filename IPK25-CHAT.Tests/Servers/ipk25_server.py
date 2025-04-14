import socket
import time
import random
import threading
import signal
import sys

"""
========================================
 IPK25 Mock TCP Server (Python) - FSM Test Enhanced
========================================

This script implements a mock TCP server for testing IPK25-CHAT clients,
specifically focusing on validating client behavior according to the strict
Finite State Machine (FSM) defined in the project specification.

----------------------------------------
How to use:
1. Start the server: `python3 ipk25_tcp_server_fsm.py` (or your filename)
2. Start your C# client with the strict FSM logic enabled:
   `dotnet run -- -t tcp -s 127.0.0.1`
3. Use the specific commands/messages below to trigger test scenarios.

----------------------------------------
Standard Test Effects (from original description):

→ /auth <user> <secret> <display_name>
   (If not 'invalid' or 'nerd')
   REPLY OK IS Welcome
   MSG FROM Server IS <display_name> joined default.
→ /auth <user> invalid <display_name>
   REPLY NOK IS Invalid credentials
→ /auth <user> <secret> nerd
   REPLY NOK IS Nerds must pay the Nerd Tax first.
→ /join secret
   REPLY NOK IS You are not allowed to join this channel
→ /join <channel> (where channel is not 'secret' or 'nothing')
   REPLY OK IS Join success.
   MSG FROM Server IS <display_name> joined <channel>.
→ /join nothing
   No response (simulates client timeout).

Messages:
→ `split`
   (Server sends "MSG FROM python_server " then waits 2s, then hellosends "IS you sent 'split' (split test)\r\nMSG FROM python_server IS another one \r\n")
→ `err`
   ERR FROM Server IS Simulated error requested
→ contains `hello` or `hi`
   Replies "Hi!", "Hello {sender_name}!", "Hey there!"
→ anything else
   Replies "MSG FROM python_server IS Received: '{content}'"
----------------------------------------
**NEW** FSM Strictness Test Scenarios:

→ /auth <user> <secret> test_msg_during_auth
   Server sends: MSG FROM Server IS Unexpected MSG during AUTH!
   Then sends: REPLY OK IS Welcome (test_msg_during_auth)
   **EXPECTED CLIENT BEHAVIOR (Strict FSM):** Client should terminate with
   a protocol violation error upon receiving the MSG while in Auth state.

→ After successful AUTH, send message: `test_reply_in_open`
   Server sends: REPLY OK IS Unexpected REPLY in OPEN state!
   **EXPECTED CLIENT BEHAVIOR (Strict FSM):** Client is in Open state. It
   receives a REPLY. Client should terminate with a protocol violation error.

→ After sending /join test_msg_during_join:
   Server sends: MSG FROM Server IS Unexpected MSG during JOIN!
   Then sends: REPLY OK IS Join success (test_msg_during_join)
   **EXPECTED CLIENT BEHAVIOR (Strict FSM):** Client sends JOIN, enters Join
   state. It receives the MSG. Client should allow msg durign /join state

----------------------------------------
"""

HOST = '127.0.0.1'
PORT = 4567

def send_message_async(conn, addr, message, log_prefix="Sent Async"):
    """Helper to send a message without blocking the main handler logic."""
    try:
        conn.sendall(message.encode())
        print(f"[<] {log_prefix} to {addr}: {repr(message)}")
    except Exception as e:
        print(f"[!] Error in send_message_async to {addr}: {e}")

def handle_client(conn, addr):
    """Handles communication with a single connected client."""
    print(f"[+] Client connected from {addr}")
    client_state = 'START' # Simulate server-side state tracking
    display_name = None

    # --- FSM Test: Send unexpected REPLY in START state ---
    # This is hard to test reliably as client sends AUTH quickly.
    # A better test is sending unexpected REPLY in OPEN state later.
    # Uncomment the following lines ONLY for specific START state testing:
    # if client_state == 'START':
    #    time.sleep(0.5) # Give client tiny moment to connect fully
    #    print("[*] TEST: Sending unexpected REPLY in START state")
    #    reply_in_start = "REPLY OK IS Unexpected REPLY in START!\r\n"
    #    threading.Thread(target=send_message_async, args=(conn, addr, reply_in_start, "Sent Test"), daemon=True).start()
    #    # We might continue processing AUTH after this, or client might disconnect

    try:
        buffer = ""
        while True:
            reply = None
            extra_msg_after_reply = None
            send_reply_normally = True # Flag to control normal reply sending

            try:
                # Increased buffer size slightly, use non-blocking recv with timeout
                conn.settimeout(0.2) # Short non-blocking timeout
                try:
                     data_bytes = conn.recv(8192)
                except socket.timeout:
                    # No data received in this short interval, loop again
                    # This allows checking connection state more frequently
                    if client_state == 'END_REQUESTED': # Check if we decided to end
                         break
                    continue
                except (ConnectionResetError, ConnectionAbortedError):
                    print(f"[-] Connection reset or aborted by {addr}")
                    break
                except OSError as e:
                    print(f"[!] Socket error receiving from {addr}: {e}")
                    break

                if not data_bytes:
                    print(f"[-] Client {addr} disconnected (recv returned 0 bytes).")
                    break

                buffer += data_bytes.decode('utf-8', errors='ignore') # Ignore decode errors for simplicity

            except UnicodeDecodeError:
                 print(f"[!] Received non-UTF8 data from {addr}. Closing.")
                 err_msg = "ERR FROM Server IS Malformed non-UTF8 message received.\r\n"
                 threading.Thread(target=send_message_async, args=(conn, addr, err_msg, "Sent Error"), daemon=True).start()
                 client_state = 'END_REQUESTED' # Signal to exit loop
                 continue # Go back to loop check
            except Exception as e:
                 print(f"[!] Error during recv/decode: {e}")
                 break # Exit on other recv errors


            # Process complete lines from the buffer
            while '\r\n' in buffer:
                line, buffer = buffer.split('\r\n', 1)
                print(f"[>] Received Line: {repr(line)}")
                current_command = line # Process one command/line at a time

                # Reset flags/messages for each command
                reply = None
                extra_msg_after_reply = None
                send_reply_normally = True
                terminate_after_send = False # Flag to break after sending ERR/BYE

                # --- State Machine Logic ---
                if current_command.startswith("AUTH"):
                    if client_state != 'START':
                        reply = "ERR FROM Server IS Already authenticated or invalid state for AUTH.\r\n"
                        terminate_after_send = True
                    else:
                        client_state = 'AUTH_WAIT' # Tentative state while processing
                        try:
                            parts = current_command.strip().split()
                            if len(parts) >= 6 and parts[2] == "AS" and parts[4] == "USING":
                                secret = parts[5]
                                display_name = parts[3]

                                if "test_msg_during_auth" in display_name.lower():
                                    print("[*] TEST: Sending MSG during AUTH wait")
                                    client_state = 'AUTH_WAIT' # Remain waiting
                                    # Send MSG *before* the reply
                                    unexpected_msg = f"MSG FROM Server IS Unexpected MSG during AUTH!\r\n"
                                    threading.Thread(target=send_message_async, args=(conn, addr, unexpected_msg, "Sent Test (Before AUTH Reply)"), daemon=True).start()
                                    time.sleep(0.2) # Small delay
                                    # Now prepare the normal reply
                                    reply = "REPLY OK IS Welcome (test_msg_during_auth)\r\n"
                                    # State will transition properly after sending reply

                                elif display_name.lower() == "nerd":
                                    reply = "REPLY NOK IS Nerds must pay the Nerd Tax first.\r\n"
                                    # State remains AUTH_WAIT until reply sent, then START
                                elif secret.lower() == "invalid":
                                    reply = "REPLY NOK IS Invalid credentials\r\n"
                                    # State remains AUTH_WAIT until reply sent, then START
                                else:
                                    reply = "REPLY OK IS Welcome\r\n"
                                    extra_msg_after_reply = f"MSG FROM Server IS {display_name} joined default.\r\n"
                                    # State remains AUTH_WAIT until reply sent, then OPEN
                            else:
                                reply = "REPLY NOK IS Invalid AUTH syntax\r\n"
                                # State remains AUTH_WAIT until reply sent, then START
                        except Exception as e:
                            print(f"[!] Exception during AUTH parsing: {e}")
                            reply = "REPLY NOK IS Malformed AUTH\r\n"
                            # State remains AUTH_WAIT until reply sent, then START

                elif current_command.startswith("JOIN"):
                    if client_state != 'OPEN':
                        reply = "ERR FROM Server IS Must be in OPEN state to JOIN.\r\n"
                        terminate_after_send = True
                    else:
                        client_state = 'JOIN_WAIT' # Tentative state
                        try:
                            parts = current_command.strip().split()
                            if len(parts) >= 4 and parts[2] == "AS":
                                channel = parts[1].lower()
                                join_display_name = parts[3]

                                if channel == "secret":
                                    reply = "REPLY NOK IS You are not allowed to join this channel\r\n"
                                    # State remains JOIN_WAIT until reply sent, then OPEN
                                elif channel == "nothing":
                                    print("[*] TEST: Simulating no response for JOIN nothing")
                                    # Stay in JOIN_WAIT, send nothing
                                    send_reply_normally = False
                                elif "test_msg_during_join" in channel:
                                    print("[*] TEST: Sending MSG during JOIN wait")
                                    # Send MSG *before* the reply
                                    unexpected_msg = f"MSG FROM Server IS Unexpected MSG during JOIN!\r\n"
                                    threading.Thread(target=send_message_async, args=(conn, addr, unexpected_msg,"Sent Test (Before JOIN Reply)"), daemon=True).start()
                                    time.sleep(0.2)
                                    # Prepare normal reply
                                    reply = "REPLY OK IS Join success (test_msg_during_join)\r\n"
                                     # State remains JOIN_WAIT until reply sent, then OPEN
                                else:
                                    reply = f"REPLY OK IS Join success.\r\n"
                                    extra_msg_after_reply = f"MSG FROM Server IS {join_display_name} joined {channel}.\r\n"
                                    # State remains JOIN_WAIT until reply sent, then OPEN
                            else:
                                reply = "REPLY NOK IS Invalid JOIN syntax\r\n"
                                # State remains JOIN_WAIT until reply sent, then OPEN
                        except Exception as e:
                             print(f"[!] Exception during JOIN parsing: {e}")
                             reply = "REPLY NOK IS Malformed JOIN\r\n"
                             # State remains JOIN_WAIT until reply sent, then OPEN

                elif current_command.startswith("MSG"):
                    if client_state != 'OPEN':
                        reply = "ERR FROM Server IS Not authorized or wrong state for MSG.\r\n"
                        terminate_after_send = True
                    else:
                        try:
                            # Basic parsing
                            is_index = current_command.find(" IS ")
                            content = current_command[is_index + 4:].strip() if is_index != -1 else ""
                            content_lower = content.lower()
                            sender_name = display_name

                            print(f"[i] MSG content from {sender_name}: '{content}'")

                            # Test Triggers
                            if "err" == content_lower:
                                reply = "ERR FROM Server IS Simulated error requested\r\n"
                                terminate_after_send = True
                            elif "split" == content_lower:
                                print("[*] TEST: Simulating split message send")
                                part1 = "MSG FROM python_server "
                                part2 = f"IS you sent '{content}' (split test)\r\nMSG FROM python_server IS another one \r\n"
                                # Use threads to avoid blocking loop
                                threading.Thread(target=send_message_async, args=(conn, addr, part1, "Sent PART 1"), daemon=True).start()
                                time.sleep(2)
                                threading.Thread(target=send_message_async, args=(conn, addr, part2, "Sent PART 2"), daemon=True).start()
                                send_reply_normally = False
                            elif "test_reply_in_open" == content_lower:
                                 print("[*] TEST: Sending unexpected REPLY in OPEN state")
                                 reply = f"REPLY OK IS This reply is unexpected!\r\n"
                                 # Client should terminate based on this reply
                            # Conversational Logic (Optional)
                            elif "hello" in content_lower or "hi" in content_lower:
                                possible_replies = ["Hi!", f"Hello {sender_name}!", "Hey there!"]
                                reply = f"MSG FROM python_server IS {random.choice(possible_replies)}\r\n"
                            # Default reply
                            else:
                                reply = f"MSG FROM python_server IS Received: '{content}'\r\n"
                        except Exception as e:
                             print(f"[!] Exception during MSG processing: {e}")
                             reply = "ERR FROM Server IS Error processing message\r\n"
                             terminate_after_send = True

                elif current_command.startswith("BYE"):
                    print("[*] Received BYE from client. Closing connection.")
                    reply = None # No reply to BYE
                    send_reply_normally = False
                    client_state = 'END_REQUESTED' # Signal loop to exit

                else: # Unknown command
                    reply = f"ERR FROM Server IS Unknown command: {repr(current_command)}\r\n"
                    terminate_after_send = True

                # --- Send Reply / Extra Message ---
                if reply and send_reply_normally:
                    # Use thread to avoid blocking
                    threading.Thread(target=send_message_async, args=(conn, addr, reply, "Sent Reply"), daemon=True).start()
                    # State transition happens *after* sending the reply
                    if reply.startswith("REPLY OK"):
                         if client_state == 'AUTH_WAIT': client_state = 'OPEN'
                         elif client_state == 'JOIN_WAIT': client_state = 'OPEN'
                    elif reply.startswith("REPLY NOK"):
                         if client_state == 'AUTH_WAIT': client_state = 'START'
                         elif client_state == 'JOIN_WAIT': client_state = 'OPEN'
                    elif reply.startswith("ERR"):
                         client_state = 'END_REQUESTED'


                    # Send extra message if scheduled (also in thread)
                    if extra_msg_after_reply:
                         # Short delay before sending extra message
                         time.sleep(0.05)
                         threading.Thread(target=send_message_async, args=(conn, addr, extra_msg_after_reply, "Sent Extra"), daemon=True).start()

                if terminate_after_send or client_state == 'END_REQUESTED':
                    break # Exit inner command processing loop

            # Check if we should exit outer loop
            if client_state == 'END_REQUESTED':
                 break


    except Exception as e:
        print(f"[!] Unhandled exception in client handler for {addr}: {e}")

    finally:
        print(f"[-] Client from {addr} disconnected")
        try:
            conn.shutdown(socket.SHUT_RDWR) # Signal close intent
        except OSError:
            pass # Ignore if already closed
        conn.close()

# --- run_server function ---
def run_server():
    """Sets up the server socket and listens for incoming connections."""
    print(f"[~] IPK25 Mock TCP Server (FSM Test Enhanced) running on {HOST}:{PORT}")
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        # Allow reusing address quickly after server restart
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        try:
            server.bind((HOST, PORT))
            server.listen()
            print("[~] Waiting for connections...")
        except Exception as e:
             print(f"[!] Failed to start server: {e}")
             return

        active_threads = []
        running = True

        def signal_handler(sig, frame):
            nonlocal running
            print("\n[~] Server shutdown requested (Ctrl+C).")
            running = False
            # Optionally try to close the listening socket to break the accept loop
            # This might raise an exception in the main loop, which is caught
            try:
                 server.close()
            except Exception:
                 pass

        signal.signal(signal.SIGINT, signal_handler)
        signal.signal(signal.SIGTERM, signal_handler)

        server.settimeout(1.0) # Set timeout for accept()

        while running:
            try:
                conn, addr = server.accept()
                # Create and start a new thread for each client
                thread = threading.Thread(target=handle_client, args=(conn, addr), daemon=True)
                active_threads.append(thread)
                thread.start()
                # Clean up finished threads
                active_threads = [t for t in active_threads if t.is_alive()]

            except socket.timeout:
                 # This is expected due to settimeout, allows checking 'running' flag
                 continue
            except KeyboardInterrupt:
                 # This might happen if Ctrl+C is pressed during accept()
                 running = False # Ensure loop condition is false
                 break
            except Exception as e:
                 # Log other errors during accept, but keep server running if possible
                 if running: # Only log if we weren't intentionally shutting down
                     print(f"[!] Error accepting connection: {e}")
                 else:
                      print("[~] Server socket closed during shutdown.")


        print("[~] Server main loop finished. Shutting down...")
        # No need to explicitly join daemon threads, they will exit with main thread
        print("[~] Server shut down.")


if __name__ == "__main__":
    run_server()