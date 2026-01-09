import openvr
import time
import math

# Initialize OpenVR
openvr.init(openvr.VRApplication_Other)
vr = openvr.VRSystem()

# Helper: Get serial number
def get_device_serial(vr, index):
    try:
        return vr.getStringTrackedDeviceProperty(
            index, openvr.Prop_SerialNumber_String
        )
    except:
        return "UNKNOWN"

# Helper: Get tracker role (Tracker 1/2/3 in SteamVR)
def get_tracker_role(vr, index):
    try:
        role = vr.getInt32TrackedDeviceProperty(
            index, openvr.Prop_TrackerRoleHint_Int32
        )
        return openvr.ETrackerRole(role).name
    except:
        return "Unknown"

# Helper: Convert OpenVR pose matrix to position + quaternion
def get_pose_matrix(pose):
    m = pose.mDeviceToAbsoluteTracking
    # Position
    x = m[0][3]
    y = m[1][3]
    z = m[2][3]
    # Rotation as quaternion
    qw = math.sqrt(max(0, 1 + m[0][0] + m[1][1] + m[2][2])) / 2
    qx = math.sqrt(max(0, 1 + m[0][0] - m[1][1] - m[2][2])) / 2
    qy = math.sqrt(max(0, 1 - m[0][0] + m[1][1] - m[2][2])) / 2
    qz = math.sqrt(max(0, 1 - m[0][0] - m[1][1] + m[2][2])) / 2
    qx = math.copysign(qx, m[2][1] - m[1][2])
    qy = math.copysign(qy, m[0][2] - m[2][0])
    qz = math.copysign(qz, m[1][0] - m[0][1])
    return (x, y, z), (qx, qy, qz, qw)

try:
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
                role = get_tracker_role(vr, i)
                trackers.append((i, serial, pose_valid, role))

        # ---- STATUS REPORT ----
        print("\n===== SteamVR Tracking Status =====")
        print(f"Base Stations detected: {len(base_stations)}")
        for i, serial, valid in base_stations:
            print(f"  BaseStation {i} | {serial} | Pose valid: {valid}")

        print(f"Trackers detected: {len(trackers)}")
        for i, serial, valid, role in trackers:
            if valid:
                pos, rot = get_pose_matrix(poses[i])
                print(f"  Tracker {i} | {serial} | Role: {role} | Pose valid: {valid}")
                print(f"    Position: {pos}")
                print(f"    Rotation (quat): {rot}")
            else:
                print(f"  Tracker {i} | {serial} | Role: {role} | Pose INVALID")

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

except KeyboardInterrupt:
    print("\nExiting...")
finally:
    openvr.shutdown()
