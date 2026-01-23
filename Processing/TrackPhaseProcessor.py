from geometry_utils import make_point, angle
import time


class TrackerPhaseProcessor:
    def __init__(self, origin=(0, 0, 0)):
        self.origin = make_point(*origin)
        self.prev_point = None
        self.phase = 0.0

    def update(self, device_manager):
        """
        Reads data from DeviceManager / SimulatedDeviceManager

        Returns:
            phase (float, degrees)
        """
        rotate, A1, _ = device_manager.get()

        if A1 is None:
            return self.phase

        current = make_point(*A1)

        if self.prev_point is None:
            self.prev_point = current
            return self.phase

        dtheta = angle(
            prev=self.prev_point,
            after=current,
            origin=self.origin
        )

        self.phase += float(dtheta)
        self.prev_point = current

        return self.phase


if __name__ == "__main__":
    from Simulation.SimulatedDeviceManager import SimulatedDeviceManager

    dm = SimulatedDeviceManager()
    processor = TrackerPhaseProcessor(origin=(0, 0, 0))

    try:
        while True:
            phase = processor.update(dm)

            print(f"A1 rotation phase: {phase:.3f} deg")

            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nStopping tracker phase processing...")
