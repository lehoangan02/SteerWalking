import socket
import json
import threading

DEGREES_PER_STATE = 24


class RudderReceiver:
    def __init__(self, port=5067):
        self.state = {
            "rotate": 0.0,
            "button": 0
        }

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(("", port))

        self.thread = threading.Thread(target=self._listen, daemon=True)
        self.thread.start()

    def _listen(self):
        while True:
            data, _ = self.sock.recvfrom(1024)
            msg = json.loads(data.decode())

            if msg.get("type") == "rudder":
                raw_state = msg["rotate"]

                # Convert state (1..9) → degrees
                self.state["rotate"] = raw_state * DEGREES_PER_STATE
                self.state["button"] = msg["button"]

    def get(self):
        return self.state.copy()
