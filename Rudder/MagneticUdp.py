import smbus
import time
import socket
import json


class MagneticEncoder:
    def __init__(self, bus_num=1, addr=0x36, udp_host="<broadcast>", udp_port=9002):
        self.bus = smbus.SMBus(bus_num)
        self.addr = addr
        self.udp_host = udp_host
        self.udp_port = udp_port
        
        # Create UDP socket with broadcast enabled
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
    
    def read_angle(self):
        """Read the current angle from the AS5600 sensor."""
        data = self.bus.read_i2c_block_data(self.addr, 0x0C, 2)
        raw = (data[0] << 8) | data[1]
        return raw * 360.0 / 4096.0
    
    def read_status(self):
        """Read the magnet detection status."""
        status = self.bus.read_byte_data(self.addr, 0x0B)
        md = (status >> 5) & 1  # Magnet detected
        ml = (status >> 4) & 1  # Magnet too weak
        mh = (status >> 3) & 1  # Magnet too strong
        return md, ml, mh
    
    def get_status_string(self, md, ml, mh):
        """Convert status flags to human-readable string."""
        if ml:
            return "WEAK"
        elif mh:
            return "STRONG"
        elif not md:
            return "NO MAG"
        else:
            return "OK"
    
    def send_udp(self, angle_deg):
        """Send angle data via UDP to receiver."""
        message = {
            "type": "input",
            "angle_deg": angle_deg
        }
        data = json.dumps(message).encode()
        self.sock.sendto(data, (self.udp_host, self.udp_port))
    
    def run(self, interval=0.01):
        """Main loop: read sensor and send data via UDP."""
        print(f"Starting Magnetic Encoder (sending to {self.udp_host}:{self.udp_port})")
        try:
            while True:
                angle = self.read_angle()
                if angle > 180.0:
                    angle -= 360.0
                md, ml, mh = self.read_status()
                signal = self.get_status_string(md, ml, mh)
                
                # Send via UDP
                self.send_udp(angle)
                
                # Print status
                print(f"{angle:7.2f}° {signal}")
                
                time.sleep(interval)
        except KeyboardInterrupt:
            print("\nStopping...")
        finally:
            self.sock.close()


if __name__ == "__main__":
    encoder = MagneticEncoder()
    encoder.run()
