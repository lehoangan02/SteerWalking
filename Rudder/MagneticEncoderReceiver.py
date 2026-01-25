import socket
import json
import threading
import sys
import time

try:
    import msvcrt
except Exception:
    msvcrt = None


class MagneticEncoderReceiver:
    def __init__(self, port=9002):
        self.state = {
            "angle_deg": 0.0
        }

        # protect access to `state`
        self._lock = threading.Lock()
        self._call_count = 0

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(("", port))

        self.thread = threading.Thread(target=self._listen, daemon=True)
        self.thread.start()

        # start keyboard listener (Windows only)
        if msvcrt is not None:
            self._kbd_thread = threading.Thread(target=self._keyboard_listener, daemon=True)
            self._kbd_thread.start()

    def _listen(self):
        while True:
            data, _ = self.sock.recvfrom(1024)
            msg = json.loads(data.decode())

            if msg.get("type") == "input":
                angle_deg = msg.get("angle_deg", 0.0)

                with self._lock:
                    self.state["angle_deg"] = angle_deg

    def _keyboard_listener(self):
        # simple Windows console keylistener: q -> left, e -> right
        while True:
            if msvcrt.kbhit():
                ch = msvcrt.getch()
                try:
                    key = ch.decode('utf-8')
                except Exception:
                    continue

                if key.lower() == 'q':
                    with self._lock:
                        self.state['angle_deg'] -= 1.0
                        print(f"Magnetic encoder angle -> {self.state['angle_deg']:.2f}°")
                elif key.lower() == 'e':
                    with self._lock:
                        self.state['angle_deg'] += 1.0
                        print(f"Magnetic encoder angle -> {self.state['angle_deg']:.2f}°")
            else:
                # avoid busy loop
                time.sleep(0.05)

    def get(self):
        with self._lock:
            self._call_count += 1
            if self._call_count % 10 == 0:
                print("Magnetic encoder state:", self.state)
            return self.state.copy()


if __name__ == "__main__":
    print("Starting Magnetic Encoder Receiver on port 9002...")
    receiver = MagneticEncoderReceiver(port=9002)
    
    try:
        while True:
            state = receiver.get()
            print(f"Angle: {state['angle_deg']:.2f}°")
            time.sleep(0.1)
    except KeyboardInterrupt:
        print("\nShutdown")
