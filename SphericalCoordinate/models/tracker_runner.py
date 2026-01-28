# tracker/runner.py
import time
from models.states import TrackerState
from models.broadcaster import TrackerUdpBroadcaster
from tracker_source import abc_tracker

class TrackerRunner:
    def __init__(self, 
        udp : TrackerUdpBroadcaster, 
        tracker : abc_tracker, 
        send_hz=20.0):
        
        self.udp = udp
        self.tracker = tracker
        self.send_dt = 1.0 / send_hz

        self.state = TrackerState.STREAMING
        self.last_time = time.perf_counter()
        self.accumulator = 0.0

    def tick(self):
        now = time.perf_counter()
        dt = now - self.last_time
        self.last_time = now
        self.accumulator += dt
        is_return = False

        while self.accumulator >= self.send_dt:
            pos = self.tracker.get_tracker_position()
            is_return = self._handle_sample(pos)
            self.accumulator -= self.send_dt
        
        return is_return

    def reset_timing(self):
        self.last_time = time.perf_counter()
        self.accumulator = 0.0

    def _handle_sample(self, pos):
        if self.state == TrackerState.COLLECT_VERTICAL:
            self.udp._update_vertical_circle(pos)
            if self.udp.centerV:
                print("[DONE] Computing vertical circle!")
                self.state = TrackerState.RETURN
        elif self.state == TrackerState.COLLECT_HORIZONTAL:
            self.udp._update_horizontal_circle(pos)
            if self.udp.centerH:
                print("[DONE] Computing horizontal circle!")
                self.state = TrackerState.RETURN
        elif self.state == TrackerState.SEND_CIRCLE:
            self.udp.send_circle(self.udp.centerV, self.udp.v_normV, self.udp.radiusV)
            self.udp.send_circle(self.udp.centerH, self.udp.v_normH, self.udp.radiusH)
            print("[DONE] Send circle!")
            self.state = TrackerState.RETURN
        elif self.state == TrackerState.SEND_REF_LINE:
            self.udp.send_ref_line()
        elif self.state == TrackerState.RETURN:
            return True

        # --- STREAMING DATA ---
        self.udp.send_xyz_position(pos)
        print(pos)
        return False
