using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PedalAnimator : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Drag your X Bot object here (the one with PedalReceiver)")]
    public PedalReceiver networkSource; 

    [Header("Setup")]
    public string stateName = "Walking"; 
    public Transform targetToMove; 
    public Transform headCamera; 

    [Header("Tuning")]
    public float distancePerCycle = 1.5f; 
    public float gearRatio = 1.0f;
    public bool reverseAnimation = false;

    // Internal Vars
    private Animator animator;
    private float currentAngle = 0f;
    private float lastAngle = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if(headCamera == null) headCamera = Camera.main.transform;
        animator.speed = 0; 

        if (networkSource == null) networkSource = GetComponent<PedalReceiver>();
    }

    void Update()
    {
        if (networkSource == null || !networkSource.hasData) return;

        // 1. Get 3D Positions (X, Y, Z)
        Vector3 left = networkSource.leftPos;
        Vector3 right = networkSource.rightPos;

        // 2. Calculate the "Crank Vector" in 3D
        // This is the line connecting your left foot to your right foot
        Vector3 footDifference = right - left;

        // 3. Project this 3D vector onto the Pedaling Plane
        // We define "Forward" as where the camera is looking (flattened on ground)
        Vector3 forwardDir = headCamera.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        // Calculate 3D components:
        // 'forwardOffset' = How far forward one foot is vs the other (Z-depth relative to player)
        float forwardOffset = Vector3.Dot(footDifference, forwardDir);
        
        // 'heightOffset' = How high one foot is vs the other (Y-height)
        float heightOffset = footDifference.y;

        // 4. Calculate Angle from these 3D offsets
        // Atan2(y, x) computes the angle of the vector
        float rawAngleDegrees = Mathf.Atan2(heightOffset, forwardOffset) * Mathf.Rad2Deg;
        
        if (rawAngleDegrees < 0) rawAngleDegrees += 360f;

        // 5. Smooth & Play Animation
        currentAngle = Mathf.LerpAngle(currentAngle, rawAngleDegrees, Time.deltaTime * 15f);

        float adjustedAngle = currentAngle / gearRatio;
        float normalizedTime = Mathf.Repeat(adjustedAngle, 360f) / 360f;

        if (reverseAnimation) normalizedTime = 1f - normalizedTime;
        animator.Play(stateName, 0, normalizedTime);

        // 6. Move Character
        float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
        if (Mathf.Abs(delta) > 0.01f)
        {
            float moveAmount = (Mathf.Abs(delta) / 360f) * distancePerCycle;
            
            if (targetToMove != null && forwardDir != Vector3.zero)
                targetToMove.Translate(forwardDir * moveAmount, Space.World);
        }
        lastAngle = currentAngle;
    }
}