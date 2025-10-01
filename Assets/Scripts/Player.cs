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

public class Player : MonoBehaviour
{

    [Header("Player")]
    public float MoveSpeed = 2.0f;
    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    public PlayerState state = PlayerState.Idle;

    [Space(10)]
    public float JumpHeight = 1.2f;

    [Space(10)]
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;

    [Header("Bow")]
    [SerializeField] private Transform nockPoint;
    [SerializeField] private Transform tip02;
    [SerializeField] private Transform tip01;
    [SerializeField] private LineRenderer bowstringLine;

    [Header("UI")]
    [SerializeField] private Slider healthbar;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private Rigidbody _rb;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;
    private bool _hasAnimator;
    [SerializeField] private GameObject canvas;

    public Transform limb01;
    public Transform limb02;

    public Transform bowstringAnchorPoint;

    public AnimationCurve bowReleaseCurve;

    private Vector3 nockPointRestLocalPosition;
    private Vector3 initialLimb01LocalEulerAngles;
    private Vector3 initialLimb02LocalEulerAngles;

    private IEnumerator bowAnimation;
    private IEnumerator bowSheathAnimation;

    [Header("Arrow")]
    public GameObject arrowInHand;
    public GameObject arrowToShoot;

    private IEnumerator getArrowAnimation;

    public GameObject crosshair;

    [Header("Unseathe")]
    public GameObject bowSheathed;
    public GameObject bowInHand;

    private bool canJump = true;
    private bool isJumpCooldownRunning = false;

    private int health = 3;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    void OnEnable()
    {
        if (nockPoint)
        {
            nockPointRestLocalPosition = nockPoint.localPosition;
        }

        if (limb01 && limb02)
        {
            initialLimb01LocalEulerAngles = limb01.localEulerAngles;
            initialLimb02LocalEulerAngles = limb02.localEulerAngles;
        }
    }

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        crosshair.SetActive(_input.aim);
    }

    private void LateUpdate()
    {
        CameraRotation();
        CreateBowstring();
    }

#if UNITY_EDITOR
    //Places bowstring even in Edit mode
    void OnValidate()
    {
        CreateBowstring();
    }
