import openvr
import time

openvr.init(openvr.VRApplication_Other)
vr = openvr.VRSystem()

def get_device_serial(vr, index):
    try:
        return vr.getStringTrackedDeviceProperty(
            index, openvr.Prop_SerialNumber_String
        )
    except:
        return "UNKNOWN"

while True:
    poses = vr.getDeviceToAbsoluteTrackingPose(
        openvr.TrackingUniverseStanding,
        0,
        openvr.k_unMaxTrackedDeviceCount
    )

    base_stations = []
    trackers = []

    for i in range(openvr.k_unMaxTrackedDeviceCount):
        if not vr.isTrackedDeviceConnected(i):
            continue

        device_class = vr.getTrackedDeviceClass(i)
        serial = get_device_serial(vr, i)
        pose_valid = poses[i].bPoseIsValid

        if device_class == openvr.TrackedDeviceClass_TrackingReference:
            base_stations.append((i, serial, pose_valid))

        elif device_class == openvr.TrackedDeviceClass_GenericTracker:
            trackers.append((i, serial, pose_valid))

    # ---- STATUS REPORT ----
    print("\n===== SteamVR Tracking Status =====")

    print(f"Base Stations detected: {len(base_stations)}")
    for i, serial, valid in base_stations:
        print(f"  BaseStation {i} | {serial} | Pose valid: {valid}")

    print(f"Trackers detected: {len(trackers)}")
    for i, serial, valid in trackers:
        print(f"  Tracker {i} | {serial} | Pose valid: {valid}")

    # ---- EXPECTATION CHECK ----
    if len(base_stations) >= 2:
        print("✔ Required base stations detected")
    else:
        print("✖ Missing base stations")

    if len(trackers) >= 2:
        print("✔ Required trackers detected")
    else:
        print("✖ Missing trackers")

    time.sleep(1)
