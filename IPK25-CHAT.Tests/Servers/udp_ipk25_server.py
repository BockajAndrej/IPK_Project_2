import socket
import struct
import threading
import time
import random
import signal
import sys

"""
========================================
 IPK25 Mock UDP Server (Python) - Command Triggered Tests
========================================

This script implements a mock UDP server for testing IPK25-CHAT clients.
It follows the expected communication format of the IPK25 protocol,
including dynamic port allocation, and allows triggering specific test
scenarios by sending specially crafted commands/messages from the client.

----------------------------------------
How it works:
1. Server listens on the default port (4567) ONLY for AUTH messages.
2. Upon receiving AUTH, it sends CONFIRM from the listener port.
3. It then allocates a new dynamic port for the client.
4. A new thread is started to handle all further communication with
   that client exclusively on the new dynamic port.
5. The handler thread sends the AUTH REPLY from the dynamic port.
6. All subsequent messages (JOIN, MSG, BYE from client; MSG, REPLY,
   ERR, BYE from server) between that client and server use the
   dynamic port.

----------------------------------------
How to Run Tests:

AUTH Scenarios (Send via /auth command):
  Use specific keywords in the <username> field:
  → /auth failauth <secret> <DisplayName>
    Server sends REPLY NOK for AUTH.
  → /auth timeoutauth <secret> <DisplayName>
    Server does *not* send REPLY for AUTH (client should timeout after 5s).
  → /auth delayauth <secret> <DisplayName>
    Server sends REPLY OK for AUTH after a 6-second delay (client should timeout after 5s).

JOIN Scenarios (Send via /join command after successful AUTH):
  Use specific keywords in the <channel_id> field:
  → /join timeoutjoin
    Server sends CONFIRM but *not* REPLY for JOIN (client should timeout after 5s).
  → /join failjoin
    Server sends REPLY NOK for JOIN.
  → /join duplicatejoin
    Server sends REPLY OK for JOIN *twice*. (Client should ignore the second, but CONFIRM both).

MSG Scenarios (Send as message content after successful AUTH/JOIN):
  Include specific keywords in the message content:
  → (message containing 'noconfirm')
    Server receives MSG but does *not* send CONFIRM back (client should retransmit).
  → (message containing 'duplicatemsg')
    Server sends its standard reply MSG ("Got your MSG...") *twice* (client should ignore second, CONFIRM both).
  → (message containing 'servererr')
    Server sends ERR back and closes connection (client should show error, exit).
  → (message containing 'serverbye')
    Server sends BYE back and closes connection (client should show message, exit).
  → (message containing 'malformed')
    Server sends a deliberately malformed MSG back (client should detect parse error, exit).

Normal Operation:
  Any other valid AUTH, JOIN, MSG, BYE from the client will be handled normally
  according to the basic IPK25 protocol flow.
----------------------------------------
"""


# --- Protocol Constants (same as before) ---
DEFAULT_PORT = 4567
BUFFER_SIZE = 8192
TYPE_CONFIRM = 0x00
TYPE_REPLY = 0x01
TYPE_AUTH = 0x02
TYPE_JOIN = 0x03
TYPE_MSG = 0x04
TYPE_PING = 0xFD
TYPE_ERR = 0xFE
TYPE_BYE = 0xFF
TYPE_UNKNOWN = 0x10

# --- Helper Functions (same as before) ---
def print_log(thread_name, message): print(f"[{thread_name}] {message}")
def read_ushort_be(data, offset):
    if offset + 2 > len(data): raise ValueError("Not enough data for ushort")
    return struct.unpack_from('>H', data, offset)[0]
def read_string(data, offset):
    end_offset = data.find(b'\x00', offset)
    if end_offset == -1: raise ValueError("Null terminator not found")
    value = data[offset:end_offset].decode('ascii')
    new_offset = end_offset + 1
    return value, new_offset
def build_confirm(ref_msg_id): return struct.pack('>BH', TYPE_CONFIRM, 0) + struct.pack('>H', ref_msg_id)
def build_reply(msg_id, success, ref_msg_id, content):
    result_byte = 0x01 if success else 0x00
    content_bytes = content.encode('ascii') + b'\x00'
    header = struct.pack('>BHBH', TYPE_REPLY, msg_id, result_byte, ref_msg_id)
    return header + content_bytes
