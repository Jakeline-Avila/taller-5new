using UnityEngine;

public class SyncLegacyAnimations : MonoBehaviour
{
    [Header("Cuerpo")]
    public Animation animCuerpo;
    public string clipCuerpo = "ArmatureAction";

    [Header("Objeto")]
    public Animation animObjeto;
    public string clipObjeto = "CircleAction";

    public float fade = 0.1f;   // CrossFade suave

    void Start()
    {
        // Configura modo de repetición si lo necesitas
        animCuerpo[clipCuerpo].wrapMode = WrapMode.Loop;  // o Once/ClampForever
        animObjeto[clipObjeto].wrapMode = WrapMode.Loop;

        // Opción: arrancar alineados en el mismo tiempo
        animCuerpo.Play(clipCuerpo);
        animObjeto[clipObjeto].time = animCuerpo[clipCuerpo].time;
        animObjeto.CrossFade(clipObjeto, fade);
    }
}
