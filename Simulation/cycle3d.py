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

UDP_IP = "127.0.0.1"
UDP_PORT = 9001
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

ax.set_xlim(-3, 3)
ax.set_ylim(-3, 3)
ax.set_zlim(-3, 3)
ax.set_box_aspect([1, 1, 1])
ax.grid(True)

ax.set_xlabel('X')
ax.set_ylabel('Z')
ax.set_zlabel('Y (vertical)')

base_O1 = np.array([0.0, 0.0, 1.0])
base_O2 = np.array([0.0, 0.0, -1.0])

rotation_y = 0.0
circle_angle = 0.0
STEP = 0.02
RADIUS = 1.0

def rot_y(theta):
    c = np.cos(theta)
    s = np.sin(theta)
    return np.array([
        [ c, 0, s],
        [ 0, 1, 0],
        [-s, 0, c]
    ])

O_dot,  = ax.plot([], [], [], 'ko', markersize=5)
O1_dot, = ax.plot([], [], [], 'ro', markersize=6)
O2_dot, = ax.plot([], [], [], 'bo', markersize=6)
A1_dot, = ax.plot([], [], [], 'r*', markersize=10)
A2_dot, = ax.plot([], [], [], 'b*', markersize=10)
link_line, = ax.plot([], [], [], 'k-')
circle1_line, = ax.plot([], [], [], 'r--', linewidth=1)
circle2_line, = ax.plot([], [], [], 'b--', linewidth=1)

A1_label = ax.text2D(0, 0, "A1", color="red", fontsize=9, weight="bold")
A2_label = ax.text2D(0, 0, "A2", color="blue", fontsize=9, weight="bold")

coord_text = ax.text2D(
    0.02, 0.98, "",
    transform=ax.transAxes,
    va="top",
    ha="left",
    fontsize=9,
    family="monospace"
)

theta = np.linspace(0, 2*np.pi, 120)
unit_circle = np.stack([
    np.cos(theta),
    np.sin(theta),
    np.zeros_like(theta)
], axis=1)

def send_udp(A1, A2, rudder_deg):
    payload = {
        "A1": {"x": float(A1[0]), "y": float(A1[1]), "z": float(A1[2])},
        "A2": {"x": float(A2[0]), "y": float(A2[1]), "z": float(A2[2])},
        "rudder_deg": float(rudder_deg)
    }
    sock.sendto(json.dumps(payload).encode("utf-8"), (UDP_IP, UDP_PORT))

def update(frame):
    global circle_angle

    R = rot_y(rotation_y)

    O = np.array([0.0, 0.0, 0.0])
    O1 = R @ base_O1
    O2 = R @ base_O2

    circle_angle += 0.05

    phase1 = circle_angle % (2*np.pi)
    phase2 = (circle_angle + np.pi) % (2*np.pi)

    a1_local = RADIUS * np.array([
        np.cos(circle_angle),
        np.sin(circle_angle),
        0.0
    ])

    a2_local = RADIUS * np.array([
        np.cos(circle_angle + np.pi),
        np.sin(circle_angle + np.pi),
        0.0
    ])

    A1 = O1 + R @ a1_local
    A2 = O2 + R @ a2_local

    send_udp(A1, A2, np.degrees(rotation_y))

    circle1 = O1 + (unit_circle @ R.T) * RADIUS
    circle2 = O2 + (unit_circle @ R.T) * RADIUS

    for dot, p in [
        (O_dot,  O),
        (O1_dot, O1),
        (O2_dot, O2),
        (A1_dot, A1),
        (A2_dot, A2),
    ]:
        x, y, z = to_plot(p)
        dot.set_data([x], [y])
        dot.set_3d_properties([z])

    x1, y1, z1 = to_plot(O1)
    x2, y2, z2 = to_plot(O2)
    link_line.set_data([x1, x2], [y1, y2])
    link_line.set_3d_properties([z1, z2])

    cx, cy, cz = zip(*[to_plot(p) for p in circle1])
    circle1_line.set_data(cx, cy)
    circle1_line.set_3d_properties(cz)

    cx, cy, cz = zip(*[to_plot(p) for p in circle2])
    circle2_line.set_data(cx, cy)
    circle2_line.set_3d_properties(cz)

    label_at_point(ax, A1_label, A1)
    label_at_point(ax, A2_label, A2)

    coord_text.set_text(
        f"O  = ({O[0]: .2f}, {O[1]: .2f}, {O[2]: .2f})\n"
        f"O1 = ({O1[0]: .2f}, {O1[1]: .2f}, {O1[2]: .2f})\n"
        f"O2 = ({O2[0]: .2f}, {O2[1]: .2f}, {O2[2]: .2f})\n"
        f"A1 = ({A1[0]: .2f}, {A1[1]: .2f}, {A1[2]: .2f})\n"
        f"A2 = ({A2[0]: .2f}, {A2[1]: .2f}, {A2[2]: .2f})\n\n"
        f"Rudder = {rotation_y: .3f} rad ({np.degrees(rotation_y): .1f}°)\n"
        f"Phase A1 = {phase1: .3f} rad ({np.degrees(phase1): .1f}°)\n"
        f"Phase A2 = {phase2: .3f} rad ({np.degrees(phase2): .1f}°)"
    )

    return (
        O_dot, O1_dot, O2_dot, A1_dot, A2_dot,
        link_line, circle1_line, circle2_line,
        A1_label, A2_label, coord_text
    )

def on_key(event):
    global rotation_y
    if event.key == 'left':
        rotation_y -= STEP
    if event.key == 'right':
        rotation_y += STEP

fig.canvas.mpl_connect('key_press_event', on_key)

ani = FuncAnimation(fig, update, interval=30)
plt.show()
