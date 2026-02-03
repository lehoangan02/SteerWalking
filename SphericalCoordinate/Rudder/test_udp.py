#!/usr/bin/env python3
"""Simple UDP test to debug send/receive issues."""

import socket
import json
import sys
import threading
import time

def test_localhost():
    """Test UDP on localhost (127.0.0.1)"""
    print("\n=== Testing LOCALHOST (127.0.0.1:9003) ===")
    
    # Receiver
    def receiver():
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(("127.0.0.1", 9003))
        sock.settimeout(2.0)
        print("[RX] Listening on 127.0.0.1:9003")
        
        try:
            data, addr = sock.recvfrom(1024)
            msg = json.loads(data.decode('utf-8'))
            print(f"[RX] ✓ Received from {addr}: {msg}")
            return True
        except socket.timeout:
            print("[RX] ✗ TIMEOUT - no packets received")
            return False
        except Exception as e:
            print(f"[RX] ✗ ERROR: {e}")
            return False
        finally:
            sock.close()
    
    # Sender
    def sender():
        time.sleep(0.2)  # Let receiver bind first
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        msg = {"type": "input", "angle_deg": 45.0}
        try:
            sock.sendto(json.dumps(msg).encode(), ("127.0.0.1", 9003))
            print("[TX] ✓ Sent to 127.0.0.1:9003")
        except Exception as e:
            print(f"[TX] ✗ ERROR: {e}")
        finally:
            sock.close()
    
    t1 = threading.Thread(target=receiver, daemon=False)
    t2 = threading.Thread(target=sender, daemon=False)
    
    t1.start()
    t2.start()
    t1.join()
    t2.join()

def test_broadcast():
    """Test UDP broadcast"""
    print("\n=== Testing BROADCAST (255.255.255.255:9004) ===")
    
    # Receiver
    def receiver():
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(("", 9004))  # Bind to all interfaces
        sock.settimeout(2.0)
        print("[RX] Listening on 0.0.0.0:9004")
        
        try:
            data, addr = sock.recvfrom(1024)
            msg = json.loads(data.decode('utf-8'))
            print(f"[RX] ✓ Received from {addr}: {msg}")
            return True
        except socket.timeout:
            print("[RX] ✗ TIMEOUT - no packets received")
            return False
        except Exception as e:
            print(f"[RX] ✗ ERROR: {e}")
            return False
        finally:
            sock.close()
    
    # Sender
    def sender():
        time.sleep(0.2)  # Let receiver bind first
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        msg = {"type": "input", "angle_deg": 45.0}
        try:
            sock.sendto(json.dumps(msg).encode(), ("255.255.255.255", 9004))
            print("[TX] ✓ Sent to 255.255.255.255:9004 (broadcast)")
        except Exception as e:
            print(f"[TX] ✗ ERROR: {e}")
        finally:
            sock.close()
    
    t1 = threading.Thread(target=receiver, daemon=False)
    t2 = threading.Thread(target=sender, daemon=False)
    
    t1.start()
    t2.start()
    t1.join()
    t2.join()

def test_localhost_broadcast():
    """Test UDP broadcast to localhost"""
    print("\n=== Testing LOCALHOST BROADCAST (127.255.255.255:9005) ===")
    
    # Receiver
    def receiver():
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        sock.bind(("", 9005))
        sock.settimeout(2.0)
        print("[RX] Listening on 0.0.0.0:9005")
        
        try:
            data, addr = sock.recvfrom(1024)
            msg = json.loads(data.decode('utf-8'))
            print(f"[RX] ✓ Received from {addr}: {msg}")
            return True
        except socket.timeout:
            print("[RX] ✗ TIMEOUT - no packets received")
            return False
        except Exception as e:
            print(f"[RX] ✗ ERROR: {e}")
            return False
        finally:
            sock.close()
    
    # Sender
    def sender():
        time.sleep(0.2)
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        msg = {"type": "input", "angle_deg": 45.0}
        try:
            sock.sendto(json.dumps(msg).encode(), ("127.255.255.255", 9005))
            print("[TX] ✓ Sent to 127.255.255.255:9005")
        except Exception as e:
            print(f"[TX] ✗ ERROR: {e}")
        finally:
            sock.close()
    
    t1 = threading.Thread(target=receiver, daemon=False)
    t2 = threading.Thread(target=sender, daemon=False)
    
    t1.start()
    t2.start()
    t1.join()
    t2.join()

if __name__ == "__main__":
    print(f"Python {sys.version}")
    print(f"Platform: {sys.platform}")
    
    test_localhost()
    test_localhost_broadcast()
    test_broadcast()
    
    print("\n" + "="*50)
    print("SUMMARY:")
    print("- If localhost tests work, the issue is network/broadcast")
    print("- If broadcast fails, disable Windows Firewall for this port")
    print("- Or use localhost mode if possible")
