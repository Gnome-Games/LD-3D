using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    public float moveSpeed = 2.0f;

    [Range(0.0f, 0.3f)] public float rotationSmoothTime = 0.12f;

    public float speedChangeRate = 10.0f;

    private float speed;

    private float targetRotation = 0.0f;

    private float rotationVelocity;

    public float JumpHeight = 1.2f;

    public float jumpCooldown = 1.0f;

    private bool canJump = true;
    private bool isJumpCooldownRunning = false;

    [Header("Animations")]

    private Animator animator;

    private bool hasAnimator;

    private Rigidbody rb;
    private PlayerControls controls;
    private GameObject mainCamera;

    // animation IDs
    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    private float animationBlend;

    public float groundedRadius;
    public LayerMask groundLayers;
    public float groundedOffset;

    private bool grounded;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        controls = GetComponent<PlayerControls>();

        AssignAnimationIDs();
    }

    private void Update()
    {
        if(!hasAnimator)
            hasAnimator = TryGetComponent(out animator);

        GroundedCheck();
    }

    private void FixedUpdate()
    {
        Move();

        if (controls.jump && canJump && grounded) 
        {
            canJump = false;
            rb.AddForce(Vector3.up * JumpHeight, ForceMode.Impulse);

            if (hasAnimator)
            {
                animator.SetBool(animIDJump, true);
            }

            // Start cooldown only once after jumping
            if (!isJumpCooldownRunning)
            {
                StartCoroutine(JumpCooldown());
            }
        } 
        if (hasAnimator)
        {
            animator.SetBool(animIDFreeFall, !grounded);
        }
       

        // Clamp horizontal velocity only (not vertical)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, moveSpeed);
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    private IEnumerator JumpCooldown()
    {
        isJumpCooldownRunning = true;


        if (hasAnimator)
        {
            animator.SetBool(animIDJump, false);
        }

        yield return new WaitForSeconds(jumpCooldown);

        while (!grounded)
        {
            yield return null; // Wait one frame
        }

        canJump = true;
        isJumpCooldownRunning = false;
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
            transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers.value,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (hasAnimator)
        {
            animator.SetBool(animIDGrounded, grounded);
        }
    }

    private void Move()
    {
        float targetSpeed = moveSpeed;

        if (controls.move == Vector2.zero)
            targetSpeed = 0.0f;

        // Get current horizontal speed from Rigidbody
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0.0f, rb.linearVelocity.z);
        float currentHorizontalSpeed = horizontalVelocity.magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = controls.analogMovement ? controls.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * speedChangeRate);
            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }

        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        Vector3 inputDirection = new Vector3(controls.move.x, 0.0f, controls.move.y).normalized;

        // Rotation
        if (controls.move != Vector2.zero || controls.aim)
        {
            if (controls.aim)
            {
                inputDirection = Vector3.zero;
                speed = 0.0f;
            }

            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              mainCamera.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity,
                rotationSmoothTime);

            Quaternion targetRot = Quaternion.Euler(0.0f, rotation, 0.0f);
            rb.MoveRotation(targetRot); // <-- Rigidbody rotation
        }

        // Direction relative to camera
        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        Vector3 finalVelocity = targetDirection.normalized * speed;
        finalVelocity.y = rb.linearVelocity.y; // preserve vertical movement (e.g. gravity/jumping)

        rb.linearVelocity = finalVelocity; // <-- Rigidbody position move

        // Update animator if using character
        if (hasAnimator)
        {
            animator.SetFloat(animIDSpeed, animationBlend);
            animator.SetFloat(animIDMotionSpeed, inputMagnitude);
        }
    }


    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
            groundedRadius);
    }
}