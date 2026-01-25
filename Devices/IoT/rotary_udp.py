from gpiozero import RotaryEncoder, Button
from signal import pause
import socket
import json

UDP_IP = "255.255.255.255"
UDP_PORT = 6001

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

rotor = RotaryEncoder(a=17, b=18, max_steps=0)
button = Button(22)

button_state = 0

def send_state():
    data = {
        "type": "input",
        "rotate": rotor.steps,
        "button": button_state
    }
    sock.sendto(json.dumps(data).encode(), (UDP_IP, UDP_PORT))
    print(data)

def rotate():
    send_state()

def press():
    global button_state
    button_state = 1
    send_state()
    button_state = 0
    rotor.steps = 0

rotor.when_rotated = rotate
button.when_pressed = press

pause()
