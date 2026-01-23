import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation

def to_plot(p):
    return p[0], p[2], p[1]

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

theta = np.linspace(0, 2*np.pi, 100)
unit_circle = np.stack([
    np.cos(theta),
    np.sin(theta),
    np.zeros_like(theta)
], axis=1)

def update(frame):
    global circle_angle

    R = rot_y(rotation_y)

    O1 = R @ base_O1
    O2 = R @ base_O2

    circle_angle += 0.05

    local_pos = np.array([
        np.cos(circle_angle),
        np.sin(circle_angle),
        0.0
    ])

    A1 = O1 + R @ local_pos
    A2 = O2 + R @ local_pos

    circle1 = O1 + (unit_circle @ R.T)
    circle2 = O2 + (unit_circle @ R.T)

    x, y, z = to_plot(np.array([0.0, 0.0, 0.0]))
    O_dot.set_data([x], [y])
    O_dot.set_3d_properties([z])

    for dot, p in [
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

    return (
        O_dot,
        O1_dot, O2_dot, A1_dot, A2_dot,
        link_line, circle1_line, circle2_line
    )

def on_key(event):
    global rotation_y
    if event.key == 'left':
        rotation_y += 0.1
    if event.key == 'right':
        rotation_y -= 0.1

fig.canvas.mpl_connect('key_press_event', on_key)

ani = FuncAnimation(fig, update, interval=30)
plt.show()
