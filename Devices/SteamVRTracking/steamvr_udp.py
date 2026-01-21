import openvr
import socket
import json
import time
import signal
import math

UDP_IP = "255.255.255.255"
UDP_PORT = 5068

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)


def get_device_serial(vr, index):
    try:
        return vr.getStringTrackedDeviceProperty(index, openvr.Prop_SerialNumber_String)
    except Exception:
        return "UNKNOWN"


def pose_to_pos_rot(pose):
    m = pose.mDeviceToAbsoluteTracking
    # m is 3 rows of 4 values: [[r00,r01,r02,r03], [r10,r11,r12,r13], [r20,r21,r22,r23]]
    x = m[0][3]
    y = m[1][3]
    z = m[2][3]

    # rotation matrix
    r00 = m[0][0]; r01 = m[0][1]; r02 = m[0][2]
    r10 = m[1][0]; r11 = m[1][1]; r12 = m[1][2]
    r20 = m[2][0]; r21 = m[2][1]; r22 = m[2][2]

    # convert rotation matrix to quaternion (x,y,z,w)
    trace = r00 + r11 + r22
    if trace > 0.0:
        s = math.sqrt(trace + 1.0) * 2.0
        qw = 0.25 * s
        qx = (r21 - r12) / s
        qy = (r02 - r20) / s
        qz = (r10 - r01) / s
    else:
        if (r00 > r11) and (r00 > r22):
            s = math.sqrt(1.0 + r00 - r11 - r22) * 2.0
            qw = (r21 - r12) / s
            qx = 0.25 * s
            qy = (r01 + r10) / s
            qz = (r02 + r20) / s
        elif r11 > r22:
            s = math.sqrt(1.0 + r11 - r00 - r22) * 2.0
            qw = (r02 - r20) / s
            qx = (r01 + r10) / s
            qy = 0.25 * s
            qz = (r12 + r21) / s
        else:
            s = math.sqrt(1.0 + r22 - r00 - r11) * 2.0
            qw = (r10 - r01) / s
            qx = (r02 + r20) / s
            qy = (r12 + r21) / s
            qz = 0.25 * s

    return (x, y, z), (qx, qy, qz, qw)


running = True


def handle_sigint(sig, frame):
    global running
    running = False


signal.signal(signal.SIGINT, handle_sigint)


def main():
    openvr.init(openvr.VRApplication_Other)
    vr = openvr.VRSystem()

    try:
        while running:
            poses = vr.getDeviceToAbsoluteTrackingPose(
                openvr.TrackingUniverseStanding,
                0,
                openvr.k_unMaxTrackedDeviceCount,
            )

            data_list = []

            for i in range(openvr.k_unMaxTrackedDeviceCount):
                if not vr.isTrackedDeviceConnected(i):
                    continue

                device_class = vr.getTrackedDeviceClass(i)
                serial = get_device_serial(vr, i)
                pose_valid = poses[i].bPoseIsValid

                if device_class == openvr.TrackedDeviceClass_GenericTracker:
                    item = {
                        "index": i,
                        "serial": serial,
                        "valid": bool(pose_valid),
                    }

                    if pose_valid:
                        pos, quat = pose_to_pos_rot(poses[i])
                        item["position"] = [pos[0], pos[1], pos[2]]
                        item["rotation"] = [quat[0], quat[1], quat[2], quat[3]]

                    data_list.append(item)

            if data_list:
                payload = {"type": "trackers", "time": time.time(), "trackers": data_list}
                sock.sendto(json.dumps(payload).encode(), (UDP_IP, UDP_PORT))

            time.sleep(0.05)
    finally:
        openvr.shutdown()


if __name__ == "__main__":
    main()
