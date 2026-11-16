using UnityEngine;

public class SpriteFadeIn : MonoBehaviour
{
    public float duration = 0.25f;
    public float targetAlpha = 1f;

    private float _timer = 0f;
    private SpriteRenderer _sr;

    public static void Play(GameObject target, float targetAlpha = 1f, float duration = 0.25f)
    {
        if (target == null) return;
        var sr = target.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        var fade = target.GetComponent<SpriteFadeIn>();
        if (fade == null) fade = target.AddComponent<SpriteFadeIn>();

        fade.targetAlpha = targetAlpha;
        fade.duration = duration;
        fade._timer = 0f;
        fade._sr = sr;

        Color c = sr.color;
        c.a = 0f;
        sr.color = c;
    }

    void Update()
    {
        if (Time.timeScale == 0f || _sr == null) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration);

        Color c = _sr.color;
        c.a = Mathf.Lerp(0f, targetAlpha, t);
        _sr.color = c;

        if (t >= 1f)
        {
            Destroy(this); // Завершили появу — компонент більше не заважає іншим ефектам
        }
    }
}