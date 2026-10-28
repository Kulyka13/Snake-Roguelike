using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Потрібно для перезавантаження сцени

public class SnakeMovement : MonoBehaviour
{
    [Header("Grid & Movement Settings")]
    [Tooltip("Розмір однієї клітинки в юнітах Unity")]
    public float gridSize = 1f;

    [Tooltip("Час між кроками в секундах (менше = швидше)")]
    public float stepInterval = 0.2f;

    [Tooltip("Відстань між сегментами в клітинках")]
    [Range(0.2f, 5f)]
    public float segmentSpacing = 1f;

    [Header("Segments")]
    public GameObject segmentPrefab;
    private readonly List<Transform> _segments = new List<Transform>();

    [Header("Food / Spawning")]
    public Food food;

    // Стан гри
    private bool _isDead = false;

    // Логічні координати на сітці
    private Vector2Int _gridPosition;
    private Vector2Int _gridDirection = Vector2Int.right;
    private Vector2Int _inputDirection = Vector2Int.right;

    // Історія позицій для сегментів
    private readonly List<Vector2Int> _positionHistory = new List<Vector2Int>();

    private float _stepTimer;

    void Start()
    {
        _gridPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / gridSize),
            Mathf.RoundToInt(transform.position.y / gridSize)
        );
        transform.position = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);

        _positionHistory.Add(_gridPosition);

        if (food != null)
        {
            food.Init(gridSize);
            food.Respawn(GetOccupiedGridPositions());
        }
    }

    void Update()
    {
        if (_isDead)
        {
            // Після смерті перезапуск на клавішу R
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        HandleInput();

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= stepInterval)
        {
            _stepTimer = 0f;
            PerformStep();
        }

        SmoothVisuals();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        bool hasSegments = _segments.Count > 0;

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.down)
                _inputDirection = Vector2Int.up;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.up)
                _inputDirection = Vector2Int.down;
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.right)
                _inputDirection = Vector2Int.left;
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.left)
                _inputDirection = Vector2Int.right;
        }
    }

    private void PerformStep()
    {
        _gridDirection = _inputDirection;
        Vector2Int nextHeadPos = _gridPosition + _gridDirection;

        // ПЕРЕВІРКА НА ВРІЗАННЯ У ВЛАСНЕ ТІЛО
        if (CheckSelfCollision(nextHeadPos))
        {
            Die();
            return;
        }

        // Крок голови на сітці
        _gridPosition = nextHeadPos;

        // Перевірка збору яблука
        if (food != null && _gridPosition == food.gridPosition)
        {
            EatFood();
        }

        // Записуємо нову позицію голови
        _positionHistory.Insert(0, _gridPosition);

        // Обрізка історії
        int maxNeededHistory = Mathf.CeilToInt((_segments.Count + 1) * segmentSpacing) + 3;
        while (_positionHistory.Count > maxNeededHistory)
        {
            _positionHistory.RemoveAt(_positionHistory.Count - 1);
        }

        // Поворот спрайта голови
        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private bool CheckSelfCollision(Vector2Int targetPosition)
    {
        // Змійка не може врізатися в себе, якщо тіло занадто коротке (менше 4 клітинок дистанції)
        if (_segments.Count < 3) return false;

        // Перевіряємо сегменти починаючи з 3-го, бо перші два чисто фізично не можуть перетнутися з головою
        for (int i = 2; i < _segments.Count; i++)
        {
            float offset = (i + 1) * segmentSpacing;
            int idxA = Mathf.FloorToInt(offset);
            int idxB = Mathf.CeilToInt(offset);

            if (idxA < _positionHistory.Count && _positionHistory[idxA] == targetPosition)
                return true;

            if (idxB < _positionHistory.Count && _positionHistory[idxB] == targetPosition)
                return true;
        }

        return false;
    }

    private void Die()
    {
        _isDead = true;

        // 1. Ховаємо спрайт голови, щоб візуально вона зникла
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // 2. Знищуємо всі сегменти хвоста зі сцени
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null)
            {
                Destroy(_segments[i].gameObject);
            }
        }
        _segments.Clear();

        Debug.Log("Змійка загинула! Натисни 'R', щоб спробувати знову.");
    }

    private void RestartGame()
    {
        _isDead = false;

        // 1. Повертаємо видимість голови
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        // 2. Скидаємо позицію голови в центр (або стартову клітинку)
        _gridPosition = Vector2Int.zero;
        transform.position = Vector3.zero;

        // 3. Скидаємо напрямок руху на стартовий (вправо)
        _gridDirection = Vector2Int.right;
        _inputDirection = Vector2Int.right;
        transform.rotation = Quaternion.Euler(0, 0, 0); // кут 0 для руху вправо

        // 4. Очищаємо історію та скидаємо таймер кроку
        _positionHistory.Clear();
        _positionHistory.Add(_gridPosition);
        _stepTimer = 0f;

        // 5. Переспавнюємо яблуко на вільне місце
        if (food != null)
        {
            food.Respawn(GetOccupiedGridPositions());
        }
    }

    private void EatFood()
    {
        AddSegment();
        food.Respawn(GetOccupiedGridPositions());
    }

    private List<Vector2Int> GetOccupiedGridPositions()
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int> { _gridPosition };

        for (int i = 0; i < _segments.Count; i++)
        {
            float offset = (i + 1) * segmentSpacing;
            int idxA = Mathf.FloorToInt(offset);
            int idxB = Mathf.CeilToInt(offset);

            if (idxA < _positionHistory.Count)
                occupied.Add(_positionHistory[idxA]);

            if (idxB < _positionHistory.Count)
                occupied.Add(_positionHistory[idxB]);
        }

        return new List<Vector2Int>(occupied);
    }

    private void SmoothVisuals()
    {
        float t = Mathf.Clamp01(_stepTimer / stepInterval);

        // Плавний рух голови
        Vector3 targetHeadWorld = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);
        Vector3 prevHeadWorld = _positionHistory.Count > 1
            ? new Vector3(_positionHistory[1].x * gridSize, _positionHistory[1].y * gridSize, 0f)
            : targetHeadWorld;

        transform.position = Vector3.Lerp(prevHeadWorld, targetHeadWorld, t);

        // Плавний рух сегментів
        for (int i = 0; i < _segments.Count; i++)
        {
            float targetOffset = (i + 1) * segmentSpacing;

            Vector3 targetPos = GetPositionAtStepOffset(targetOffset);
            Vector3 prevPos = GetPositionAtStepOffset(targetOffset + 1f);

            _segments[i].position = Vector3.Lerp(prevPos, targetPos, t);
        }
    }

    private Vector3 GetPositionAtStepOffset(float stepOffset)
    {
        if (_positionHistory.Count == 0) return transform.position;

        int baseIndex = Mathf.FloorToInt(stepOffset);
        float fraction = stepOffset - baseIndex;

        if (baseIndex >= _positionHistory.Count - 1)
        {
            var lastPos = _positionHistory[_positionHistory.Count - 1];
            return new Vector3(lastPos.x * gridSize, lastPos.y * gridSize, 0f);
        }

        Vector3 posA = new Vector3(_positionHistory[baseIndex].x * gridSize, _positionHistory[baseIndex].y * gridSize, 0f);
        Vector3 posB = new Vector3(_positionHistory[baseIndex + 1].x * gridSize, _positionHistory[baseIndex + 1].y * gridSize, 0f);

        return Vector3.Lerp(posA, posB, fraction);
    }

    public void AddSegment()
    {
        float targetOffset = (_segments.Count + 1) * segmentSpacing;
        Vector3 spawnWorldPos = GetPositionAtStepOffset(targetOffset);

        GameObject newSeg = Instantiate(segmentPrefab, spawnWorldPos, Quaternion.identity);
        _segments.Add(newSeg.transform);
    }
}