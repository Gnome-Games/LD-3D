using StarterAssets;
using System.Collections;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{

    [Header("Bow")]
    [SerializeField] private Transform nockPoint;
    [SerializeField] private Transform tip02;
    [SerializeField] private Transform tip01;
    [SerializeField] private LineRenderer bowstringLine;
    public Transform bowLimb01;
    public Transform bowLimb02;
    public Transform bowStringAnchorPoint;
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


    private PlayerControls controls;
    private Camera mainCamera;


    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Start()
    {
        controls = GetComponent<PlayerControls>();
    }

    void OnEnable()
    {
        if (nockPoint)
        {
            nockPointRestLocalPosition = nockPoint.localPosition;
        }

        if (bowLimb01 && bowLimb02)
        {
            initialLimb01LocalEulerAngles = bowLimb01.localEulerAngles;
            initialLimb02LocalEulerAngles = bowLimb02.localEulerAngles;
        }
    }

    private void Update()
    {
        crosshair.SetActive(controls.aim);
    }

    private void LateUpdate()
    {
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
            nockPoint.position = bowStringAnchorPoint.position;
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
            bowLimb01.localEulerAngles =
            Vector3.Lerp(initialLimb01LocalEulerAngles, limb01LoadLocalEulerAngles, t);
            bowLimb02.localEulerAngles =
            Vector3.Lerp(initialLimb02LocalEulerAngles, limb02LoadLocalEulerAngles, t);

            nockPoint.position = Vector3.Lerp(nockPoint.position, bowStringAnchorPoint.position, t);

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

        // Ray from center of screen
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000f); // Arbitrary distant point
        }

        // Direction from bow to target point
        Vector3 shootDirection = (targetPoint - bowStringAnchorPoint.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(shootDirection);

        // Instantiate arrow
        Instantiate(arrowToShoot, bowStringAnchorPoint.position, rotation);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            bowLimb01.localEulerAngles =
            Vector3.LerpUnclamped(limb01LoadLocalEulerAngles, initialLimb01LocalEulerAngles, bowReleaseCurve.Evaluate(t));
            bowLimb02.localEulerAngles =
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
            bowLimb01.localEulerAngles =
            Vector3.LerpUnclamped(limb01LoadLocalEulerAngles, initialLimb01LocalEulerAngles, t);
            bowLimb02.localEulerAngles =
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
