from gpiozero import RotaryEncoder, Button
from signal import pause
import socket
import json

UDP_IP = "255.255.255.255"
UDP_PORT = 5005

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)

rotor = RotaryEncoder(a=17, b=18, max_steps=0)
button = Button(22)

value = 0

def send(data):
    sock.sendto(json.dumps(data).encode(), (UDP_IP, UDP_PORT))

def cw():
    global value
    value += 1
    send({"type": "rotate", "value": value})
    print(value)

def ccw():
    global value
    value -= 1
    send({"type": "rotate", "value": value})
    print(value)

def press():
    global value
    value = 0
    send({"type": "button"})
    print("Button Pressed!")

rotor.when_rotated_clockwise = cw
rotor.when_rotated_counter_clockwise = ccw
button.when_pressed = press

pause()
