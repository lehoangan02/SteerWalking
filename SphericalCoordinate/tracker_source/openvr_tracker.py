import openvr
from tracker_source.abc_tracker import TrackerSource
from Rudder.RudderManager import RudderManager

class OpenVRTrackerSource(TrackerSource):
    def __init__(self):
        openvr.init(openvr.VRApplication_Other)
        self.vr = openvr.VRSystem()
        self.rudder_manager = RudderManager()

    def shutdown(self):
        openvr.shutdown()

    def get_tracker_position(self):
        poses = self.vr.getDeviceToAbsoluteTrackingPose(
            openvr.TrackingUniverseStanding,
            0,
            openvr.k_unMaxTrackedDeviceCount
        )
        i = 1
        if (
            self.vr.isTrackedDeviceConnected(i)
            and self.vr.getTrackedDeviceClass(i) == openvr.TrackedDeviceClass_GenericTracker
            and poses[i].bPoseIsValid
        ):
            m = poses[i].mDeviceToAbsoluteTracking
            return (m[0][3], m[1][3], m[2][3])
        else:
            if not self.vr.isTrackedDeviceConnected(i):
                print("[OpenVRTrackerSource] Error: Tracker not found")
            elif self.vr.getTrackedDeviceClass(i) != openvr.TrackedDeviceClass_GenericTracker:
                print("[OpenVRTrackerSource] Error: Device at index ", i, " is not a tracker")
            elif not poses[i].bPoseIsValid:
                print("[OpenVRTrackerSource] Error: Tracker pose is invalid")


        return None
    
    def get_tracker_rudder_degree(self):
        return self.rudder_manager.get_rudder_degree("MagneticEncoder")