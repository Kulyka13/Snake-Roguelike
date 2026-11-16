using UnityEngine;

public class PortalApplePair : MonoBehaviour
{
    public Vector2Int portalAPos;
    public Vector2Int portalBPos;

    [HideInInspector] public bool isTraversing = false;

    private GameObject _portalAObj;
    private GameObject _portalBObj;
    private float _lifeTime;
    private SpriteRenderer _srA;
    private SpriteRenderer _srB;
    private System.Action<PortalApplePair> _onExpired;

    public void Init(Vector2Int posA, Vector2Int posB, GameObject prefab, Sprite spriteA, Sprite spriteB, float gridSize, float duration, System.Action<PortalApplePair> onExpired)
    {
        portalAPos = posA;
        portalBPos = posB;
        _lifeTime = duration;
        _onExpired = onExpired;

        _portalAObj = CreatePortalVisual(prefab, posA, gridSize, spriteA, new Color(0.2f, 0.7f, 1f, 1f), out _srA);
        _portalBObj = CreatePortalVisual(prefab, posB, gridSize, spriteB, new Color(0.2f, 1f, 0.85f, 1f), out _srB);
    }

    private GameObject CreatePortalVisual(GameObject prefab, Vector2Int gridPos, float gridSize, Sprite customSprite, Color fallbackColor, out SpriteRenderer sr)
    {
        GameObject obj = Instantiate(prefab, new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0f), Quaternion.identity, transform);
        var baseFood = obj.GetComponent<Food>();
        if (baseFood != null) baseFood.enabled = false;

        sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (customSprite != null)
            {
                sr.sprite = customSprite;
                sr.color = Color.white;
            }
            else
            {
                sr.color = fallbackColor;
            }
        }

        SpriteFadeIn.Play(obj, 1f, 0.3f);
        return obj;
    }

    public void BeginTraversal()
    {
        isTraversing = true;
        SetAlpha(_srA, 1f);
        SetAlpha(_srB, 1f);
    }

    void Update()
    {
        if (Time.timeScale == 0f || isTraversing) return;

        _lifeTime -= Time.deltaTime;

        if (_lifeTime <= 2f)
        {
            float alpha = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
            SetAlpha(_srA, alpha);
            SetAlpha(_srB, alpha);
        }

        if (_lifeTime <= 0f)
        {
            CleanupAndDestroy();
        }
    }

    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    public void CleanupAndDestroy()
    {
        _onExpired?.Invoke(this);
        if (_portalAObj != null) Destroy(_portalAObj);
        if (_portalBObj != null) Destroy(_portalBObj);
        Destroy(gameObject);
    }
}