def build_msg(msg_id, display_name, content):
    dname_bytes = display_name.encode('ascii') + b'\x00'
    content_bytes = content.encode('ascii') + b'\x00'
    header = struct.pack('>BH', TYPE_MSG, msg_id)
    return header + dname_bytes + content_bytes
def build_err(msg_id, display_name, content):
    dname_bytes = display_name.encode('ascii') + b'\x00'
    content_bytes = content.encode('ascii') + b'\x00'
    header = struct.pack('>BH', TYPE_ERR, msg_id)
    return header + dname_bytes + content_bytes
def build_bye(msg_id, display_name):
    dname_bytes = display_name.encode('ascii') + b'\x00'
    header = struct.pack('>BH', TYPE_BYE, msg_id)
    return header + dname_bytes

# --- Client Handler Thread with Command-Based Triggers ---
def client_handler(handler_sock, client_addr, initial_data, initial_msg_id):
    """Handles communication, reacting to special client commands/content."""
    thread_name = f"Handler-{client_addr[0]}:{client_addr[1]}"
    print_log(thread_name, f"Started on dynamic port {handler_sock.getsockname()[1]}")

    server_msg_id_counter = 0
    client_state = {'display_name': None}
    authenticated = False

    try:
        # --- Initial AUTH processing ---
        parsed_auth = parse_message(initial_data, client_addr, "InitialAUTH")
        if parsed_auth and parsed_auth['type'] == TYPE_AUTH:
            username_lower = parsed_auth.get('username', '').lower()
            secret_lower = parsed_auth.get('secret', '').lower() # Assuming secret might trigger tests too
            client_state['display_name'] = parsed_auth.get('display_name', 'Unknown')
            print_log(thread_name, f"AUTH received for user '{parsed_auth.get('username','')}', display name '{client_state['display_name']}'")

            # --- AUTH Test Triggers based on Username/Secret ---
            reply_success = True
            reply_content = "Authentication successful."
            simulate_timeout = False
            simulate_delay_sec = 0

            if "failauth" in username_lower:
                print_log(thread_name, "*** Simulating AUTH REPLY NOK triggered by username ***")
                reply_success = False
                reply_content = "Auth failed (username trigger)."
            elif "timeoutauth" in username_lower:
                print_log(thread_name, "*** Simulating AUTH REPLY Timeout triggered by username ***")
                simulate_timeout = True
            elif "delayauth" in username_lower:
                 print_log(thread_name, "*** Simulating AUTH REPLY Delay (6s) triggered by username ***")
                 simulate_delay_sec = 6
            # Add more triggers based on secret if needed, e.g.
            # elif "badsecret" in secret_lower: reply_success = False; reply_content = "Invalid secret trigger."

            if simulate_timeout:
                # Just don't send the reply
                pass
            else:
                if simulate_delay_sec > 0:
                    time.sleep(simulate_delay_sec)

                #server_msg_id_counter += 1
                random_msg_id = random.randint(1, 0xFFFF) # Náhodné ID od 1 do 65535
                reply_msg = build_reply(random_msg_id, reply_success, initial_msg_id, reply_content)
                handler_sock.sendto(reply_msg, client_addr)
                print_log(thread_name, f"Sent REPLY {'OK' if reply_success else 'NOK'} for AUTH (ID={server_msg_id_counter}, RefID={initial_msg_id})")

                if reply_success:
                    authenticated = True
                    time.sleep(0.1)
                    server_msg_id_counter += 1
                    join_notice_content = f"{client_state['display_name']} has joined default."
                    join_notice_msg = build_msg(server_msg_id_counter, "Server", join_notice_content)
                    handler_sock.sendto(join_notice_msg, client_addr)
                    print_log(thread_name, f"Sent MSG join notice (ID={server_msg_id_counter})")

        else: # Initial message not AUTH
             print_log(thread_name, f"Initial message was not AUTH. Closing handler.")
             return

        # --- Main communication loop ---
        while authenticated:
            try:
                handler_sock.settimeout(60)
                data, addr = handler_sock.recvfrom(BUFFER_SIZE)
                if not data or addr != client_addr: continue

                print_log(thread_name, f"Received {len(data)} bytes")
                parsed = parse_message(data, client_addr, thread_name)
                if not parsed: continue

                msg_type = parsed['type']
                send_confirm = True
                send_standard_reply = True # Flag to control if standard processing occurs

                # --- Command-Based Test Triggers ---

                # A) Triggers based on received message TYPE and specific content
                if msg_type == TYPE_JOIN:
                    channel_id_lower = parsed.get('channel_id', '').lower()
                    if "timeoutjoin" in channel_id_lower:
                        print_log(thread_name, "*** Simulating JOIN REPLY Timeout triggered by ChannelID ***")
                        # Send CONFIRM, but no REPLY
                        send_standard_reply = False
                    elif "failjoin" in channel_id_lower:
                         print_log(thread_name, "*** Simulating JOIN REPLY NOK triggered by ChannelID ***")
                         server_msg_id_counter += 1
                         reply_msg = build_reply(server_msg_id_counter, False, parsed['msg_id'], "Join failed (channel trigger).")
                         handler_sock.sendto(reply_msg, client_addr)
                         print_log(thread_name, f"Sent REPLY NOK for JOIN (ID={server_msg_id_counter})")
                         send_standard_reply = False # Don't send the success reply too
                    elif "duplicatejoin" in channel_id_lower:
                         print_log(thread_name, "*** Simulating Duplicate JOIN REPLY triggered by ChannelID ***")
                         # Send standard success reply first (later in the code)
                         # Will send second reply after standard processing
                         pass # Standard processing will send first reply

                elif msg_type == TYPE_MSG:
                    content_lower = parsed.get('content', '').lower()
                    if "noconfirm" in content_lower:
                        print_log(thread_name, "*** Simulating CONFIRM Loss for MSG triggered by content ***")
                        send_confirm = False
                        # Still process standard reply if needed
                    elif "duplicatemsg" in content_lower:
                        print_log(thread_name, "*** Simulating Duplicate Server MSG triggered by content ***")
                        # Standard processing will send first reply
                        # Will send second reply after standard processing
                        pass
                    elif "servererr" in content_lower:
                         print_log(thread_name, "*** Sending ERR triggered by MSG content ***")
                         server_msg_id_counter += 1
                         err_msg = build_err(server_msg_id_counter, "Server", "ERR triggered by client.")
                         handler_sock.sendto(err_msg, client_addr)
                         print_log(thread_name, f"Sent ERR (ID={server_msg_id_counter}). Closing.")
                         send_confirm = True # Confirm the trigger message
                         send_standard_reply = False
                         break # Exit loop
                    elif "serverbye" in content_lower:
                         print_log(thread_name, "*** Sending BYE triggered by MSG content ***")
                         server_msg_id_counter += 1
                         bye_msg = build_bye(server_msg_id_counter, "Server")
                         handler_sock.sendto(bye_msg, client_addr)
                         print_log(thread_name, f"Sent BYE (ID={server_msg_id_counter}). Closing.")
                         send_confirm = True # Confirm the trigger message
                         send_standard_reply = False
                         break # Exit loop
                    elif "malformed" in content_lower:
                         print_log(thread_name, f"*** Sending Malformed MSG triggered by MSG content ***")
                         server_msg_id_counter += 1
                         malformed_msg_bytes = build_msg(server_msg_id_counter, "Server", "This message is malformed")[:-1] # Remove null
                         handler_sock.sendto(malformed_msg_bytes, client_addr)
                         print_log(thread_name, f"Sent Malformed MSG (ID={server_msg_id_counter}).")
                         send_confirm = False # Don't confirm the trigger message
                         send_standard_reply = False
                         # Don't break, let client handle it

                # B) Send CONFIRM if not suppressed
                if msg_type != TYPE_CONFIRM and send_confirm:
                    confirm_pkt = build_confirm(parsed['msg_id'])
                    handler_sock.sendto(confirm_pkt, client_addr)
                    print_log(thread_name, f"Sent CONFIRM for received ClientMsgID={parsed['msg_id']}")

                # --- Standard Message Processing (if not skipped by a trigger) ---
                if send_standard_reply:
                    if msg_type == TYPE_JOIN:
                        channel_id_lower = parsed.get('channel_id', '').lower() # Get again for duplicate check
                        print_log(thread_name, f"JOIN received: Channel='{parsed.get('channel_id','')}', DName='{parsed.get('display_name', '')}'")
                        # Standard Reply OK for JOIN
                        server_msg_id_counter += 1
                        reply_content = f"Join to '{parsed.get('channel_id','')}' successful."
                        reply_msg = build_reply(server_msg_id_counter, True, parsed['msg_id'], reply_content)
                        handler_sock.sendto(reply_msg, client_addr)
                        print_log(thread_name, f"Sent standard REPLY OK for JOIN (ID={server_msg_id_counter})")

                        # Check for duplicate trigger *after* sending first reply
                        if "duplicatejoin" in channel_id_lower:
                           print_log(thread_name, "*** Sending Duplicate JOIN REPLY now ***")
                           time.sleep(0.1)
                           handler_sock.sendto(reply_msg, client_addr) # Send same reply again

                        # Standard joining channel message
                        time.sleep(0.1)
                        server_msg_id_counter += 1
                        join_notice_content = f"{client_state.get('display_name', 'Unknown')} has joined {parsed.get('channel_id','')}"
                        join_notice_msg = build_msg(server_msg_id_counter, "Server", join_notice_content)
                        handler_sock.sendto(join_notice_msg, client_addr)
                        print_log(thread_name, f"Sent standard MSG join notice (ID={server_msg_id_counter})")

                    elif msg_type == TYPE_MSG:
                        content_lower = parsed.get('content', '').lower() # Get again for duplicate check
                        print_log(thread_name, f"MSG received: From='{parsed.get('display_name', '')}', Content='{parsed.get('content', '')[:50]}...'")
                        # Standard reply MSG
                        server_msg_id_counter += 1
                        reply_content = f"Got your MSG: '{parsed.get('content', '')[:20]}...'"
                        server_msg_bytes = build_msg(server_msg_id_counter, "Server", reply_content)
                        handler_sock.sendto(server_msg_bytes, client_addr)
                        print_log(thread_name, f"Sent standard reply MSG (ID={server_msg_id_counter})")

                        # Check for duplicate trigger *after* sending first reply
                        if "duplicatemsg" in content_lower:
                            print_log(thread_name, "*** Sending Duplicate reply MSG now ***")
                            time.sleep(0.1)
                            handler_sock.sendto(server_msg_bytes, client_addr) # Send same reply again

                    elif msg_type == TYPE_BYE:
                        print_log(thread_name, f"BYE received from '{parsed.get('display_name', '')}'. Closing connection.")
                        break # Standard processing is just to break

                    elif msg_type == TYPE_CONFIRM:
                        print_log(thread_name, f"CONFIRM received for ServerMsgID={parsed['ref_msg_id']}")
                        pass # No standard action needed

                    elif msg_type != TYPE_JOIN: # Avoid double logging for JOIN already handled
                         print_log(thread_name, f"Received unhandled message type in handler: {hex(msg_type)}")


            # --- Error Handling for the loop ---
            except socket.timeout: print_log(thread_name, "Client timed out."); break
            except ConnectionResetError: print_log(thread_name, "Connection reset."); break
            except Exception as e:
                print_log(thread_name, f"Error in handler loop: {e}")
                # Attempt to send ERR
                try:
                    server_msg_id_counter += 1
                    err_msg = build_err(server_msg_id_counter, "Server", f"Handler error: {type(e).__name__}")
                    handler_sock.sendto(err_msg, client_addr)
                except Exception: pass # Ignore error during error sending
                break

    finally:
        print_log(thread_name, "Closing handler socket.")
        handler_sock.close()

