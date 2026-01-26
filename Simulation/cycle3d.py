import argparse
import time
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation
from mpl_toolkits.mplot3d import proj3d
import socket
import json

def to_plot(p):
    return p[0], p[2], p[1]

def label_at_point(ax, text_obj, p, dx=6, dy=6):
    x, y, z = to_plot(p)
    x2, y2, _ = proj3d.proj_transform(x, y, z, ax.get_proj())
    inv = ax.transData.inverted()
    x_disp, y_disp = ax.transData.transform((x2, y2))
    x_disp += dx
    y_disp += dy
    x_final, y_final = inv.transform((x_disp, y_disp))
    text_obj.set_position((x_final, y_final))

class Cycle3DSimulator:
    def __init__(self, udp_ip: str = "127.0.0.1", udp_port: int = 9000):
        self.UDP_IP = udp_ip
        self.UDP_PORT = udp_port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        self.fig = plt.figure()
        self.ax = self.fig.add_subplot(111, projection='3d')

        self.ax.set_xlim(-3, 3)
        self.ax.set_ylim(-3, 3)
        self.ax.set_zlim(-3, 3)
        self.ax.set_box_aspect([1, 1, 1])
        self.ax.grid(True)

        self.ax.set_xlabel('X')
        self.ax.set_ylabel('Z')
        self.ax.set_zlabel('Y (vertical)')

        self.base_O1 = np.array([0.0, 0.0, 1.0])
        self.base_O2 = np.array([0.0, 0.0, -1.0])

        self.rotation_y = 0.0
        self.circle_angle = 0.0
        self.last_time = time.time()

        self.cycle_speed = 0.1  # radians per frame step (can go negative for reverse)
        self.cycle_speed_step = 0.02  # additive step for up/down keys
        self.frame_interval_s = 0.03  # nominal frame interval for stable deg/s reporting

        self.STEP = 0.02
        self.RADIUS = 1.0

        self.theta = np.linspace(0, 2*np.pi, 120)
        self.unit_circle = np.stack([
            np.cos(self.theta),
            np.sin(self.theta),
            np.zeros_like(self.theta)
        ], axis=1)

        self.O_dot,  = self.ax.plot([], [], [], 'ko', markersize=5)
        self.O1_dot, = self.ax.plot([], [], [], 'ro', markersize=6)
        self.O2_dot, = self.ax.plot([], [], [], 'bo', markersize=6)
        self.A1_dot, = self.ax.plot([], [], [], 'r*', markersize=10)
        self.A2_dot, = self.ax.plot([], [], [], 'b*', markersize=10)
        self.link_line, = self.ax.plot([], [], [], 'k-')
        self.circle1_line, = self.ax.plot([], [], [], 'r--', linewidth=1)
        self.circle2_line, = self.ax.plot([], [], [], 'b--', linewidth=1)

        self.A1_label = self.ax.text2D(0, 0, "A1", color="red", fontsize=9, weight="bold")
        self.A2_label = self.ax.text2D(0, 0, "A2", color="blue", fontsize=9, weight="bold")

        self.coord_text = self.ax.text2D(
            0.02, 0.98, "",
            transform=self.ax.transAxes,
            va="top",
            ha="left",
            fontsize=9,
            family="monospace"
        )

        self.fig.canvas.mpl_connect('key_press_event', self.on_key)
        self.ani = FuncAnimation(self.fig, self.update, interval=30)

    def rot_y(self, theta):
        c = np.cos(theta)
        s = np.sin(theta)
        return np.array([
            [ c, 0, s],
            [ 0, 1, 0],
            [-s, 0, c]
        ])

    def send_udp_angle(self, angle_deg, angular_velocity_deg_s, rudder_deg):
        payload = {
            "angle_deg": float(angle_deg),
            "angular_velocity": float(angular_velocity_deg_s),
            "rudder_deg": float(rudder_deg),
            "ts": time.time(),
        }
        self.sock.sendto(json.dumps(payload).encode("utf-8"), (self.UDP_IP, self.UDP_PORT))

    def update(self, frame):
        now = time.time()
        dt = max(now - self.last_time, 1e-6)

        R = self.rot_y(self.rotation_y)

        O = np.array([0.0, 0.0, 0.0])
        O1 = R @ self.base_O1
        O2 = R @ self.base_O2

        self.circle_angle += self.cycle_speed

        phase1 = self.circle_angle % (2*np.pi)
        phase2 = (self.circle_angle + np.pi) % (2*np.pi)

        a1_local = self.RADIUS * np.array([
            np.cos(self.circle_angle),
            np.sin(self.circle_angle),
            0.0
        ])

        a2_local = self.RADIUS * np.array([
            np.cos(self.circle_angle + np.pi),
            np.sin(self.circle_angle + np.pi),
            0.0
        ])

        A1 = O1 + R @ a1_local
        A2 = O2 + R @ a2_local

        # Compute rudder angle and cycle angular velocity in degrees
        rudder_deg = np.degrees(self.rotation_y)
        cycle_speed_deg_step = np.degrees(self.cycle_speed)
        angular_velocity_deg_s = cycle_speed_deg_step / self.frame_interval_s
        self.last_time = now

        # Send combined payload to localhost
        self.send_udp_angle(
            angle_deg=rudder_deg,
            angular_velocity_deg_s=angular_velocity_deg_s,
            rudder_deg=rudder_deg,
        )

        circle1 = O1 + (self.unit_circle @ R.T) * self.RADIUS
        circle2 = O2 + (self.unit_circle @ R.T) * self.RADIUS

        for dot, p in [
            (self.O_dot,  O),
            (self.O1_dot, O1),
            (self.O2_dot, O2),
            (self.A1_dot, A1),
            (self.A2_dot, A2),
        ]:
            x, y, z = to_plot(p)
            dot.set_data([x], [y])
            dot.set_3d_properties([z])

        x1, y1, z1 = to_plot(O1)
        x2, y2, z2 = to_plot(O2)
        self.link_line.set_data([x1, x2], [y1, y2])
        self.link_line.set_3d_properties([z1, z2])

        cx, cy, cz = zip(*[to_plot(p) for p in circle1])
        self.circle1_line.set_data(cx, cy)
        self.circle1_line.set_3d_properties(cz)

        cx, cy, cz = zip(*[to_plot(p) for p in circle2])
        self.circle2_line.set_data(cx, cy)
        self.circle2_line.set_3d_properties(cz)

        label_at_point(self.ax, self.A1_label, A1)
        label_at_point(self.ax, self.A2_label, A2)

        self.coord_text.set_text(
            f"O  = ({O[0]: .2f}, {O[1]: .2f}, {O[2]: .2f})\n"
            f"O1 = ({O1[0]: .2f}, {O1[1]: .2f}, {O1[2]: .2f})\n"
            f"O2 = ({O2[0]: .2f}, {O2[1]: .2f}, {O2[2]: .2f})\n"
            f"A1 = ({A1[0]: .2f}, {A1[1]: .2f}, {A1[2]: .2f})\n"
            f"A2 = ({A2[0]: .2f}, {A2[1]: .2f}, {A2[2]: .2f})\n\n"
            f"Rudder = {rudder_deg: .1f}°\n"
            f"Phase A1 = {np.degrees(phase1): .1f}°\n"
            f"Phase A2 = {np.degrees(phase2): .1f}°\n"
            f"Cycle angular vel = {angular_velocity_deg_s: .2f} °/s"
        )

        return (
            self.O_dot, self.O1_dot, self.O2_dot, self.A1_dot, self.A2_dot,
            self.link_line, self.circle1_line, self.circle2_line,
            self.A1_label, self.A2_label, self.coord_text
        )

    def on_key(self, event):
        if event.key == 'left':
            self.rotation_y -= self.STEP
        if event.key == 'right':
            self.rotation_y += self.STEP
        if event.key == 'up':
            self.cycle_speed += self.cycle_speed_step
        if event.key == 'down':
            self.cycle_speed -= self.cycle_speed_step

    def run(self):
        plt.show()


def main():
    parser = argparse.ArgumentParser(description="Cycle 3D simulator (UDP angle/rudder sender)")
    parser.add_argument("--ip", default="127.0.0.1", help="Destination IP for UDP messages")
    parser.add_argument("--port", type=int, default=9000, help="Destination UDP port (default 9000)")
    args = parser.parse_args()

    app = Cycle3DSimulator(udp_ip=args.ip, udp_port=args.port)
    app.run()


if __name__ == "__main__":
    main()
