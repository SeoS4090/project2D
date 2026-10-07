using UnityEngine;

public sealed class TrainingCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Range(1f, 20f)] private float followSharpness = 8f;
    [SerializeField, Min(1f)] private float pixelsPerUnit = 32f;

    public void SetTarget(Transform value) => target = value;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 destination = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothed = Vector3.Lerp(transform.position, destination, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        float unitsPerPixel = 1f / pixelsPerUnit;
        transform.position = new Vector3(
            Mathf.Round(smoothed.x / unitsPerPixel) * unitsPerPixel,
            Mathf.Round(smoothed.y / unitsPerPixel) * unitsPerPixel,
            smoothed.z);
    }
}
