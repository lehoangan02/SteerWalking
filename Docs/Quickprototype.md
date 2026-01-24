# 🎮 Player Master Controller (VR & Keyboard)

This document explains how to use the **PlayerMasterController** to move the character. This script acts as the "Brain" of the player, automatically switching between your **Python Simulation (UDP)** and **Keyboard Controls**.

## 1. How to Setup in Unity

To ensure the VR headset, physics, and animations work correctly, follow this hierarchy:

### Hierarchy Structure
* **[XR Origin]** (The top-level VR Rig)
  * **[Camera Offset]**
    * **Main Camera** (Your Headset)
  * **[CharacterVisuals]** (The 3D Model)
    * ⮕ **Add Component**: `PlayerMasterController`
    * ⮕ **Add Component**: `PlayerMovement`
    * ⮕ **Add Component**: `SyncLegs`
    * ⮕ **Add Component**: `UDP_SimulatedReceiver`

### Inspector Setup
1. Drag the **XR Origin** object into the `Xr Origin` slot on the Master Controller.
2. Ensure `Ground Layer` (in SyncLegs) is set to the layer used by your floor/environment.

---

## 2. Control Modes

You can switch between these modes in the Inspector dropdown:

| Mode | Description |
| :--- | :--- |
| **Auto** | **Recommended.** The character uses the Python Sim by default. If you press a key, it switches to Keyboard mode instantly. |
| **KeyboardOnly** | Ignores the Python script. Use this for testing movement inside Unity alone. |
| **UDPOnly** | Ignores the Keyboard. Use this for 100% Python-driven simulation. |

---

## 3. Data Sync (Python ↔ Unity)

The **Walk Speed** in Unity is now synchronized with your Python script's **RPS (Rotations Per Second)**.

* **Python Logic**: `angle = (time * rps * 360) % 360`
* **Unity Logic**: `virtualAngle = (Time.time * walkSpeed * 360) % 360`

> **Tip:** If you set **Walk Speed** to `1.0` in Unity, the legs will complete exactly **one full walk cycle per second**, matching the Python simulation perfectly.

---

## 4. How to Script Your Own Controls

If you want to create a custom input script, simply reference the `PlayerMasterController` and call the public functions.

**Note**: You do not need to calculate rotations or physics; the Master Controller handles it.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MyCustomControls : MonoBehaviour 
{
    public PlayerMasterController master;

    void Update() 
    {
        // 1. Move Forward (Moves in the direction the VR Headset is looking)
        if (Keyboard.current.wKey.isPressed) master.MoveForward();

        // 2. Move Backward
        if (Keyboard.current.sKey.isPressed) master.MoveBackward();

        // 3. Manual Stop (Resets physics and returns control to UDP)
        if (Keyboard.current.spaceKey.wasPressedThisFrame) master.Stop();
    }
}