using UnityEngine;

// Esta es solo la INTERFAZ, no se agrega como componente.
public interface IWaterHeightProvider
{
    // Retorna la altura del agua en una posición del mundo y tiempo dado.
    float GetHeight(Vector3 worldPos, float time);
}
