using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public char_animation_temp pitchSource;
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float followSpeed = 10f;
    public float rotateSpeed = 10f;
    public float lookHeightOffset = 1f;
    public float pitchInfluence = 1f;

    private void Awake()
    {
        if (pitchSource == null && target != null)
            pitchSource = target.GetComponent<char_animation_temp>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float pitch = pitchSource != null ? pitchSource.LookPitch * pitchInfluence : 0f;
        Quaternion orbitRot = target.rotation * Quaternion.Euler(pitch, 0f, 0f);

        Vector3 desiredPos = target.position + orbitRot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        Vector3 lookTarget = target.position + Vector3.up * lookHeightOffset;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateSpeed * Time.deltaTime);
    }
}
