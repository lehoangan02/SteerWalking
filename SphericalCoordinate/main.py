from models.tracker_runner import TrackerRunner
from tracker_source.json_tracker import JsonTrackerSource
# from tracker_source.openvr_tracker import OpenVRTrackerSource
from tracker_source.vive_tracker import ViveTrackers
from models.broadcaster import TrackerUdpBroadcaster
from models.states import TrackerState

import time


def run_tracker(
    runner: TrackerRunner
):
    # try:
    runner.reset_timing()
    while True:
        is_return = runner.tick()
        if is_return:
            break
        time.sleep(0.001)

    # finally:
    #     udp.close()
    #     tracker.shutdown()

def main():
    print("Type 'v' or 'h' or 'sc' or 'q' or 'steam'")
    
    LAN_IP = "255.255.255.255"
    LOCALHOST_IP = "127.0.0.1"
    SEND_HZ = 10.0
    
    udp = TrackerUdpBroadcaster(ip=LOCALHOST_IP, port=9000)
    tracker1 =  ViveTrackers(JsonTrackerSource("sphere_positions_vertical.json", loop=True)) 
    tracker2 =  ViveTrackers(JsonTrackerSource("sphere_positions_horizontal.json", loop=True))
    # tracker = ViveTrackers(OpenVRTrackerSource())
    runner = TrackerRunner(udp, tracker1, send_hz=SEND_HZ)

    while True:
        cmd = input("> ").strip().lower()

        if cmd == "v":
            runner.state = TrackerState.COLLECT_VERTICAL
            runner.tracker = tracker1
            run_tracker(runner)
            print("Back to command mode.")
        elif cmd == "h":
            runner.state = TrackerState.COLLECT_HORIZONTAL
            runner.tracker = tracker2
            run_tracker(runner)
            print("Back to command mode.")
        elif cmd == "sc":
            runner.state = TrackerState.SEND_CIRCLE
            run_tracker(runner)
            print("Back to command mode.")
        elif cmd == "stream":
            runner.state = TrackerState.STREAMING
            run_tracker(runner)
            print("Back to command mode.")
        elif cmd == "quit":
            print("Exiting.")
            break
        else:
            print("Unknown command. Use: start | quit")


if __name__ == "__main__":
    main()
