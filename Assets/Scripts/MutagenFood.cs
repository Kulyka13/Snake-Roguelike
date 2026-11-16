using System.Collections.Generic;
using UnityEngine;

public class MutagenFood : MonoBehaviour
{
    [Header("Налаштування спавну")]
    public Vector2Int minBounds = new Vector2Int(-10, -5);
    public Vector2Int maxBounds = new Vector2Int(10, 5);
    public float maxTimeAlive = 8f;

    [Header("Модифікатори та UI")]
    public List<SnakeModifier> availableModifiers;
    public ModifierSelectionUI selectionUI;

    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public bool isActive = false;

    private float _gridSize = 1f;
    private float _remainingTime = 0f;
    private SpriteRenderer _sr;

    public void Init(float gridSize)
    {
        _gridSize = gridSize;
        _sr = GetComponent<SpriteRenderer>();
        Hide();
    }

    void Update()
    {
        if (!isActive || Time.timeScale == 0f) return;

        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 2f && _sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
            _sr.color = c;
        }

        if (_remainingTime <= 0f)
        {
            Hide();
        }
    }

    public void Spawn(List<Vector2Int> occupiedPositions)
    {
        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>(occupiedPositions);

        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector2Int candidate = new Vector2Int(
                Random.Range(minBounds.x, maxBounds.x + 1),
                Random.Range(minBounds.y, maxBounds.y + 1)
            );

            if (!blocked.Contains(candidate))
            {
                gridPosition = candidate;
                transform.position = new Vector3(gridPosition.x * _gridSize, gridPosition.y * _gridSize, 0f);
                _remainingTime = maxTimeAlive;
                isActive = true;

                if (_sr != null)
                {
                    Color c = _sr.color;
                    c.a = 1f;
                    _sr.color = c;
                    _sr.enabled = true;
                }

                SpriteFadeIn.Play(gameObject, 1f, 0.3f);
                return;
            }
        }
    }

    public void OnSnakeStep() { }

    public void Consume(SnakeMovement snake)
    {
        if (!isActive) return;
        Hide();

        if (selectionUI != null && availableModifiers != null && availableModifiers.Count > 0)
        {
            selectionUI.ShowChoices(availableModifiers, snake);
        }
    }

    public void Hide()
    {
        isActive = false;
        _remainingTime = 0f;
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = 1f;
            _sr.color = c;
            _sr.enabled = false;
        }
    }
}