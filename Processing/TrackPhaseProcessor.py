import sys
import time
import argparse
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from sympy import atan2, deg
from sympy import Point3D


class TrackPhaseProcessor:
    def __init__(self, center):
        self.center = center

    def get_phase(self, tracker):
        """
        Returns absolute phase in degrees [0, 360)
        """
        v_x = tracker.x - self.center.x
        v_y = tracker.y - self.center.y

        phase = deg(atan2(v_y, v_x))

        if phase < 0:
            phase += 360

        return float(phase)


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

    processor = TrackPhaseProcessor(center=Point3D(0, 0, 1))

    try:
        while True:
            _, A1, _ = dm.get()

            if A1 is not None:
                A1_p = Point3D(*A1)
                phase = processor.get_phase(A1_p)

                print(f"A1 rotation phase: {phase:.3f} deg")
                print(f"A1 Position: {A1}")

            time.sleep(0.02)

    finally:
        dm.shutdown()
