using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SnakeMovement : MonoBehaviour
{
    [Header("Grid & Movement Settings")]
    [Tooltip("Розмір однієї клітинки в юнітах Unity")]
    public float gridSize = 1f;

    [Tooltip("Час між кроками в секундах (менше = швидше)")]
    public float stepInterval = 0.2f;

    [Tooltip("Відстань між сегментами в клітинках (можна десятковими, наприклад 0.8 чи 1.5)")]
    [Range(0.2f, 5f)]
    public float segmentSpacing = 1f;

    [Header("Segments")]
    public GameObject segmentPrefab;
    private readonly List<Transform> _segments = new List<Transform>();

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
    }

    void Update()
    {
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AddSegment();
        }

        // Якщо є сегменти — забороняємо розворот на 180°. Якщо сегментів 0 — можна повертати куди завгодно!
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

        // Крок голови на сітці
        _gridPosition += _gridDirection;

        // Записуємо нову позицію голови на початок історії
        _positionHistory.Insert(0, _gridPosition);

        // Обрізка історії
        int maxNeededHistory = Mathf.CeilToInt((_segments.Count + 1) * segmentSpacing) + 3;
        while (_positionHistory.Count > maxNeededHistory)
        {
            _positionHistory.RemoveAt(_positionHistory.Count - 1);
        }

        // Поворот спрайта голови:
        // Якщо твій спрайт спочатку дивиться ВГОРУ — використовуй: - 90f
        // Якщо твій спрайт спочатку дивиться ВПРАВО — просто прибери "- 90f"
        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
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

        // Плавний рух сегментів вздовж історії із дробовою відстанню
        for (int i = 0; i < _segments.Count; i++)
        {
            float targetOffset = (i + 1) * segmentSpacing;

            // Знаходимо цільову позицію та попередню (звідки сегмент рухається)
            Vector3 targetPos = GetPositionAtStepOffset(targetOffset);
            Vector3 prevPos = GetPositionAtStepOffset(targetOffset + 1f);

            _segments[i].position = Vector3.Lerp(prevPos, targetPos, t);
        }
    }

    // Допоміжний метод: вираховує координати в просторі для будь-якого дробового кроку назад в історії
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