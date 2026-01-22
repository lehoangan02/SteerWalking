# 2D Circular Motion UDP Sender

A small **Pygame** demo that animates uniform circular motion and sends motion data over **UDP** at a fixed rate.

---

## What It Does
- Displays a point moving in a circle (uniform circular motion)
- Speed adjustable via slider or keyboard
- Sends angle and angular velocity over UDP at a constant frequency (independent of FPS)

---

## Controls
- **Mouse drag on slider** – change rotation speed (rps)
- **+ / - keys** – increase / decrease speed
- **ESC / Close window** – exit

---

## UDP Output
Sent as JSON at **90 Hz**:
```json
{
  "angle_deg": <float>,          // current angle (degrees)
  "angular_velocity": <float>,   // degrees per second
  "ts": <unix_timestamp>
}
````

---

## Run

```bash
python script.py [ip] [port]
```

Defaults:

* IP: `127.0.0.1`
* Port: `9000`

---

## Notes

* Rendering runs at up to 120 FPS
* Network timing is decoupled from rendering
* Suitable for VR, simulation, or motion-sync testing