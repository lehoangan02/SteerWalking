#!/usr/bin/env python3
"""
Speed Control UI
Sends walk speed data via UDP to port 9003
Range: -2.0 to 2.0
"""

import tkinter as tk
from tkinter import ttk
import socket
import json
import time


class SpeedControlUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Walk Speed Control")
        self.root.geometry("300x450")
        
        # UDP socket setup
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.target_ip = "127.0.0.1"
        self.target_port = 9003
        
        # Create UI elements
        self.create_widgets()
        
    def create_widgets(self):
        # Title label
        title_label = tk.Label(
            self.root,
            text="Walk Speed Control",
            font=("Arial", 16, "bold")
        )
        title_label.pack(pady=10)
        
        # Speed value display
        self.speed_label = tk.Label(
            self.root,
            text="Speed: 0.00",
            font=("Arial", 14)
        )
        self.speed_label.pack(pady=5)
        
        # Slider
        self.slider = tk.Scale(
            self.root,
            from_=2.0,
            to=-2.0,
            resolution=0.1,
            orient=tk.VERTICAL,
            length=300,
            command=self.on_slider_change
        )
        self.slider.set(0.0)
        self.slider.pack(pady=10)
        
        # Min/Max labels
        label_frame = tk.Frame(self.root)
        label_frame.pack()
        
        tk.Label(label_frame, text="Top: 2.0").pack(side=tk.LEFT, padx=10)
        tk.Label(label_frame, text="Bottom: -2.0").pack(side=tk.LEFT, padx=10)
        
        # Status label
        self.status_label = tk.Label(
            self.root,
            text="Ready",
            fg="green",
            font=("Arial", 10)
        )
        self.status_label.pack(pady=10)
        
    def on_slider_change(self, value):
        """Called when slider value changes"""
        speed = float(value)
        self.speed_label.config(text=f"Speed: {speed:.2f}")
        self.send_speed(speed)
        
    def send_speed(self, speed):
        """Send speed data via UDP"""
        try:
            payload = {
                "walk_speed": speed
            }
            message = json.dumps(payload).encode('utf-8')
            self.sock.sendto(message, (self.target_ip, self.target_port))
            self.status_label.config(
                text=f"Sent: {speed:.2f} to {self.target_ip}:{self.target_port}",
                fg="green"
            )
        except Exception as e:
            self.status_label.config(
                text=f"Error: {str(e)}",
                fg="red"
            )
    
    def on_closing(self):
        """Clean up when closing"""
        self.sock.close()
        self.root.destroy()


def main():
    root = tk.Tk()
    app = SpeedControlUI(root)
    root.protocol("WM_DELETE_WINDOW", app.on_closing)
    root.mainloop()


if __name__ == "__main__":
    main()
