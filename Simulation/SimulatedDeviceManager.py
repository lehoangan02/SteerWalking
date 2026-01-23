import socket
import json
import threading


class SimulatedDeviceManager:
    def __init__(self, port=9001):
        self.state = {
            "rotate": 0.0,
            "A1": None,
            "A2": None
        }

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(("", port))

        self.thread = threading.Thread(target=self._listen, daemon=True)
        self.thread.start()

    def _listen(self):
        while True:
            data, _ = self.sock.recvfrom(2048)
            msg = json.loads(data.decode())

            # Expecting data from cycle3d.py
            # {
            #   "A1": {"x": ..., "y": ..., "z": ...},
            #   "A2": {"x": ..., "y": ..., "z": ...},
            #   "rudder_deg": ...
            # }

            A1 = msg.get("A1")
            A2 = msg.get("A2")

            if A1 is not None and A2 is not None:
                self.state["A1"] = (
                    A1["x"],
                    A1["y"],
                    A1["z"]
                )

                self.state["A2"] = (
                    A2["x"],
                    A2["y"],
                    A2["z"]
                )

            if "rudder_deg" in msg:
                self.state["rotate"] = msg["rudder_deg"]

    def get(self):
        """
        Same return format as DeviceManager:

        Returns:
            rotate (float, degrees)
            tracker1 (tuple | None)
            tracker2 (tuple | None)
        """
        return (
            self.state["rotate"],
            self.state["A1"],
            self.state["A2"]
        )

    def shutdown(self):
        pass
