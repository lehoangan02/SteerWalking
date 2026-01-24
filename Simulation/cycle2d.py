import socket
import json
import time
import math
import sys
import pygame
import os

os.environ["SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS"] = "0"

# ================= CLI =================
args = sys.argv[1:]
ui_enabled = True
if "--no-ui" in args:
    ui_enabled = False
    args.remove("--no-ui")
elif "--ui" in args:
    ui_enabled = True
    args.remove("--ui")

# ================= UDP CONFIG =================
UDP_IP = args[0] if len(args) > 0 else "127.0.0.1"
UDP_PORT = int(args[1]) if len(args) > 1 else 9000
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# ================= FIXED SEND RATE =================
SEND_HZ = 60.0
SEND_DT = 1.0 / SEND_HZ

# ================= SIMULATION =================
angle_deg = 0.0
motion_speed = 0.5          # rotations per second
min_speed = 0.01
max_speed = 2.0

# ================= TIME =================
last_time = time.perf_counter()
send_accumulator = 0.0
running = True

# ================= PYGAME =================
if ui_enabled:
    pygame.init()
    screen_size = (600, 640)
    screen = pygame.display.set_mode(screen_size)
    pygame.display.set_caption("Stable 2D Circular Motion (Uniform UDP)")
    clock = pygame.time.Clock()
    font = pygame.font.SysFont(None, 26)
else:
    screen_size = (600, 640)

radius = 200
center = (screen_size[0] // 2, 300)
dragging_slider = False

# ================= SLIDER =================
def slider_rect():
    return pygame.Rect(100, 560, 400, 12)

def speed_from_mouse(mx):
    r = slider_rect()
    t = max(0.0, min(1.0, (mx - r.x) / r.width))
    return min_speed + t * (max_speed - min_speed)

def draw_slider(speed):
    r = slider_rect()
    pygame.draw.rect(screen, (60, 60, 70), r)
    t = (speed - min_speed) / (max_speed - min_speed)
    handle_x = r.x + int(t * r.width)
    pygame.draw.rect(screen, (0, 180, 255), (r.x, r.y, handle_x - r.x, r.height))
    pygame.draw.circle(screen, (230, 230, 230), (handle_x, r.centery), 8)

# ================= MAIN LOOP =================
while running:
    now = time.perf_counter()
    dt = now - last_time
    last_time = now
    send_accumulator += dt

    # ---------- EVENTS ----------
    if ui_enabled:
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False

            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_ESCAPE:
                    running = False
                elif event.key in (pygame.K_PLUS, pygame.K_EQUALS):
                    motion_speed = min(max_speed, motion_speed + 0.05)
                elif event.key == pygame.K_MINUS:
                    motion_speed = max(min_speed, motion_speed - 0.05)

            elif event.type == pygame.MOUSEBUTTONDOWN:
                if event.button == 1 and slider_rect().inflate(10, 10).collidepoint(event.pos):
                    dragging_slider = True
                    motion_speed = speed_from_mouse(event.pos[0])

            elif event.type == pygame.MOUSEBUTTONUP:
                if event.button == 1:
                    dragging_slider = False

            elif event.type == pygame.MOUSEMOTION:
                if dragging_slider:
                    motion_speed = speed_from_mouse(event.pos[0])

    # ---------- FIXED-TIMESTEP SIMULATION + SEND ----------
    while send_accumulator >= SEND_DT:
        angle_deg = (angle_deg + motion_speed * 360.0 * SEND_DT) % 360.0

        packet = json.dumps(
            {
                "angle_deg": angle_deg,
                "ts": now,  # monotonic timestamp
            }
        ).encode("utf-8")

        sock.sendto(packet, (UDP_IP, UDP_PORT))
        send_accumulator -= SEND_DT

    # ---------- DRAW ----------
    if ui_enabled:
        angle_rad = math.radians(angle_deg)
        x = center[0] + radius * math.cos(angle_rad)
        y = center[1] + radius * math.sin(angle_rad)

        screen.fill((18, 18, 22))
        pygame.draw.circle(screen, (90, 90, 100), center, radius, 2)
        pygame.draw.circle(screen, (0, 180, 255), (int(x), int(y)), 8)

        draw_slider(motion_speed)

        screen.blit(
            font.render(f"Angle: {angle_deg:.2f}°", True, (230, 230, 230)),
            (20, 20),
        )
        screen.blit(
            font.render(f"Speed: {motion_speed:.2f} rps", True, (230, 230, 230)),
            (20, 50),
        )

        pygame.display.flip()
        clock.tick(120)
    else:
        time.sleep(0.001)

if ui_enabled:
    pygame.quit()
