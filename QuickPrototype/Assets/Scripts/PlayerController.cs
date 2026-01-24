using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class PlayerMasterController : MonoBehaviour
{
    // The three modes for your friend to choose from
    public enum ControlMode { 
        Auto,           // Switches to keyboard when a key is pressed, otherwise uses UDP
        KeyboardOnly,   // Ignores Python simulation entirely
        UDPOnly         // Ignores Keyboard inputs entirely
    }

    [Header("Control Mode")]
    public ControlMode activeMode = ControlMode.Auto;

    [Header("Main References")]
    public PlayerMovement movement;
    public SyncLegs legs;
    public UDP_SimulatedReceiver udp;

    [Header("Movement Settings")]
    [Range(0.5f, 10f)] public float walkSpeed = 2.0f;
    [Range(0.1f, 1.0f)] public float globalStepHeight = 0.4f;
    public float rotationSpeed = 100f;

    [Header("Visual & IK Tuning")]
    [Range(0f, 1f)] public float strideLength = 0.5f;
    [Range(-2.0f, 2.0f)] public float hipHeightOffset = 0f;

    private float virtualAngle = 0f;
    private bool isManualMoving = false;

    private void Awake()
    {
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!legs) legs = GetComponent<SyncLegs>();
        if (!udp) udp = GetComponent<UDP_SimulatedReceiver>();
    }

    private void Update()
    {
        SyncAllSettings();

        if (activeMode == ControlMode.Auto)
        {
            if (isManualMoving && !AnyKeyHeld())
            {
                isManualMoving = false;
                manualStartTime = 0f; // Reset the clock for next time
                if (legs) legs.UseUDPControl(); 
            }
        }
    }

    private void HandleModeTransitions()
    {
        if (legs == null) return;

        switch (activeMode)
        {
            case ControlMode.UDPOnly:
                legs.UseUDPControl();
                isManualMoving = false;
                break;

            case ControlMode.KeyboardOnly:
                // Do nothing, wait for manual function calls
                break;

            case ControlMode.Auto:
                // If we were moving manually but stopped, go back to UDP
                if (isManualMoving && !AnyKeyHeld())
                {
                    isManualMoving = false;
                    legs.UseUDPControl(); 
                }
                break;
        }
    }

    public void SyncAllSettings()
    {
        if (movement) {
            movement.speed = walkSpeed;
            movement.stepHeight = globalStepHeight;
        }
        if (legs) {
            legs.stepHeight = globalStepHeight; 
            legs.stridePrediction = strideLength;
            legs.hipHeight = hipHeightOffset;
        }
    }

    // --- FRIENDLY INTERFACE FUNCTIONS ---

    public void MoveForward()
    {
        if (activeMode == ControlMode.UDPOnly) return; // Block if in UDP mode

        isManualMoving = true;
        if (movement) movement.AddVelocity(transform.forward);
        UpdateVirtualCycle(1f);
    }

    public void MoveBackward()
    {
        if (activeMode == ControlMode.UDPOnly) return;

        isManualMoving = true;
        if (movement) movement.AddVelocity(-transform.forward);
        UpdateVirtualCycle(-1f);
    }

    public void Turn(float direction)
    {
        if (activeMode == ControlMode.UDPOnly) return;
        if (movement) movement.Rotate(direction * rotationSpeed * Time.deltaTime);
    }

    public void Stop()
    {
        if (movement) movement.velocity = Vector3.zero;
        isManualMoving = false;
        if (legs) legs.UseUDPControl();
    }

    private float manualStartTime = 0f;

    private void UpdateVirtualCycle(float direction)
    {
        // If we just started moving, record the start time to sync the clock
        if (!isManualMoving) {
            manualStartTime = Time.time;
        }

        // This matches your Python math: (time * speed * 360) % 360
        // We use Time.time - manualStartTime so the leg starts at 0 degrees when you press the key
        float elapsed = Time.time - manualStartTime;
        
        // direction is 1 for forward, -1 for backward
        virtualAngle = (elapsed * walkSpeed * 360.0f) % 360.0f;

        // Handle backward motion correctly for the modulo
        if (direction < 0) {
            virtualAngle = 360.0f - virtualAngle;
        }

        if (legs) legs.UpdateStepPhase(virtualAngle);
    }
    private bool AnyKeyHeld()
    {
        return Keyboard.current != null && Keyboard.current.anyKey.isPressed;
    }
}