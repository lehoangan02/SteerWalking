import time
import argparse
import sys

from Broadcaster.udp_broadcast import TrackerUdpBroadcaster
# from TrackerSource.json_tracker import JsonTrackerSource
from TrackerSource.udp_tracker import UdpTrackerSource
from TrackerSource.vive_tracker import ViveTrackers

LAN_IP = "255.255.255.255"
LOCALHOST_IP = "127.0.0.1"
SEND_HZ = 10.0
SEND_DT = 1.0 / SEND_HZ
last_time = time.perf_counter()
accumulator = 0.0
center_sent = False

# Args: allow configuring receive and send endpoints
parser = argparse.ArgumentParser(description="Tracker broadcaster that fits a circle and streams to viewer")
parser.add_argument("--recv-ip", default="0.0.0.0", help="IP to bind for incoming UDP positions")
parser.add_argument("--recv-port", type=int, default=9002, help="Port to bind for incoming UDP positions")
parser.add_argument("--send-ip", default=LOCALHOST_IP, help="Destination IP for viewer")
parser.add_argument("--send-port", type=int, default=9003, help="Destination port for viewer")
args = parser.parse_args()
print(
    f"[main_vive] recv={args.recv_ip}:{args.recv_port} -> send={args.send_ip}:{args.send_port}"
)

# Real device
# vt = ViveTrackers(OpenVRTrackerSource())

# OR receive from simulator (cycle3d) over UDP
try:
    # Listen on a dedicated port (default 9002) to avoid conflicts with the viewer (9000).
    vt = ViveTrackers(UdpTrackerSource(ip=args.recv_ip, port=args.recv_port))
except OSError as e:
    print(f"[main_vive] Failed to bind UDP receiver on {args.recv_ip}:{args.recv_port}: {e}")
    print("Hint: choose a different --recv-port (e.g., 9003) or stop the process using that port.")
    sys.exit(1)

udp = TrackerUdpBroadcaster(ip=args.send_ip, port=args.send_port)

try:
    while True:
        now = time.perf_counter()
        dt = now - last_time
        last_time = now
        accumulator += dt

        while accumulator >= SEND_DT:
            pos = vt.get_tracker_position()
            if pos is None:
                # No new data yet; wait for UDP tracker to receive a sample.
                break

            if not udp.get_circle():
                udp._update_circle(pos)
            elif not center_sent:
                udp.send_circle()
                center_sent = True

            udp.send_xyz_position(pos)
            print(pos)
            accumulator -= SEND_DT

        time.sleep(0.001)
finally:
    udp.close()
    vt.shutdown()


# nc -u -l 9000