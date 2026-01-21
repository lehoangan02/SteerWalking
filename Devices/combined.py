import openvr
import time
import math
from gpiozero import RotaryEncoder, Button

# ---------------- GPIO ----------------

rotor = RotaryEncoder(a=17, b=18, max_steps=0)
button = Button(22)

# ---------------- SteamVR ----------------

openvr.init(openvr.VRApplication_Other)
vr = openvr.VRSystem()

def get_position(pose):
    m = pose.mDeviceToAbsoluteTracking
    return (m[0][3], m[1][3], m[2][3])

print("Running... Ctrl+C to exit")

try:
    while True:
        # ---- SteamVR ----
        poses = vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )

        tracker_positions = []

        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if poses[i].bPoseIsValid and \
               vr.getTrackedDeviceClass(i) == openvr.TrackedDeviceClass_GenericTracker:
                tracker_positions.append((i, get_position(poses[i])))

        # ---- GPIO ----
        encoder_value = rotor.steps
        button_state = button.is_pressed

        # ---- OUTPUT ----
        print("\n--- FRAME ---")
        print(f"Encoder steps: {encoder_value}")
        print(f"Button pressed: {button_state}")

        for i, pos in tracker_positions:
            print(f"Tracker {i}: {pos}")

        time.sleep(0.02)  # 50 Hz

except KeyboardInterrupt:
    pass
finally:
    openvr.shutdown()
