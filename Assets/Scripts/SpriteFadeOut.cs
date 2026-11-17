using UnityEngine;

public class SpriteFadeOut : MonoBehaviour
{
    public float duration = 0.25f;
    public bool destroyOnComplete = true;

    private float _timer = 0f;
    private float _startAlpha = 1f;
    private SpriteRenderer _sr;

    // Плавно зменшує прозорість до 0 і знищує об'єкт
    public static void PlayAndDestroy(GameObject target, float duration = 0.25f)
    {
        StartFade(target, duration, true);
    }

    // Плавно зменшує прозорість до 0 і вимикає SpriteRenderer (для багаторазових об'єктів як MutagenFood)
    public static void PlayAndHide(GameObject target, float duration = 0.25f)
    {
        StartFade(target, duration, false);
    }

    private static void StartFade(GameObject target, float duration, bool destroy)
    {
        if (target == null) return;

        // Якщо об'єкт ще не встиг до кінця з'явитися — прибираємо SpriteFadeIn, щоб не було конфлікту
        var fadeIn = target.GetComponent<SpriteFadeIn>();
        if (fadeIn != null) Destroy(fadeIn);

        var sr = target.GetComponent<SpriteRenderer>();
        if (sr == null || !sr.enabled)
        {
            if (destroy) Destroy(target);
            return;
        }

        var fadeOut = target.GetComponent<SpriteFadeOut>();
        if (fadeOut == null) fadeOut = target.AddComponent<SpriteFadeOut>();

        fadeOut.duration = duration;
        fadeOut.destroyOnComplete = destroy;
        fadeOut._timer = 0f;
        fadeOut._sr = sr;
        fadeOut._startAlpha = sr.color.a;
    }

    void Update()
    {
        if (Time.timeScale == 0f || _sr == null) return;

        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / duration);

        Color c = _sr.color;
        c.a = Mathf.Lerp(_startAlpha, 0f, t);
        _sr.color = c;

        if (t >= 1f)
        {
            if (destroyOnComplete)
            {
                Destroy(gameObject);
            }
            else
            {
                _sr.enabled = false;
                c.a = 1f;
                _sr.color = c;
                Destroy(this);
            }
        }
    }
}