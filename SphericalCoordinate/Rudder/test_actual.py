#!/usr/bin/env python3
"""Test the actual MagneticUdp sender"""

import sys
sys.path.insert(0, '/home/pi')  # Adjust if needed

from MagneticUdp import MagneticEncoder
from MagneticEncoderReceiver import MagneticEncoderReceiver
import threading
import time
import json

def test_sender():
    """Test that sender can at least open the socket"""
    print("[SENDER TEST]")
    try:
        encoder = MagneticEncoder(udp_host="255.255.255.255", udp_port=9002)
        print(f"✓ Sender socket created successfully")
        
        # Test sending a fake angle
        encoder.send_udp(90.0)
        print(f"✓ Test message sent to 255.255.255.255:9002")
        encoder.sock.close()
        return True
    except Exception as e:
        print(f"✗ Sender error: {e}")
        return False

def test_receiver():
    """Test that receiver can listen"""
    print("\n[RECEIVER TEST]")
    try:
        receiver = MagneticEncoderReceiver(port=9002)
        time.sleep(0.5)
        
        # Try to get state
        state = receiver.get()
        stats = receiver.get_stats()
        print(f"✓ Receiver initialized on port 9002")
        print(f"  - State: {state}")
        print(f"  - Stats: {stats}")
        return True
    except Exception as e:
        print(f"✗ Receiver error: {e}")
        return False

if __name__ == "__main__":
    print(f"Platform: {sys.platform}\n")
    test_sender()
    test_receiver()