# --- Message Parser (same as before, ensure it handles missing fields gracefully) ---
def parse_message(data, addr, log_context="Parser"):
    try:
        if len(data) < 3: raise ValueError(f"Msg from {addr} too short ({len(data)}B)")
        msg_type = data[0]; msg_id = read_ushort_be(data, 1); offset = 3
        result = {'raw_type': msg_type, 'msg_id': msg_id}
        if msg_type == TYPE_CONFIRM:
            if len(data) < 5: raise ValueError("CONFIRM too short")
            result['type'] = TYPE_CONFIRM; result['ref_msg_id'] = read_ushort_be(data, offset)
        elif msg_type == TYPE_REPLY: # Client shouldn't receive REPLY in this simulation
             result['type'] = TYPE_UNKNOWN; print_log(log_context, f"Client received unexpected REPLY from {addr}")
        elif msg_type == TYPE_AUTH:
            result['type'] = TYPE_AUTH
            result['username'], offset = read_string(data, offset)
            result['display_name'], offset = read_string(data, offset)
            result['secret'], offset = read_string(data, offset)
        elif msg_type == TYPE_JOIN:
            result['type'] = TYPE_JOIN
            result['channel_id'], offset = read_string(data, offset)
            result['display_name'], offset = read_string(data, offset)
        elif msg_type == TYPE_MSG:
            result['type'] = TYPE_MSG
            result['display_name'], offset = read_string(data, offset)
            result['content'], offset = read_string(data, offset)
        elif msg_type == TYPE_ERR:
            result['type'] = TYPE_ERR
            result['display_name'], offset = read_string(data, offset)
            result['content'], offset = read_string(data, offset)
        elif msg_type == TYPE_BYE:
            result['type'] = TYPE_BYE
            result['display_name'], offset = read_string(data, offset)
        elif msg_type == TYPE_PING: result['type'] = TYPE_PING
        else: result['type'] = TYPE_UNKNOWN; print_log(log_context, f"Unknown type {hex(msg_type)} from {addr}")
        return result
    except (ValueError, IndexError, UnicodeDecodeError, struct.error) as e:
        print_log(log_context, f"Parse error from {addr}: {e}")
        # Maybe try to send ERR? Difficult if parsing failed.
        return None # Indicate parsing failure


