import openvr
import time
import math

class ViveTracker:
    def __init__(self):
        openvr.init(openvr.VRApplication_Other)
        self.vr = openvr.VRSystem()
        self.device_class_names = {
            openvr.TrackedDeviceClass_Invalid: "Invalid",
            openvr.TrackedDeviceClass_HMD: "HMD",
            openvr.TrackedDeviceClass_Controller: "Controller",
            openvr.TrackedDeviceClass_GenericTracker: "Tracker",
            openvr.TrackedDeviceClass_TrackingReference: "BaseStation",
            openvr.TrackedDeviceClass_DisplayRedirect: "DisplayRedirect",
        }

    def shutdown(self):
        openvr.shutdown()

    def _get_pose_position(self, pose):
        m = pose.mDeviceToAbsoluteTracking
        return (
            m[0][3],
            m[1][3],
            m[2][3]
        )

    def get_tracker_positions(self):
        poses = self.vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )
        tracker_positions = []

        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if not self.vr.isTrackedDeviceConnected(i):
                continue

            if self.vr.getTrackedDeviceClass(i) != openvr.TrackedDeviceClass_GenericTracker:
                continue

            if poses[i].bPoseIsValid:
                tracker_positions.append(self._get_pose_position(poses[i]))
            else:
                # keep placeholder for invalid pose so order of trackers is preserved
                tracker_positions.append((0.0, 0.0, 0.0))

            if len(tracker_positions) >= 2:
                break

        # always return two position tuples (pad with zeros if fewer trackers)
        while len(tracker_positions) < 2:
            tracker_positions.append((0.0, 0.0, 0.0))

        return tracker_positions[0], tracker_positions[1]

    def get_tracker_rotations(self):
        poses = self.vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )
        tracker_rotations = []

        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if not self.vr.isTrackedDeviceConnected(i):
                continue

            if self.vr.getTrackedDeviceClass(i) != openvr.TrackedDeviceClass_GenericTracker:
                continue

            if poses[i].bPoseIsValid:
                R = self._rotation_matrix(poses[i])
                euler = self._matrix_to_euler_deg(R)
                tracker_rotations.append(euler)
            else:
                tracker_rotations.append((0.0, 0.0, 0.0))

            if len(tracker_rotations) >= 2:
                break

        while len(tracker_rotations) < 2:
            tracker_rotations.append((0.0, 0.0, 0.0))

        return tracker_rotations[0], tracker_rotations[1]

    def get_tracker_2_rotation(self):
        """Get roll, pitch, yaw (in degrees) of tracker 2 only."""
        _, tracker_2_rot = self.get_tracker_rotations()
        return tracker_2_rot

    def _rotation_matrix(self, pose):
        m = pose.mDeviceToAbsoluteTracking
        return (
            (m[0][0], m[0][1], m[0][2]),
            (m[1][0], m[1][1], m[1][2]),
            (m[2][0], m[2][1], m[2][2]),
        )

    def _matrix_to_quaternion(self, R):
        r00, r01, r02 = R[0]
        r10, r11, r12 = R[1]
        r20, r21, r22 = R[2]

        trace = r00 + r11 + r22
        if trace > 0:
            s = 0.5 / math.sqrt(trace + 1.0)
            w = 0.25 / s
            x = (r21 - r12) * s
            y = (r02 - r20) * s
            z = (r10 - r01) * s
        else:
            if r00 > r11 and r00 > r22:
                s = 2.0 * math.sqrt(1.0 + r00 - r11 - r22)
                w = (r21 - r12) / s
                x = 0.25 * s
                y = (r01 + r10) / s
                z = (r02 + r20) / s
            elif r11 > r22:
                s = 2.0 * math.sqrt(1.0 + r11 - r00 - r22)
                w = (r02 - r20) / s
                x = (r01 + r10) / s
                y = 0.25 * s
                z = (r12 + r21) / s
            else:
                s = 2.0 * math.sqrt(1.0 + r22 - r00 - r11)
                w = (r10 - r01) / s
                x = (r02 + r20) / s
                y = (r12 + r21) / s
                z = 0.25 * s
        return (w, x, y, z)

    def _matrix_to_euler_deg(self, R):
        r00, r01, r02 = R[0]
        r10, r11, r12 = R[1]
        r20, r21, r22 = R[2]

        roll = math.atan2(r21, r22)
        pitch = math.asin(max(-1.0, min(1.0, -r20)))
        yaw = math.atan2(r10, r00)

        return (math.degrees(roll), math.degrees(pitch), math.degrees(yaw))

    def get_all_devices(self):
        poses = self.vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )

        devices = []
        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if not self.vr.isTrackedDeviceConnected(i):
                continue

            cls = self.vr.getTrackedDeviceClass(i)
            class_name = self.device_class_names.get(cls, str(cls))
            try:
                serial = self.vr.getStringTrackedDeviceProperty(i, openvr.Prop_SerialNumber_String)
            except Exception:
                serial = "UNKNOWN"

            pose = poses[i]
            pose_valid = bool(pose.bPoseIsValid)
            pos = None
            quat = None
            euler = None
            if pose_valid:
                pos = self._get_pose_position(pose)
                R = self._rotation_matrix(pose)
                quat = self._matrix_to_quaternion(R)
                euler = self._matrix_to_euler_deg(R)

            devices.append({
                "index": i,
                "class": class_name,
                "serial": serial,
                "pose_valid": pose_valid,
                "position": pos,
                "quaternion": quat,
                "euler_deg": euler
            })

        return devices

    def print_all_readings(self):
        devices = self.get_all_devices()
        for d in devices:
            idx = d["index"]
            cls = d["class"]
            ser = d["serial"]
            valid = d["pose_valid"]
            if valid:
                x, y, z = d["position"]
                w, qx, qy, qz = d["quaternion"]
                roll, pitch, yaw = d["euler_deg"]
                print(f"Device {idx} | {cls} | {ser} | valid: {valid} | pos: x={x:.3f},y={y:.3f},z={z:.3f} | quat: w={w:.4f},x={qx:.4f},y={qy:.4f},z={qz:.4f} | euler_deg: roll={roll:.2f},pitch={pitch:.2f},yaw={yaw:.2f}")
            else:
                print(f"Device {idx} | {cls} | {ser} | valid: {valid}")


if __name__ == "__main__":
    vt = ViveTrackers()

    try:
        while True:
            devices = vt.get_all_devices()
            trackers = [d for d in devices if d["class"] == "Tracker"]

            if trackers:
                for n, d in enumerate(trackers, start=1):
                    i = d["index"]
                    if d["pose_valid"]:
                        x, y, z = d["position"]
                        roll, pitch, yaw = d["euler_deg"]
                        print(f"Tracker {n} (device {i}): x={x:.3f}, y={y:.3f}, z={z:.3f}")
                        print(f"Tracker {n} (device {i}) Euler (deg): roll={roll:.2f}, pitch={pitch:.2f}, yaw={yaw:.2f}")
                    else:
                        print(f"Tracker {n} (device {i}): Pose invalid")
            else:
                print("No trackers detected")

            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nShutting down...")

    finally:
        vt.shutdown()
