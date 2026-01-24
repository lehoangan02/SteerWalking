import json
import socket

from TrackerSource.abc_tracker import TrackerSource


class UdpTrackerSource(TrackerSource):
    def __init__(self, ip="0.0.0.0", port=9002, timeout_s=0.05):
        # Bind a UDP socket to receive position samples
        self.addr = (ip, port)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        # Improve restart behavior; still fails if another process actively binds same port.
        try:
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        except Exception:
            pass
        try:
            if hasattr(socket, "SO_REUSEPORT"):
                self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)
        except Exception:
            pass
        self.sock.bind(self.addr)
        self.sock.settimeout(timeout_s)
        self.latest_pos = None

    def shutdown(self):
        try:
            self.sock.close()
        except Exception:
            pass

    def get_tracker_position(self):
        try:
            data, _ = self.sock.recvfrom(4096)
        except socket.timeout:
            # No new data; return the most recent position if available
            return self.latest_pos
        except Exception:
            return self.latest_pos

        try:
            msg = json.loads(data.decode("utf-8"))
        except Exception:
            return self.latest_pos

        if all(k in msg for k in ("x", "y", "z")):
            pos = (float(msg["x"]), float(msg["y"]), float(msg["z"]))
            self.latest_pos = pos
            return pos

        return self.latest_pos
