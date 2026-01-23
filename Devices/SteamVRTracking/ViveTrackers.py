import openvr
import time

class ViveTrackers:
    def __init__(self):
        openvr.init(openvr.VRApplication_Other)
        self.vr = openvr.VRSystem()

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

            if not poses[i].bPoseIsValid:
                continue

            tracker_positions.append(self._get_pose_position(poses[i]))

            if len(tracker_positions) >= 2:
                break

        if len(tracker_positions) < 2:
            return None, None

        return tracker_positions[0], tracker_positions[1]


if __name__ == "__main__":
    vt = ViveTrackers()

    try:
        while True:
            t1, t2 = vt.get_tracker_positions()

            if t1 and t2:
                print(f"Tracker 1: x={t1[0]:.3f}, y={t1[1]:.3f}, z={t1[2]:.3f}")
                print(f"Tracker 2: x={t2[0]:.3f}, y={t2[1]:.3f}, z={t2[2]:.3f}")
            else:
                print("Waiting for two trackers...")

            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\nShutting down...")

    finally:
        vt.shutdown()
