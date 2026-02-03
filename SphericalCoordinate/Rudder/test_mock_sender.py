#!/usr/bin/env python3
"""Mock magnetic encoder sender - simulates what the Pi sends"""

import socket
import json
import time
import threading

class MockMagneticEncoder:
    """Simulates the MagneticEncoder from Raspberry Pi"""
    def __init__(self, udp_host="255.255.255.255", udp_port=9002):
        self.udp_host = udp_host
        self.udp_port = udp_port
        self.angle = 0.0
        
        # Create UDP socket with broadcast enabled
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        print(f"[TX] Created broadcast socket to {udp_host}:{udp_port}")
    
    def send_udp(self, angle_deg):
        """Send angle data via UDP"""
        message = {
            "type": "input",
            "angle_deg": angle_deg
        }
        data = json.dumps(message).encode()
        try:
            bytes_sent = self.sock.sendto(data, (self.udp_host, self.udp_port))
            print(f"[TX] Sent {bytes_sent} bytes: angle={angle_deg:.1f}° to {self.udp_host}:{self.udp_port}")
        except Exception as e:
            print(f"[TX] ERROR: {e}")
    
    def run(self):
        """Simulate sensor readings"""
        for i in range(10):
            self.angle = (self.angle + 5) % 360.0
            self.send_udp(self.angle)
            time.sleep(0.1)

from MagneticEncoderReceiver import MagneticEncoderReceiver

if __name__ == "__main__":
    print("=" * 60)
    print("MOCK SENDER + ACTUAL RECEIVER TEST")
    print("=" * 60)
    
    # Start receiver in background
    print("\n[INIT] Starting receiver on port 9002...")
    receiver = MagneticEncoderReceiver(port=9002)
    time.sleep(0.5)
    
    # Start sender
    print("\n[INIT] Starting mock sender...")
    sender = MockMagneticEncoder(udp_host="255.255.255.255", udp_port=9002)
    
    # Run sender for a few packets
    print("\n[RUN] Sending 10 packets...\n")
    sender.run()
    
    # Check receiver stats
    print("\n" + "=" * 60)
    print("RECEIVER STATS:")
    stats = receiver.get_stats()
    print(f"  Packets received: {stats['packets_received']}")
    print(f"  Packets dropped: {stats['packets_dropped']}")
    print(f"  Last error: {stats['last_error']}")
    print(f"  Current angle: {receiver.get()['angle_deg']:.1f}°")
    print("=" * 60)
    
    if stats['packets_received'] > 0:
        print("✓ SUCCESS: Receiver is working!")
    else:
        print("✗ FAILED: Receiver got no packets")
