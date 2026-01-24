import json
import socket
import time

from Utils.polarUtils import best_fit_3d_circle


class TrackerUdpBroadcaster:
    def __init__(self, ip="255.255.255.255", port=9000, broadcast=True):
        
        # --- Network ---
        self.addr = (ip, port)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        if broadcast:
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            
        # --- Logic ---
        self.circle = None # center(x,y,z) | normal(x,y,z) | radius
        self.num_point_init = 100
        self._init_points = []

    def close(self):
        self.sock.close()
        
    def get_circle(self):
        return self.circle

    def _update_circle(self, pos):
        if self.circle is not None:
            return True

        self._init_points.append([pos[0], pos[1], pos[2]])
        if len(self._init_points) >= self.num_point_init:
            self.circle = best_fit_3d_circle(self._init_points)
            self._init_points = []
            return True
        return False

    def send_xyz_position(self, pos):
        if pos is None:
            return

        packet = json.dumps(
            {
                "x": pos[0],
                "y": pos[1],
                "z": pos[2],
                "ts": time.time(),
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)

    def send_circle(self):
        if self.circle is None:
            return

        center, normal, radius = self.circle
        packet = json.dumps(
            {
                "center": center,
                "normal": normal,
                "radius": radius,
                "ts": time.time(),
                "type": "circle",
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)
