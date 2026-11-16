using UnityEngine;

public class BonusApple : MonoBehaviour
{
    public Vector2Int gridPosition;
    public bool isRotten = false;

    private float _lifeTime;
    private SpriteRenderer _sr;
    private System.Action<BonusApple> _onExpired;

    public void Init(Vector2Int gridPos, float gridSize, float duration, bool rotten, Sprite customSprite, System.Action<BonusApple> onExpired)
    {
        gridPosition = gridPos;
        isRotten = rotten;
        _lifeTime = duration;
        _onExpired = onExpired;

        transform.position = new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0f);
        _sr = GetComponent<SpriteRenderer>();

        if (_sr != null)
        {
            if (customSprite != null)
            {
                _sr.sprite = customSprite;
                _sr.color = Color.white;
            }
            else
            {
                _sr.color = isRotten ? new Color(0.45f, 0.2f, 0.55f, 1f) : new Color(1f, 0.55f, 0.1f, 1f);
                if (!isRotten) transform.localScale *= 0.8f;
            }
        }

        SpriteFadeIn.Play(gameObject, 1f, 0.25f);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        _lifeTime -= Time.deltaTime;

        if (_lifeTime <= 2f && _sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
            _sr.color = c;
        }

        if (_lifeTime <= 0f)
        {
            _onExpired?.Invoke(this);
            Destroy(gameObject);
        }
    }
}