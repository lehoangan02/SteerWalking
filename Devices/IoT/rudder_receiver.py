import socket
import json
import threading

class RudderReceiver:
    def __init__(self, port=5067):
        self.state = {
            "rotate": 0,
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
                self.state["rotate"] = msg["rotate"]
                self.state["button"] = msg["button"]

    def get(self):
        return self.state.copy()
