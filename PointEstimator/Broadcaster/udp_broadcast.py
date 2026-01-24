import json
import socket
import time

from Utils.polarUtils import (
    best_fit_3d_circle, line_origin_to_highest_y,
    angle_deg_from_highest
)

class TrackerUdpBroadcaster:
    def __init__(self, ip="255.255.255.255", port=9000, broadcast=True):
        
        # --- Network ---
        self.addr = (ip, port)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        if broadcast:
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            
        # --- Logic ---
        self.circle = None # center(x,y,z) | normal(x,y,z) | radius
        self.center = None
        self.v_norm = None
        self.redius = None
        self.yline = None
        self.num_point_init = 100
        self._init_points = []
        
        self.prev_ang = 0
        self.prev_time = 0

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
            self.center, self.v_norm, self.radius = self.circle
            self.yline = line_origin_to_highest_y(self._init_points, self.center)
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
        
    def send_degree_position(self, pos):
        if pos is None:
            return

        if self.center is None or self.yline is None:
            return

        origin, highest = self.yline
        angle_deg = angle_deg_from_highest(origin, highest, pos)
        ts = time.time()
        dt = ts - self.prev_time if self.prev_time else 0.0
        angular_velocity = (angle_deg - self.prev_ang) / dt if dt > 0 else 0.0
        self.prev_time = ts
        self.prev_ang = angle_deg
        
        packet = json.dumps(
            {
                "angle_deg": angle_deg,
                "angular_velocity": angular_velocity,
                "ts": ts,
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)
        return angle_deg

    def send_circle(self):
        if self.circle is None:
            return
        
        packet = json.dumps(
            {
                "center": self.center,
                "normal": self.v_norm,
                "radius": self.radius,
                "ts": time.time(),
                "type": "circle",
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)
