import time

from Broadcaster.udp_broadcast import TrackerUdpBroadcaster
from TrackerSource.json_tracker import JsonTrackerSource
from TrackerSource.vive_tracker import ViveTrackers

LAN_IP = "255.255.255.255"
LOCALHOST_IP = "127.0.0.1"
SEND_HZ = 10.0
SEND_DT = 1.0 / SEND_HZ
last_time = time.perf_counter()
accumulator = 0.0
circle_sent = False

# Real device
# vt = ViveTrackers(OpenVRTrackerSource())

# OR simulation
vt = ViveTrackers(JsonTrackerSource("sphere_positions.json", loop=True))
udp = TrackerUdpBroadcaster(ip=LOCALHOST_IP, port=9000)

try:
    while True:
        now = time.perf_counter()
        dt = now - last_time
        last_time = now
        accumulator += dt

        while accumulator >= SEND_DT:
            pos = vt.get_tracker_position()
            if not udp.get_circle():
                udp._update_circle(pos)
            elif not circle_sent:
                udp.send_circle()
                udp.send_yline()
                circle_sent = True
            else:
            #udp.send_xyz_position(pos)
                ang, vel = udp.send_degree_position(pos)
                print(ang)
                print(vel)
            accumulator -= SEND_DT

        time.sleep(0.001)
finally:
    udp.close()
    vt.shutdown()


# nc -u -l 9000