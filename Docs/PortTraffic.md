# Port Traffic and Payload Formats

This document details the network port traffic and payload formats used in the system.

## 1. Angle and Rudder Data Port
Port: `9000` (UDP, Everyone)

```json
{
  "angle_deg": 45.0,
  "angular_velocity": 90.0,
  "rudder_deg": 10.0,
  "ts": 1710000000.123
}
```