using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FadeByCode : MonoBehaviour
{
    public string nextSceneName = "Basic2";
    public float fadeDuration = 0.5f;
    public float waitBeforeChange = 5f;

    CanvasGroup fadeGroup;

    void Start()
    {
        // Crear un Canvas dinámicamente
        GameObject canvasGO = new GameObject("FadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        fadeGroup = canvasGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;

        StartCoroutine(FadeAndChange());
    }

    IEnumerator FadeAndChange()
    {
        yield return new WaitForSeconds(waitBeforeChange);
        yield return StartCoroutine(Fade(0f, 1f)); // fade a negro
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float start, float end)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(start, end, t / fadeDuration);
            yield return null;
        }
    }
}