#endif 

    void CreateBowstring()
    {
        if (!bowstringLine || !tip01 || !tip02 || !nockPoint)
        {
            return;
        }

        bowstringLine.positionCount = 3;
        bowstringLine.SetPosition(0, tip01.position);
        bowstringLine.SetPosition(1, nockPoint.position);
        bowstringLine.SetPosition(2, tip02.position);
    }

    private void FixedUpdate()
    {
        Move();

        if (_input.jump && canJump)
        {
            canJump = false;
            _rb.AddForce(Vector3.up * JumpHeight, ForceMode.Impulse);

            // Start cooldown only once after jumping
            if (!isJumpCooldownRunning)
            {
                StartCoroutine(JumpCooldown());
            }
        }

        // Clamp horizontal velocity only (not vertical)
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MoveSpeed);
        _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
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

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch,
            _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, 0.2f, LayerMask.GetMask("Default")))
            return;

        float targetSpeed = MoveSpeed;
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        float targetSpeedWithInput = targetSpeed * inputMagnitude;

        // Smoothly interpolate _speed towards target
        _speed = Mathf.MoveTowards(_speed, targetSpeedWithInput, SpeedChangeRate * Time.deltaTime);

        if (targetSpeed == 0.0f)
            _speed = 0.0f;

        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero || _input.aim)
        {
            if (_input.aim)
            {
                inputDirection = Vector3.zero;
            }
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        Vector3 velocity = targetDirection.normalized * _speed;
        velocity.y = _rb.linearVelocity.y; // preserve vertical velocity

        _rb.linearVelocity = _input.aim ? new Vector3(0, _rb.linearVelocity.y, 0) : velocity;

        Vector3 currentHorizontalVelocity = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z);
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

        if (_hasAnimator)
        {
            _animator.SetInteger("State", (int)state);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    
    public void Damage()
    {
        health -= 1;
        healthbar.value = health;
        healthbar.fillRect.GetComponent<Image>().color = health == 2 ? Color.yellow : Color.red;

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (canvas != null)
            canvas.SetActive(true);

        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    ///BOW PULL/RELEASE ANIMATION
    public void LoadBow(float delay, float duration)
    {
        if (bowAnimation != null)
        {
            StopCoroutine(bowAnimation);
            nockPoint.localPosition = nockPointRestLocalPosition;
        }
        bowAnimation = LoadBowCoroutine(delay, duration);
        StartCoroutine(bowAnimation);
    }
    public void ShootArrow(float delay, float duration)
    {
        if (bowAnimation != null)
        {
            StopCoroutine(bowAnimation);
            nockPoint.position = bowstringAnchorPoint.position;
        }
        bowAnimation = ShootArrowCoroutine(delay, duration);
        StartCoroutine(bowAnimation);
    }
    public void CancelLoadBow(float delay, float cancelDuration)
    {
        if (bowAnimation != null)
        {
            StopCoroutine(bowAnimation);
        }
        bowAnimation = CancelLoadBowCoroutine(delay, cancelDuration);
        StartCoroutine(bowAnimation);
    }

    private IEnumerator LoadBowCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        Vector3 limb01LoadLocalEulerAngles =
        new Vector3(initialLimb01LocalEulerAngles.x, initialLimb01LocalEulerAngles.y, initialLimb01LocalEulerAngles.z - 15f);
        Vector3 limb02LoadLocalEulerAngles =
        new Vector3(initialLimb02LocalEulerAngles.x, initialLimb02LocalEulerAngles.y, initialLimb02LocalEulerAngles.z - 15f);

        nockPoint.localPosition = nockPointRestLocalPosition;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            limb01.localEulerAngles =
            Vector3.Lerp(initialLimb01LocalEulerAngles, limb01LoadLocalEulerAngles, t);
            limb02.localEulerAngles =
            Vector3.Lerp(initialLimb02LocalEulerAngles, limb02LoadLocalEulerAngles, t);

            nockPoint.position = Vector3.Lerp(nockPoint.position, bowstringAnchorPoint.position, t);

            yield return null;
        }
    }

    private IEnumerator ShootArrowCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        Vector3 limb01LoadLocalEulerAngles =
        new Vector3(initialLimb01LocalEulerAngles.x, initialLimb01LocalEulerAngles.y, initialLimb01LocalEulerAngles.z - 15f);
        Vector3 limb02LoadLocalEulerAngles =
        new Vector3(initialLimb02LocalEulerAngles.x, initialLimb02LocalEulerAngles.y, initialLimb02LocalEulerAngles.z - 15f);

        Vector3 initialNockRestLocalPosition = nockPoint.localPosition;

        arrowInHand.SetActive(false);

        Quaternion rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
        Instantiate(arrowToShoot, bowstringAnchorPoint.position, rotation);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            limb01.localEulerAngles =
            Vector3.LerpUnclamped(limb01LoadLocalEulerAngles, initialLimb01LocalEulerAngles, bowReleaseCurve.Evaluate(t));
            limb02.localEulerAngles =
            Vector3.LerpUnclamped(limb02LoadLocalEulerAngles, initialLimb02LocalEulerAngles, bowReleaseCurve.Evaluate(t));

            nockPoint.localPosition = Vector3.LerpUnclamped(initialNockRestLocalPosition, nockPointRestLocalPosition, bowReleaseCurve.Evaluate(t));

            yield return null;
        }
    }

    private IEnumerator CancelLoadBowCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        Vector3 limb01LoadLocalEulerAngles =
        new Vector3(initialLimb01LocalEulerAngles.x, initialLimb01LocalEulerAngles.y, initialLimb01LocalEulerAngles.z - 15f);
        Vector3 limb02LoadLocalEulerAngles =
        new Vector3(initialLimb02LocalEulerAngles.x, initialLimb02LocalEulerAngles.y, initialLimb02LocalEulerAngles.z - 15f);

        Vector3 initialNockRestLocalPosition = nockPoint.localPosition;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            limb01.localEulerAngles =
            Vector3.LerpUnclamped(limb01LoadLocalEulerAngles, initialLimb01LocalEulerAngles, t);
            limb02.localEulerAngles =
            Vector3.LerpUnclamped(limb02LoadLocalEulerAngles, initialLimb02LocalEulerAngles, t);

            nockPoint.localPosition = Vector3.LerpUnclamped(initialNockRestLocalPosition, nockPointRestLocalPosition, t);

            yield return null;
        }
    }

    ///GET ARROW AFTER SHOOTING
    public void GetArrow(float delay)
    {
        if (getArrowAnimation != null)
        {
            StopCoroutine(getArrowAnimation);
        }
        getArrowAnimation = GetArrowCoroutine(delay);
        StartCoroutine(getArrowAnimation);
    }

    private IEnumerator GetArrowCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        arrowInHand.SetActive(true);
    }

    ///BOW UNSHEATHE / SHEATHE
    public void UnsheatheBow(float delay)
    {
        if (bowSheathAnimation != null)
        {
            StopCoroutine(bowSheathAnimation);
        }
        bowSheathAnimation = UnsheatheBowCoroutine(delay);
        StartCoroutine(bowSheathAnimation);
    }

    private IEnumerator UnsheatheBowCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        bowSheathed.SetActive(false);
        bowInHand.SetActive(true);

    }

    public void SheatheBow(float delay)
    {
        if (bowSheathAnimation != null)
        {
            StopCoroutine(bowSheathAnimation);
        }
        bowSheathAnimation = SheatheBowCoroutine(delay);
        StartCoroutine(bowSheathAnimation);
    }

    private IEnumerator SheatheBowCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        bowSheathed.SetActive(true);
        bowInHand.SetActive(false);
    }

}