# --- Main Server (unchanged from previous version) ---
def run_server(host='127.0.0.1', port=DEFAULT_PORT):
    listen_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    listen_sock.bind((host, port))
    print_log("Server", f"Listening on UDP {host}:{port}")
    active_threads = []
    running = True
    def signal_handler(sig, frame):
        nonlocal running; print_log("Server", "Shutdown signal..."); running = False; listen_sock.close()
    signal.signal(signal.SIGINT, signal_handler); signal.signal(signal.SIGTERM, signal_handler)
    while running:
        try:
            # print_log("Server", "Waiting on listener...") # Optional log
            listen_sock.settimeout(1.0)
            try: data, client_addr = listen_sock.recvfrom(BUFFER_SIZE)
            except socket.timeout: continue
            print_log("Server", f"Received {len(data)} bytes from {client_addr} on listener")
            if len(data) < 3: continue
            msg_type = data[0]; initial_msg_id = read_ushort_be(data, 1)
            if msg_type == TYPE_AUTH:
                 confirm_pkt = build_confirm(initial_msg_id)
                 listen_sock.sendto(confirm_pkt, client_addr)
                 print_log("Server", f"Sent CONFIRM for AUTH (RefID={initial_msg_id}) from listener")
                 handler_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM); handler_sock.bind((host, 0))
                 dynamic_port = handler_sock.getsockname()[1]
                 print_log("Server", f"Allocated dynamic port {dynamic_port} for {client_addr}")
                 handler_thread = threading.Thread(target=client_handler, args=(handler_sock, client_addr, data, initial_msg_id), daemon=True)
                 active_threads.append(handler_thread); handler_thread.start()
                 active_threads = [t for t in active_threads if t.is_alive()] # Cleanup finished
            else: print_log("Server", f"Ignoring non-AUTH msg type {hex(msg_type)} on listener")
        except OSError as e:
             if running: print_log("Server", f"Listener socket error: {e}")
             else: print_log("Server", "Listener socket closed.")
             break
        except Exception as e: print_log("Server", f"Main loop error: {e}"); time.sleep(1)
    print_log("Server", "Shutting down handlers..."); shutdown_start = time.time()
    for t in active_threads:
        join_timeout = max(0.1, 5.0 - (time.time() - shutdown_start)); t.join(timeout=join_timeout)
        if t.is_alive(): print_log("Server", f"Warning: Thread {t.name} did not exit.")
    print_log("Server", "Shutdown complete.")

if __name__ == "__main__":
    run_server()