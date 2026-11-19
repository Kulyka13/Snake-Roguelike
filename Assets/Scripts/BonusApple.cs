using System;
using UnityEngine;

public class BonusApple : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public bool isRotten = false;
    [HideInInspector] public bool canExplode = false;

    private float _lifeTime;
    private SpriteRenderer _sr;
    private Action<BonusApple> _onExpired;

    public void Init(Vector2Int pos, float gridSize, float duration, bool rotten, bool explodesOnEat, float scale, Sprite customSprite, Action<BonusApple> onExpired)
    {
        gridPosition = pos;
        isRotten = rotten;
        canExplode = explodesOnEat;
        _lifeTime = duration;
        _onExpired = onExpired;

        transform.position = new Vector3(pos.x * gridSize, pos.y * gridSize, 0f);
        transform.localScale = Vector3.one * scale;

        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            _sr.enabled = true;
            if (customSprite != null) _sr.sprite = customSprite;
            else if (rotten) _sr.color = new Color(0.55f, 0.35f, 0.25f, 1f);
        }

        SpriteFadeIn.Play(gameObject, 1f, 0.2f);
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
            enabled = false;
            SpriteFadeOut.PlayAndDestroy(gameObject, 0.25f);
        }
    }
}