using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PlayerState
{
    Idle = 0,
    Walking = 1,
    Running = 2
}

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    public float moveSpeed = 2.0f;

    [Range(0.0f, 0.3f)] public float rotationSmoothTime = 0.12f;

    public float speedChangeRate = 10.0f;

    private float speed;

    private float targetRotation = 0.0f;

    private float rotationVelocity;

    private PlayerState state = PlayerState.Idle;

    public float JumpHeight = 1.2f;

    private bool canJump = true;
    private bool isJumpCooldownRunning = false;

    [Header("Animations")]

    private Animator animator;

    private bool hasAnimator;

    private Rigidbody rb;
    private PlayerControls controls;
    private GameObject mainCamera;


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
    }

    private void Update()
    {
        if(!hasAnimator)
            hasAnimator = TryGetComponent(out animator);
    }

    private void FixedUpdate()
    {
        Move();

        if (controls.jump && canJump)
        {
            canJump = false;
            rb.AddForce(Vector3.up * JumpHeight, ForceMode.Impulse);

            // Start cooldown only once after jumping
            if (!isJumpCooldownRunning)
            {
                StartCoroutine(JumpCooldown());
            }
        }

        // Clamp horizontal velocity only (not vertical)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, moveSpeed);
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    private IEnumerator JumpCooldown()
    {
        isJumpCooldownRunning = true;
        yield return new WaitForSeconds(0.4f);

        while (!Physics.Raycast(transform.position, Vector3.down, 0.2f, LayerMask.GetMask("Default")))
        {
            yield return null; // Wait one frame
        }

        canJump = true;
        isJumpCooldownRunning = false;
    }

    private void Move()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, 0.2f, LayerMask.GetMask("Default")))
            return;

        float targetSpeed = moveSpeed;
        if (controls.move == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = controls.analogMovement ? controls.move.magnitude : 1f;

        float targetSpeedWithInput = targetSpeed * inputMagnitude;

        // Smoothly interpolate _speed towards target
        speed = Mathf.MoveTowards(speed, targetSpeedWithInput, speedChangeRate * Time.deltaTime);

        if (targetSpeed == 0.0f)
            speed = 0.0f;

        Vector3 inputDirection = new Vector3(controls.move.x, 0.0f, controls.move.y).normalized;

        if (controls.move != Vector2.zero || controls.aim)
        {
            if (controls.aim)
            {
                inputDirection = Vector3.zero;
            }
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        Vector3 velocity = targetDirection.normalized * speed;
        velocity.y = rb.linearVelocity.y; // preserve vertical velocity

        rb.linearVelocity = controls.aim ? new Vector3(0, rb.linearVelocity.y, 0) : velocity;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0.0f, rb.linearVelocity.z);
        float currentHorizontalSpeed = currentHorizontalVelocity.magnitude;

        if (currentHorizontalSpeed > 5.0f)
        {
            state = PlayerState.Running;
        }
        else if (currentHorizontalSpeed > 0.1f)
        {
            state = PlayerState.Walking;
        }
        else
        {
            state = PlayerState.Idle;
        }

        if (hasAnimator)
        {
            animator.SetInteger("State", (int)state);
        }
    }
}