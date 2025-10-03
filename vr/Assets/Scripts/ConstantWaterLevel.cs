using UnityEngine;

public class ConstantWaterLevel : MonoBehaviour, IWaterHeightProvider
{
    public float offsetY = 0f;

    // Añade estos dos parámetros:
    public float swellAmplitude = 0.15f; // altura del vaivén
    public float swellSpeed = 0.6f;      // velocidad del vaivén

    public float GetHeight(Vector3 worldPos, float time)
    {
        float swell = Mathf.Sin(time * swellSpeed) * swellAmplitude;
        return transform.position.y + offsetY + swell;
    }
}
