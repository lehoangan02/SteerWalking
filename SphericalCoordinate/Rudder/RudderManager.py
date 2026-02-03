import json
from .ViveTracker import ViveTracker
from .RotaryEncoderReceiver import RotaryEncoderReceiver
from .MagneticEncoderReceiver import MagneticEncoderReceiver


class RudderManager:
    def __init__(self):
        self.vive_tracker = ViveTracker()
        self.rotary_encoder = RotaryEncoderReceiver()
        self.magnetic_encoder = MagneticEncoderReceiver()

    def get_rudder_degree(self, source):
        """
        Get rudder angle in degrees from the specified source.
        
        Args:
            source (str): "ViveTracker2", "RotaryEncoder", or "MagneticEncoder"
        
        Returns:
            float: Rudder angle in degrees
        """
        if source == "ViveTracker2":
            roll, pitch, yaw = self.vive_tracker.get_tracker_2_rotation()
            return pitch
        
        elif source == "RotaryEncoder":
            state = self.rotary_encoder.get()
            return state["rotate"]
        
        elif source == "MagneticEncoder":
            state = self.magnetic_encoder.get()
            return state["angle_deg"]
        
        else:
            raise ValueError(f"Unknown source: {source}. Options: ViveTracker2, RotaryEncoder, MagneticEncoder")

    def shutdown(self):
        """Clean up resources."""
        self.vive_tracker.shutdown()