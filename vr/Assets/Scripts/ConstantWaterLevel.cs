using UnityEngine;

public class ConstantWaterLevel : MonoBehaviour, IWaterHeightProvider
{
    public float offsetY = 0f;

    // A�ade estos dos par�metros:
    public float swellAmplitude = 0.15f; // altura del vaiv�n
    public float swellSpeed = 0.6f;      // velocidad del vaiv�n

    public float GetHeight(Vector3 worldPos, float time)
    {
        float swell = Mathf.Sin(time * swellSpeed) * swellAmplitude;
        return transform.position.y + offsetY + swell;
    }
}
