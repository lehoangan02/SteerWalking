#!/usr/bin/env python3
"""
Diagnostic script for Raspberry Pi - to be run on the Pi
Checks if the magnetic encoder and network are working
"""

import socket
import json
import sys

def check_smbus():
    """Check if smbus module is available"""
    try:
        import smbus
        print("✓ smbus module available")
        return True
    except ImportError:
        print("✗ smbus module NOT available - install with: pip install smbus-cffi")
        return False

def check_i2c_device():
    """Check if I2C device responds at address 0x36"""
    try:
        import smbus
        bus = smbus.SMBus(1)
        try:
            data = bus.read_byte_data(0x36, 0x0B)
            print(f"✓ AS5600 detected at address 0x36")
            return True
        except Exception as e:
            print(f"✗ No I2C device at 0x36: {e}")
            return False
    except:
        print("✗ Cannot check I2C (smbus not available)")
        return False

def check_broadcast_socket():
    """Check if we can create a broadcast socket"""
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        msg = {"type": "input", "angle_deg": 0.0}
        sock.sendto(json.dumps(msg).encode(), ("255.255.255.255", 9002))
        print(f"✓ Broadcast socket working - sent test packet")
        sock.close()
        return True
    except Exception as e:
        print(f"✗ Broadcast socket error: {e}")
        return False

def check_network():
    """Get network info"""
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.connect(("8.8.8.8", 80))
        ip = sock.getsockname()[0]
        sock.close()
        print(f"✓ Network interface IP: {ip}")
        return True
    except Exception as e:
        print(f"✗ Network error: {e}")
        return False

if __name__ == "__main__":
    print("=" * 60)
    print("MAGNETIC ENCODER DIAGNOSTICS")
    print("=" * 60)
    print(f"Python: {sys.version}")
    print(f"Platform: {sys.platform}\n")
    
    check_network()
    check_smbus()
    check_i2c_device()
    check_broadcast_socket()
    
    print("\n" + "=" * 60)
    print("NEXT STEPS:")
    print("1. Run MagneticUdp.py on the Pi")
    print("2. Run MagneticEncoderReceiver.py on Windows")
    print("3. Check that both are on same network (ping test)")
    print("=" * 60)
