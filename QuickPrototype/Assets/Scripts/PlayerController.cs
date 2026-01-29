using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class PlayerMasterController : MonoBehaviour
{
    public enum ControlMode { Auto, KeyboardOnly, UDPOnly }

    [Header("Control Mode")]
    public ControlMode activeMode = ControlMode.Auto;

    [Header("Main References")]
    public PlayerMovement movement;
    public SyncLegs legs;
    public UDP_SimulatedReceiver udp;

    [Header("Movement Speed")]
    [Range(0.5f, 10f)] public float walkSpeed = 2.0f;
    public float rotationSpeed = 100f;

    [Header("Step Height Settings")]
    [Range(0.2f, 1.0f)] public float stairStepHeight = 0.45f;
    [Range(0.05f, 0.4f)] public float walkStepHeight = 0.1f;
    [Range(0.05f, 0.4f)] public float stepDownArcHeight = 0.15f; 

    [Header("Visual & IK Tuning")]
    [Range(0f, 1f)] public float strideLength = 0.5f;
    [Range(-2.0f, 2.0f)] public float hipHeightOffset = 0f;

    private float virtualAngle = 0f;
    private bool isManualMoving = false;
    private bool isManualTurning = false; // <--- NEW TRACKER
    private float manualStartTime = 0f;

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
            // Reset to UDP if no keys are pressed
            if ((isManualMoving || isManualTurning) && !AnyKeyHeld())
            {
                isManualMoving = false;
                isManualTurning = false;
                manualStartTime = 0f; 
                if (legs) legs.UseUDPControl(); 
            }
        }
    }

    public void SyncAllSettings()
    {
        if (movement) {
            movement.speed = walkSpeed;
            movement.stepHeight = stairStepHeight; 
        }
        if (legs) {
            legs.stairStepHeight = stairStepHeight; 
            legs.flatStepHeight = walkStepHeight;
            legs.downStepHeight = stepDownArcHeight; 
            legs.stridePrediction = strideLength;
            legs.hipHeight = hipHeightOffset;
        }
    }

    // --- INPUT FUNCTIONS ---

    public void MoveForward()
    {
        if (activeMode == ControlMode.UDPOnly) return;
        isManualMoving = true;
        // FIX 1: Send Vector3.forward (Local), NOT transform.forward (World)
        if (movement) movement.AddVelocity(Vector3.forward); 
        UpdateVirtualCycle(1f);
    }

    public void MoveBackward()
    {
        if (activeMode == ControlMode.UDPOnly) return;
        isManualMoving = true;
        // FIX 1: Send Local Backward
        if (movement) movement.AddVelocity(Vector3.back); 
        UpdateVirtualCycle(-1f);
    }

    public void Turn(float direction)
    {
        if (activeMode == ControlMode.UDPOnly) return;
        
        isManualTurning = true; // Mark that we are turning
        if (movement) movement.Rotate(direction * rotationSpeed * Time.deltaTime);

        // FIX 2: If we are turning but NOT walking, march in place to prevent leg twist
        if (!isManualMoving)
        {
            UpdateVirtualCycle(0.5f); // 0.5f speed for turning in place
        }
    }

    public void Stop()
    {
        if (movement) movement.velocity = Vector3.zero;
        isManualMoving = false;
        isManualTurning = false;
        if (legs) legs.UseUDPControl();
    }

    private void UpdateVirtualCycle(float direction)
    {
        if (!isManualMoving && !isManualTurning) manualStartTime = Time.time;

        // If just turning (direction is small), use a slower cycle
        float cycleSpeed = (Mathf.Abs(direction) < 0.9f) ? walkSpeed * 0.5f : walkSpeed;

        float elapsed = Time.time - manualStartTime;
        virtualAngle = (elapsed * cycleSpeed * 360.0f) % 360.0f;

        if (direction < 0) virtualAngle = 360.0f - virtualAngle;

        if (legs) legs.UpdateStepPhase(virtualAngle);
    }

    private bool AnyKeyHeld()
    {
        return Keyboard.current != null && Keyboard.current.anyKey.isPressed;
    }
}