import sys
import time
import json
import socket
import argparse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from sympy import Point3D
from Processing.TrackPhaseProcessor import TrackPhaseProcessor


UDP_IP = "255.255.255.255"
UDP_PORT = 9000


class AngleUdpSender:
    def __init__(self, device_manager, center):
        self.dm = device_manager
        self.processor = TrackPhaseProcessor(center=center)

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

        self.prev_phase = None
        self.prev_time = None

    def step(self):
        _, A1, _ = self.dm.get()
        if A1 is None:
            return

        now = time.time()
        phase = self.processor.get_phase(Point3D(*A1))

        angular_velocity = 0.0
        if self.prev_phase is not None:
            dphi = phase - self.prev_phase

            if dphi > 180:
                dphi -= 360
            elif dphi < -180:
                dphi += 360

            dt = now - self.prev_time
            if dt > 0:
                angular_velocity = dphi / dt

        payload = {
            "angle_deg": phase,
            "angular_velocity": angular_velocity,
            "ts": now
        }

        self.sock.sendto(
            json.dumps(payload).encode("utf-8"),
            (UDP_IP, UDP_PORT)
        )

        self.prev_phase = phase
        self.prev_time = now


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--mode",
        choices=["simulated", "real"],
        default="simulated"
    )

    args = parser.parse_args()

    if args.mode == "simulated":
        from Simulation.SimulatedDeviceManager import SimulatedDeviceManager
        dm = SimulatedDeviceManager()
    else:
        from Devices.DeviceManager import DeviceManager
        dm = DeviceManager()

    sender = AngleUdpSender(
        device_manager=dm,
        center=Point3D(0, 0, 1)
    )

    try:
        while True:
            sender.step()
            time.sleep(0.02)

    finally:
        dm.shutdown()
