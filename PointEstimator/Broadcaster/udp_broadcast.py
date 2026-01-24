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
        self.num_point_init = 10
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
    
    def angle_diff_deg(self,curr, prev):
        diff = curr - prev
        if diff > 180:
            diff -= 360
        elif diff < -180:
            diff += 360
        return diff


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
        
        # =========================================================
        # [FIX] ALIGNMENT SETTINGS
        # Change these if the character is wrong!
        # =========================================================
        REVERSE_DIRECTION = False   # Set True if Left/Right are swapped
        ANGLE_OFFSET = 180.0        # Set to 0, 90, 180, or 270 to rotate starting point
        # =========================================================

        # 1. Apply Reversal (CW vs CCW)
        if REVERSE_DIRECTION:
            angle_deg = 360.0 - angle_deg

        # 2. Apply Offset (Fixes "Opposite Side" issue)
        angle_deg = (angle_deg + ANGLE_OFFSET) % 360.0

        ts = time.time()
        
        # --- VELOCITY CALCULATION ---
        dt = ts - self.prev_time if self.prev_time else 0.0
        
        if dt > 0:
            d_angle = self.angle_diff_deg(angle_deg, self.prev_ang)
            angular_velocity = d_angle / dt
        else:
            angular_velocity = 0.0

        self.prev_time = ts
        self.prev_ang = angle_deg
        
        # We send ABS(velocity) because Unity just wants "Speed", 
        # the direction is handled by "angle_deg"
        packet = json.dumps(
            {
                "angle_deg": -angle_deg,
                "angular_velocity": abs(angular_velocity), 
                "ts": ts,
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)
        
        # Verify in terminal
        # print(f"Raw: {angle_deg:.1f} | Vel: {angular_velocity:.1f}")
        
        return angle_deg, angular_velocity
        
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

    def send_yline(self):
        if self.yline is None:
            return

        origin, highest = self.yline
        packet = json.dumps(
            {
                "origin": origin,
                "highest": highest,
                "ts": time.time(),
                "type": "yline",
            }
        ).encode("utf-8")
        self.sock.sendto(packet, self.addr)
