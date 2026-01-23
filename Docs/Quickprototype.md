# UDP → Movement → Animation Workflow (Data-Focused)

This document describes **how data flows through the current system**, focusing on **data formats**, **transformations**, and **how animation speed is ultimately driven**.

---

## 1. High-Level Architecture


![Device process diagram](Devices.svg)

```
Simulated/Real Deivices
   ↓ (UDP / JSON)
UDP_SimulatedReceiver
   ↓ (TrackerPayload)
UDPAnimationController
   ↓ (velocity)
PlayerMovement
   ↓
Animator.speed
```

The system converts **simple angular motion data** into:

* World-space movement
* Character translation
* Animation playback speed

---

## 2. Incoming Data (Python → Unity)

### 2.1 UDP Transport

* **Protocol**: UDP
* **Port**: `9000`
* **Encoding**: UTF-8 JSON string
* **Threading**: Background thread (`ReceiveLoop`)

---

### 2.2 Raw JSON Format (PythonSimPayload)

```json
{
  "angle_deg": 45.0,
  "angular_velocity": 90.0,
  "ts": 1710000000.123
}
```

| Field              | Type     | Meaning                                |
| ------------------ | -------- | -------------------------------------- |
| `angle_deg`        | `float`  | Angular position on a circle (degrees) |
| `angular_velocity` | `float`  | Rotation speed (degrees / second)      |
| `ts`               | `double` | Timestamp from Python                  |

This format is intentionally **minimal** and **simulation-friendly**.

---

## 3. Conversion Layer (Simulation → Tracker System)

### 3.1 Converted Format: TrackerPayload

After receiving `PythonSimPayload`, Unity converts it into a **full tracker-style payload**.

```csharp
TrackerPayload
{
  string type;
  double time;
  float angular_velocity;
  TrackerData[] trackers;
}
```

---

### 3.2 TrackerData Structure

```csharp
TrackerData
{
  int index;
  string serial;
  bool valid;
  float[3] position;   // x, y, z
  float[4] rotation;   // quaternion (x, y, z, w)
}
```

This makes the simulation **compatible with real tracker pipelines**.

---

### 3.3 Position Calculation (Key Data Transformation)

**Input**: `angle_deg`

```text
rad = angle_deg × π / 180
x = cos(rad) × radius
z = sin(rad) × radius
y = fixed height
```

**Resulting Position Format**:

```json
"position": [x, height, z]
```

---

### 3.4 Rotation Format

Rotation is computed so the tracker faces outward:

```csharp
Quaternion.Euler(0, -angle_deg, 0)
```

Stored as:

```json
"rotation": [qx, qy, qz, qw]
```

---


## 5. Animation & Movement Controller

### 5.1 Data Input (UDPAnimationController)

```csharp
TrackerPayload payload = udpReceiver.GetLatestPayload();
float rawVelocity = payload.angular_velocity;
```

**Important**: The system **does NOT** use position deltas.
It relies entirely on **angular_velocity**.

---

### 5.2 Velocity Deadzone Filtering

```text
if |angular_velocity| < deadzone → 0
```

Purpose:

* Remove jitter
* Prevent micro animation flicker

---

### 5.3 Angular → Linear Speed Conversion

```text
angular_velocity (deg/s)
→ radians/s
→ meters/s
```

Formula:

```text
linear_speed = |angular_velocity| × π/180 × walkingRadius
```

This produces **real-world walking speed (m/s)**.

---


---

## 7. Movement Data Flow

### 7.1 PlayerMovement Input Format

```csharp
playerMovement.velocity = transform.forward * currentAnimValue;
```

* **Space**: Local space
* **Meaning**: Desired movement direction & magnitude

---

### 7.2 PlayerMovement Processing

Inside `PlayerMovement.Update()`:

1. Convert local velocity → world space
2. Try stair step-up
3. Project movement onto ground normal
4. Apply translation:

```csharp
transform.position += worldMove * speed * deltaTime;
```

---

## 8. Animation Speed Control

### 8.1 Final Animation Speed Formula

```text
animator.speed = currentAnimValue × animationSpeedScale
```

Constraints:

* Minimum enforced speed: `0.1x`

---

---

## 9. Key Design Principles

* **Data-driven animation**
* **Single source of truth**: `angular_velocity`
* **Tracker-compatible payload format**
* **Thread-safe networking**
* **Physics → animation coupling**

---

## 10. Summary (Data Perspective)

```text
JSON (angle, velocity)
→ TrackerPayload
→ Angular velocity (deg/s)
→ Linear speed (m/s)
→ Smoothed velocity
→ Character movement
→ Animator playback speed
```

This pipeline ensures **consistent, realistic motion** from simulation to animation.
p