# Port Traffic and Payload Formats

This document details the network port traffic and payload formats used in the system.

## 1. Angle and Rudder Data Port
Port: `9000` (UDP, from Python backend to Unity)

```json
{
  "angle_deg": 45.0,
  "angular_velocity": 90.0,
  "rudder_deg": 10.0,
  "ts": 1710000000.123
}
```

## 2. Rotary Encoder Data Port
Port: `9001` (UDP, from Rotary Encoder (raspberry pi/esp32) to Python backend)

```json
{
  "type": "input",
  "rotate": 1,
  "button": 0
}
```

## 3. Magnetic Encoder Data Port
Port: `9002` (UDP, from Magnetic Encoder (raspberry pi/esp32) to Python backend)
```json
{
  "type": "input",
  "angle_deg": 123.45
}
```