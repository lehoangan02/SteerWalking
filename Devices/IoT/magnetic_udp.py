import smbus
import json
import socket
import time

bus = smbus.SMBus(1)
AS5600_ADDR = 0x36

UDP_PORT = 9002
UDP_IP = "255.255.255.255"

def read_angle():
    """Read raw angle from AS5600 magnetic encoder and convert to degrees."""
    high = bus.read_byte_data(AS5600_ADDR, 0x0E)
    low = bus.read_byte_data(AS5600_ADDR, 0x0F)
    raw = (high << 8) | low
    return raw * 360.0 / 4096.0

def send_angle_data(angle_deg):
    """Send angle data via UDP broadcast to all devices."""
    data = {
        "type": "input",
        "angle_deg": round(angle_deg, 2)
    }
    
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
    sock.sendto(json.dumps(data).encode(), (UDP_IP, UDP_PORT))
    sock.close()

if __name__ == "__main__":
    print(f"Broadcasting magnetic encoder data on UDP 255.255.255.255:{UDP_PORT}")
    
    try:
        while True:
            angle = read_angle()
            send_angle_data(angle)
            print(f"Sent: angle_deg={angle:.2f}°")
            time.sleep(0.1)
    except KeyboardInterrupt:
        print("\nShutdown")
