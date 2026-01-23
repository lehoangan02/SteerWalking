import numpy as np
import matplotlib.pyplot as plt
from matplotlib.animation import FuncAnimation

fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

ax.set_xlim(-3, 3)
ax.set_ylim(-3, 3)
ax.set_zlim(-3, 3)

ax.set_xlabel('X')
ax.set_ylabel('Y')
ax.set_zlabel('Z')

origin = np.array([0.0, 0.0, 0.0])

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

O1_dot, = ax.plot([], [], [], 'ro', markersize=6)
O2_dot, = ax.plot([], [], [], 'bo', markersize=6)
A1_dot, = ax.plot([], [], [], 'r*', markersize=10)
A2_dot, = ax.plot([], [], [], 'b*', markersize=10)
link_line, = ax.plot([], [], [], 'k-')

def update(frame):
    global circle_angle

    R = rot_y(rotation_y)

    O1 = R @ base_O1
    O2 = R @ base_O2

    circle_angle += 0.05

    local_circle = np.array([
        np.cos(circle_angle),
        np.sin(circle_angle),
        0.0
    ])

    A1 = O1 + R @ local_circle
    A2 = O2 + R @ local_circle

    O1_dot.set_data([O1[0]], [O1[1]])
    O1_dot.set_3d_properties([O1[2]])

    O2_dot.set_data([O2[0]], [O2[1]])
    O2_dot.set_3d_properties([O2[2]])

    A1_dot.set_data([A1[0]], [A1[1]])
    A1_dot.set_3d_properties([A1[2]])

    A2_dot.set_data([A2[0]], [A2[1]])
    A2_dot.set_3d_properties([A2[2]])

    link_line.set_data([O1[0], O2[0]], [O1[1], O2[1]])
    link_line.set_3d_properties([O1[2], O2[2]])

    return O1_dot, O2_dot, A1_dot, A2_dot, link_line

def on_key(event):
    global rotation_y
    if event.key == 'left':
        rotation_y += 0.1
    if event.key == 'right':
        rotation_y -= 0.1

fig.canvas.mpl_connect('key_press_event', on_key)

ani = FuncAnimation(fig, update, interval=30)

plt.show()
