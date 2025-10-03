using UnityEngine;

// Implementación con olas senoidales simples
public class SimpleGerstnerWater : MonoBehaviour, IWaterHeightProvider
{
    public float amplitude = 0.35f;
    public float wavelength = 7f;
    public float speed = 1.1f;
    public Vector2 dir1 = new Vector2(1, 0);
    public Vector2 dir2 = new Vector2(0.5f, 0.86f);

    public float GetHeight(Vector3 worldPos, float time)
    {
        float k = 2f * Mathf.PI / Mathf.Max(0.001f, wavelength);
        float c = Mathf.Sqrt(9.81f / k) * speed;

        float d1 = Vector2.Dot(dir1.normalized, new Vector2(worldPos.x, worldPos.z));
        float d2 = Vector2.Dot(dir2.normalized, new Vector2(worldPos.x, worldPos.z));

        float h = amplitude * Mathf.Sin(k * d1 - c * time)
                + amplitude * 0.6f * Mathf.Sin(k * d2 - (c * 0.8f) * time);

        return transform.position.y + h;
    }
}
