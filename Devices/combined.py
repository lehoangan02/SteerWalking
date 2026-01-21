import openvr
import time
import math
import socket
import json
from gpiozero import RotaryEncoder, Button
from anmath import *

# ===================== UDP =====================

UDP_IP = "255.255.255.255"
UDP_PORT = 5069

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

# ===================== GPIO =====================

rotor = RotaryEncoder(a=17, b=18, max_steps=0)
button = Button(22)

# ===================== SteamVR =====================

openvr.init(openvr.VRApplication_Other)
vr = openvr.VRSystem()

def get_position(pose):
    m = pose.mDeviceToAbsoluteTracking
    return (m[0][3], m[1][3], m[2][3])

print("Streaming SteamVR + GPIO via UDP (port 5069)")
print("Press Ctrl+C to stop")

# ===================== MAIN LOOP =====================

frame_index = 0
tracker1_init_positions = []
previous_tracker1_position = None
center_tracker1_position = None

try:
    while True:
        # ---- SteamVR ----
        poses = vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )

        trackers = []

        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if not poses[i].bPoseIsValid:
                continue
            if vr.getTrackedDeviceClass(i) == openvr.TrackedDeviceClass_GenericTracker:
                pos = get_position(poses[i])
                trackers.append({
                    "id": i,
                    "pos": pos
                })

        # ---- GPIO ----
        encoder_steps = rotor.steps
        button_pressed = button.is_pressed

        rudder_degree = encoder_steps * 24 # KY040 has 30 detents per revolution, 2 detents per step
        tracker1X = trackers[0]["pos"][0]
        tracker1Y = trackers[0]["pos"][1]
        tracker1Z = trackers[0]["pos"][2]
        tracker2X = trackers[1]["pos"][0]
        tracker2Y = trackers[1]["pos"][1]
        tracker2Z = trackers[1]["pos"][2]
        origin = make_midpoint(
            make_point(tracker1X, tracker1Y, tracker1Z),
            make_point(tracker2X, tracker2Y, tracker2Z)
        )
        tracker1_pos = make_point(tracker1X, tracker1Y, tracker1Z)
        tracker1_rotated_pos = rotate_around_ground_normal(
            tracker1_pos,
            origin,
            -rudder_degree
        )
        if len(tracker1_init_positions) < 3:
            tracker1_init_positions.append(tracker1_rotated_pos)
        elif len(tracker1_init_positions) == 3 and center_tracker1_position is None:
            center_tracker1_position, _, _ = three_point_circle(
                tracker1_init_positions[0],
                tracker1_init_positions[1],
                tracker1_init_positions[2]
            )
        degree = 0
        if len(tracker1_init_positions) == 3:
            degree = angle(
                previous_tracker1_position,
                tracker1_rotated_pos,
                center_tracker1_position
            )


        # ---- PACKET ----
        packet = {
            "time": time.time(),
            "encoder": encoder_steps,
            "button": button_pressed,
            "trackers": trackers,
            "rudder_degree": rudder_degree,
            "cycle_degree": degree
        }
        if frame_index >= 4:
            sock.sendto(
                json.dumps(packet).encode("utf-8"),
                (UDP_IP, UDP_PORT)
            )
        frame_index += 1
        previous_tracker1_position = tracker1_rotated_pos
        time.sleep(0.02)  # 50 Hz
        

except KeyboardInterrupt:
    print("\nStopping...")

finally:
    openvr.shutdown()
    sock.close()
