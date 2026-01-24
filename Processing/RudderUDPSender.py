import sys
import time
import json
import socket
import argparse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from Devices.SteamVRTracking.ViveTrackers import ViveTrackers


UDP_IP = "255.255.255.255"
UDP_PORT = 9002


class RudderUDPSender:
    def __init__(self):
        self.vt = ViveTrackers()
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

    def step(self):
        roll, pitch, yaw = self.vt.get_tracker_2_rotation()

        payload = {
            "rudder_deg": pitch,
            "ts": time.time()
        }

        self.sock.sendto(
            json.dumps(payload).encode("utf-8"),
            (UDP_IP, UDP_PORT)
        )

    def shutdown(self):
        self.vt.shutdown()


if __name__ == "__main__":
    sender = RudderUDPSender()

    try:
        while True:
            sender.step()
            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nShutting down...")

    finally:
        sender.shutdown()
