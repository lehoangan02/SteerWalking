from rudder_receiver import RudderReceiver
from vive_trackers import ViveTrackers


class DeviceManager:
    def __init__(self, rudder_port=5067):
        self.rudder = RudderReceiver(port=rudder_port)
        self.trackers = ViveTrackers()

    def get(self):
        """
        Returns:
            rotate (int)
            tracker1 (tuple | None)
            tracker2 (tuple | None)
        """
        rudder_state = self.rudder.get()
        t1, t2 = self.trackers.get_tracker_positions()

        return (
            rudder_state["rotate"],
            t1,
            t2
        )

    def shutdown(self):
        self.trackers.shutdown()
