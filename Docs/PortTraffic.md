# Port Traffic and Payload Formats

This document details the network port traffic and payload formats used in the system.

## 1. Angle Data Port
Port: `9000` (UDP)

```json
{
  "angle_deg": 45.0,
  "angular_velocity": 90.0,
  "ts": 1710000000.123
}
```

## 2. Pedal Tracker and Rudder Data Port
Port: `9001` (UDP)

```json
{
    "A1": {
        "x": 1.0,
        "y": 2.0,
        "z": 3.0
    },
    "A2": {
        "x": 4.0,
        "y": 5.0,
        "z": 6.0
    },
    "rudder_deg": 30.0,
}
```
