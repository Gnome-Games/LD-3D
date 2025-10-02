using StarterAssets;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class PlayerCameraManager : MonoBehaviour
{
    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;

    private PlayerControls controls;
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;
    private const float threshold = 0.01f;


    private void Start()
    {

        controls = GetComponent<PlayerControls>();

        cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        if (controls.look.sqrMagnitude >= threshold)
        {
            float deltaTimeMultiplier = controls.IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            cinemachineTargetYaw += controls.look.x * deltaTimeMultiplier;
            cinemachineTargetPitch += controls.look.y * deltaTimeMultiplier;
        }

        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch,
            cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
