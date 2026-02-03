import socket
import json
import threading
import sys
import time
import math

try:
    import msvcrt
except Exception:
    msvcrt = None

try:
    import pygame
except ImportError:
    pygame = None


class MagneticEncoderReceiver:
    def __init__(self, port=9002):
        self.state = {
            "angle_deg": 0.0
        }

        # protect access to `state`
        self._lock = threading.Lock()
        
        # statistics for debugging
        self.packets_received = 0
        self.packets_dropped = 0
        self.last_error = None

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)  # Allow reuse
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_RCVBUF, 1024 * 1024)  # 1MB buffer
        
        # For broadcast reception on Windows
        if sys.platform == "win32":
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        
        self.sock.bind(("", port))  # Bind to all interfaces
        self.sock.settimeout(1.0)  # 1 second timeout
        
        # print(f"[MagneticEncoderReceiver] Listening on 0.0.0.0:{port}")
        # print(f"[MagneticEncoderReceiver] Platform: {sys.platform}")

        self.thread = threading.Thread(target=self._listen, daemon=True)
        self.thread.start()

        # start keyboard listener (Windows only)
        if msvcrt is not None:
            self._kbd_thread = threading.Thread(target=self._keyboard_listener, daemon=True)
            self._kbd_thread.start()

    def _listen(self):
        # print("[MagneticEncoderReceiver] Listen thread started")
        first_packet = True
        while True:
            try:
                # Use blocking receive with timeout
                data, addr = self.sock.recvfrom(1024)
                
                try:
                    msg = json.loads(data.decode('utf-8'))
                    if msg.get("type") == "input":
                        angle_deg = msg.get("angle_deg", 0.0)
                        with self._lock:
                            self.state["angle_deg"] = angle_deg
                            self.packets_received += 1
                        
                        if first_packet:
                            # print(f"[RX] ✓ First packet received from {addr[0]}:{addr[1]}")
                            first_packet = False
                        
                        if self.packets_received % 10 == 0:  # Log every 10th packet
                            pass
                            # print(f"[RX] Angle: {angle_deg:.1f}° | Total packets: {self.packets_received}")
                    else:
                        pass
                        # print(f"[RX] Unknown message type: {msg.get('type')}")
                except json.JSONDecodeError as e:
                    pass
                    # print(f"[RX] JSON decode error: {e}, data: {data[:50]}")
                    with self._lock:
                        self.packets_dropped += 1
                except ValueError as e:
                    # print(f"[RX] Value error: {e}")
                    with self._lock:
                        self.packets_dropped += 1
                        
            except socket.timeout:
                # Timeout is normal, just continue listening
                pass
            except Exception as e:
                # print(f"[RX] Socket error: {type(e).__name__}: {e}")
                with self._lock:
                    self.last_error = str(e)
                time.sleep(0.1)

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
                elif key.lower() == 'e':
                    with self._lock:
                        self.state['angle_deg'] += 1.0
            else:
                time.sleep(0.05)

    def get(self):
        with self._lock:
            return self.state.copy()
    
    def get_stats(self):
        """Return reception statistics for debugging."""
        with self._lock:
            return {
                "packets_received": self.packets_received,
                "packets_dropped": self.packets_dropped,
                "last_error": self.last_error
            }


def draw_compass(screen, angle_deg, width=800, height=600):
    """Draw a compass with arrow pointing at the angle."""
    if pygame is None:
        return
    
    center_x, center_y = width // 2, height // 2
    radius = 150
    
    # Draw circle
    pygame.draw.circle(screen, (200, 200, 200), (center_x, center_y), radius, 2)
    
    # Draw cardinal directions
    font = pygame.font.Font(None, 36)
    directions = [("N", 0), ("E", 90), ("S", 180), ("W", 270)]
    for label, deg in directions:
        rad = math.radians(deg - 90)
        x = center_x + (radius + 30) * math.cos(rad)
        y = center_y + (radius + 30) * math.sin(rad)
        text = font.render(label, True, (0, 0, 0))
        screen.blit(text, (x - 10, y - 15))
    
    # Draw arrow pointing at angle
    arrow_angle_rad = math.radians(angle_deg - 90)
    arrow_length = radius - 20
    end_x = center_x + arrow_length * math.cos(arrow_angle_rad)
    end_y = center_y + arrow_length * math.sin(arrow_angle_rad)
    
    pygame.draw.line(screen, (255, 0, 0), (center_x, center_y), (end_x, end_y), 3)
    
    # Draw arrowhead
    arrow_size = 15
    angle1_rad = arrow_angle_rad + math.radians(150)
    angle2_rad = arrow_angle_rad - math.radians(150)
    
    p1 = (end_x + arrow_size * math.cos(angle1_rad), end_y + arrow_size * math.sin(angle1_rad))
    p2 = (end_x + arrow_size * math.cos(angle2_rad), end_y + arrow_size * math.sin(angle2_rad))
    
    pygame.draw.polygon(screen, (255, 0, 0), [(end_x, end_y), p1, p2])
    
    # Draw angle text
    font_large = pygame.font.Font(None, 48)
    angle_text = font_large.render(f"{angle_deg:.1f}°", True, (0, 0, 0))
    screen.blit(angle_text, (width // 2 - 50, height - 80))


if __name__ == "__main__":
    if pygame is None:
        # print("pygame not installed. Install with: pip install pygame")
        sys.exit(1)
    
    pygame.init()
    WIDTH, HEIGHT = 800, 600
    screen = pygame.display.set_mode((WIDTH, HEIGHT))
    pygame.display.set_caption("Magnetic Encoder Compass")
    clock = pygame.time.Clock()
    
    # print("Starting Magnetic Encoder Receiver on port 9002...")
    receiver = MagneticEncoderReceiver(port=9002)
    
    running = True
    while running:
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
        
        state = receiver.get()
        stats = receiver.get_stats()
        
        screen.fill((255, 255, 255))
        draw_compass(screen, state['angle_deg'], WIDTH, HEIGHT)
        
        # Draw stats
        font_small = pygame.font.Font(None, 24)
        stats_text = f"RX: {stats['packets_received']} | Dropped: {stats['packets_dropped']}"
        if stats['last_error']:
            stats_text += f" | Error: {stats['last_error'][:30]}"
        text_surface = font_small.render(stats_text, True, (0, 0, 0))
        screen.blit(text_surface, (10, 10))
        
        pygame.display.flip()
        clock.tick(60)
    
    pygame.quit()
    # print("Shutdown")
