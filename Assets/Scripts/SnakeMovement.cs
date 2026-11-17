using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SnakeEffectsManager))]
public class SnakeMovement : MonoBehaviour
{
    [Header("Grid & Movement Settings")]
    public float gridSize = 1f;
    public float stepInterval = 0.2f;
    public float segmentSpacing = 0.9f;

    [Header("References")]
    public ArenaSpawner arenaSpawner;
    public GameObject segmentPrefab;
    public Sprite normalHeadSprite;
    public Sprite coolStyleHeadSprite;

    public int SegmentCount => _segments.Count;

    private readonly List<Transform> _segments = new List<Transform>();
    private readonly List<Vector2Int> _positionHistory = new List<Vector2Int>();

    private bool _isDead = false;
    private Vector2Int _gridPosition;
    private Vector2Int _gridDirection = Vector2Int.right;
    private Vector2Int _inputDirection = Vector2Int.right;

    private float _stepProgress = 0f;
    private float _baseStepInterval;
    private float _baseSegmentSpacing;
    private SpriteRenderer _headSpriteRenderer;
    private SnakeEffectsManager _effects;

    void Start()
    {
        _baseStepInterval = stepInterval;
        _baseSegmentSpacing = segmentSpacing;
        _headSpriteRenderer = GetComponent<SpriteRenderer>();
        _effects = GetComponent<SnakeEffectsManager>();

        if (_headSpriteRenderer != null && normalHeadSprite == null)
        {
            normalHeadSprite = _headSpriteRenderer.sprite;
        }

        _gridPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / gridSize),
            Mathf.RoundToInt(transform.position.y / gridSize)
        );
        transform.position = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);
        _positionHistory.Add(_gridPosition);

        if (arenaSpawner != null)
        {
            arenaSpawner.Init(gridSize, _effects, GetOccupiedGridPositions());
        }
    }

    void Update()
    {
        if (_isDead)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        if (Time.timeScale == 0f) return;

        HandleInput();

        float speedMult = _effects != null ? _effects.GetSpeedMultiplier() : 1f;
        _stepProgress += (Time.deltaTime / stepInterval) * speedMult;

        if (_stepProgress >= 1f)
        {
            _stepProgress -= 1f;
            PerformStep();
        }

        SmoothVisuals();
        UpdateGhostVisuals();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        if (_effects != null && _effects.CanDash &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame))
        {
            _effects.StartDash();
            return;
        }

        if (_effects != null && _effects.IsDashing) return;

        bool hasSegments = _segments.Count > 0;
        bool invert = _effects != null && _effects.HasInversion;

        Vector2Int desiredDir = _inputDirection;

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.down)
                desiredDir = Vector2Int.up;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (!hasSegments || _gridDirection != Vector2Int.up)
                desiredDir = Vector2Int.down;
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            Vector2Int target = invert ? Vector2Int.right : Vector2Int.left;
            if (!hasSegments || _gridDirection != -target)
                desiredDir = target;
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            Vector2Int target = invert ? Vector2Int.left : Vector2Int.right;
            if (!hasSegments || _gridDirection != -target)
                desiredDir = target;
        }

        if (desiredDir != _inputDirection)
        {
            _inputDirection = desiredDir;
            if (_effects != null) _effects.TriggerBulletTimeTurn();
        }
    }

    private void PerformStep()
    {
        _gridDirection = _inputDirection;
        Vector2Int nextHeadPos = _gridPosition + _gridDirection;

        bool isGhost = _effects != null && _effects.HasGhost;
        bool isMidDash = _effects != null && _effects.dashStepsRemaining > 1;

        if (!isGhost && !isMidDash && CheckSelfCollision(nextHeadPos))
        {
            if (_effects != null && _effects.TryAbsorbHit())
            {
                SetCoolStyle(false);
                _stepProgress = 0f;
                return;
            }

            if (_effects != null && _effects.TryConsumeUroboros())
            {
                SetCoolStyle(false);
                int segmentsToBite = Mathf.Max(1, _segments.Count / 3);
                RemoveLastSegments(segmentsToBite);
                Debug.Log($"Уроборос спрацював! Відкушено {segmentsToBite} сегментів.");
            }
            else
            {
                Die();
                return;
            }
        }

        _gridPosition = nextHeadPos;

        if (arenaSpawner != null)
        {
            Vector2Int? exitPortal = arenaSpawner.TryEnterPortal(_gridPosition);
            if (exitPortal.HasValue)
            {
                _gridPosition = exitPortal.Value;
                AddSegment();
            }

            if (!arenaSpawner.ProcessPickups(this, _gridPosition))
            {
                Die();
                return;
            }
        }

        if (_effects != null)
        {
            _effects.OnSnakeStep();
        }

        _positionHistory.Insert(0, _gridPosition);

        int maxNeededHistory = Mathf.CeilToInt((_segments.Count + 1) * segmentSpacing) + 3;
        while (_positionHistory.Count > maxNeededHistory)
        {
            _positionHistory.RemoveAt(_positionHistory.Count - 1);
        }

        if (arenaSpawner != null)
        {
            arenaSpawner.UpdateActivePortals(_gridPosition, _positionHistory, _segments.Count, segmentSpacing);
        }

        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetCoolStyle(bool active)
    {
        if (_effects != null) _effects.hasCoolStyle = active;

        if (_headSpriteRenderer != null)
        {
            if (active && coolStyleHeadSprite != null)
                _headSpriteRenderer.sprite = coolStyleHeadSprite;
            else if (!active && normalHeadSprite != null)
                _headSpriteRenderer.sprite = normalHeadSprite;
        }
    }

    private bool CheckSelfCollision(Vector2Int targetPosition)
    {
        if (_segments.Count < 3) return false;

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
        if (_headSpriteRenderer != null) _headSpriteRenderer.enabled = false;

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null) Destroy(_segments[i].gameObject);
        }
        _segments.Clear();

        Debug.Log("Змійка загинула! Натисни 'R', щоб спробувати знову.");
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        stepInterval = _baseStepInterval;
        segmentSpacing = _baseSegmentSpacing;
        SetSnakeAlpha(1f);
        SetCoolStyle(false);

        if (_effects != null) _effects.ResetAllEffects();

        _isDead = false;
        if (_headSpriteRenderer != null) _headSpriteRenderer.enabled = true;

        _gridPosition = Vector2Int.zero;
        transform.position = Vector3.zero;

        _gridDirection = Vector2Int.right;
        _inputDirection = Vector2Int.right;
        transform.rotation = Quaternion.Euler(0, 0, 0);

        _positionHistory.Clear();
        _positionHistory.Add(_gridPosition);
        _stepProgress = 0f;

        if (arenaSpawner != null)
        {
            arenaSpawner.ResetArena(GetOccupiedGridPositions());
        }
    }

    public List<Vector2Int> GetOccupiedGridPositions()
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int> { _gridPosition };

        for (int i = 0; i < _segments.Count; i++)
        {
            float offset = (i + 1) * segmentSpacing;
            int idxA = Mathf.FloorToInt(offset);
            int idxB = Mathf.CeilToInt(offset);

            if (idxA < _positionHistory.Count) occupied.Add(_positionHistory[idxA]);
            if (idxB < _positionHistory.Count) occupied.Add(_positionHistory[idxB]);
        }

        return new List<Vector2Int>(occupied);
    }

    private void SmoothVisuals()
    {
        float t = Mathf.Clamp01(_stepProgress);

        Vector3 targetHeadWorld = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);

        if (_positionHistory.Count > 1)
        {
            Vector2Int prevGrid = _positionHistory[1];
            bool wasTeleport = Mathf.Abs(_gridPosition.x - prevGrid.x) > 1 || Mathf.Abs(_gridPosition.y - prevGrid.y) > 1;
            Vector3 prevHeadWorld = wasTeleport
                ? targetHeadWorld
                : new Vector3(prevGrid.x * gridSize, prevGrid.y * gridSize, 0f);

            transform.position = Vector3.Lerp(prevHeadWorld, targetHeadWorld, t);
        }
        else
        {
            transform.position = targetHeadWorld;
        }

        for (int i = 0; i < _segments.Count; i++)
        {
            float targetOffset = (i + 1) * segmentSpacing;
            Vector3 targetPos = GetPositionAtStepOffset(targetOffset);
            Vector3 prevPos = GetPositionAtStepOffset(targetOffset + 1f);

            if (Vector3.Distance(prevPos, targetPos) > gridSize * 1.5f)
                _segments[i].position = targetPos;
            else
                _segments[i].position = Vector3.Lerp(prevPos, targetPos, t);
        }
    }

    private void UpdateGhostVisuals()
    {
        if (_effects == null) return;

        if (_effects.HasGhost)
        {
            if (_effects.ghostTime <= 2f)
            {
                float alpha = Mathf.Lerp(0.25f, 0.85f, Mathf.PingPong(Time.time * 8f, 1f));
                SetSnakeAlpha(alpha);
            }
            else
            {
                SetSnakeAlpha(0.5f);
            }
        }
        else if (_headSpriteRenderer != null && _headSpriteRenderer.color.a < 1f)
        {
            SetSnakeAlpha(1f);
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

        Vector2Int gridA = _positionHistory[baseIndex];
        Vector2Int gridB = _positionHistory[baseIndex + 1];

        if (Mathf.Abs(gridA.x - gridB.x) > 1 || Mathf.Abs(gridA.y - gridB.y) > 1)
        {
            return new Vector3(gridA.x * gridSize, gridA.y * gridSize, 0f);
        }

        Vector3 posA = new Vector3(gridA.x * gridSize, gridA.y * gridSize, 0f);
        Vector3 posB = new Vector3(gridB.x * gridSize, gridB.y * gridSize, 0f);

        return Vector3.Lerp(posA, posB, fraction);
    }

    public void AddSegment()
    {
        float targetOffset = (_segments.Count + 1) * segmentSpacing;
        Vector3 spawnWorldPos = GetPositionAtStepOffset(targetOffset);

        GameObject newSeg = Instantiate(segmentPrefab, spawnWorldPos, Quaternion.identity);

        float targetAlpha = (_effects != null && _effects.HasGhost) ? 0.5f : 1f;
        SpriteFadeIn.Play(newSeg, targetAlpha, 0.25f);

        _segments.Add(newSeg.transform);
    }

    public void RemoveLastSegments(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_segments.Count == 0) break;

            int lastIndex = _segments.Count - 1;
            if (_segments[lastIndex] != null)
            {
                Destroy(_segments[lastIndex].gameObject);
            }
            _segments.RemoveAt(lastIndex);
        }
    }

    public void ReverseSnake()
    {
        if (_segments.Count == 0 || _positionHistory.Count < 2)
        {
            _gridDirection = -_gridDirection;
            _inputDirection = _gridDirection;
            float singleAngle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, singleAngle);
            return;
        }

        int bodyCellsCount = Mathf.CeilToInt(_segments.Count * segmentSpacing) + 1;
        if (_positionHistory.Count > bodyCellsCount)
        {
            _positionHistory.RemoveRange(bodyCellsCount, _positionHistory.Count - bodyCellsCount);
        }

        _positionHistory.Reverse();

        _gridPosition = _positionHistory[0];
        transform.position = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);

        Vector2Int newDir = _positionHistory[0] - _positionHistory[1];
        if (newDir == Vector2Int.zero) newDir = -_gridDirection;

        _gridDirection = newDir;
        _inputDirection = newDir;
        _stepProgress = 0f;

        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        for (int i = 0; i < _segments.Count; i++)
        {
            _segments[i].position = GetPositionAtStepOffset((i + 1) * segmentSpacing);
        }

        Debug.Log("Реверс! Хвіст став головою.");
    }

    private void SetSnakeAlpha(float alpha)
    {
        if (_headSpriteRenderer != null)
        {
            Color c = _headSpriteRenderer.color;
            c.a = alpha;
            _headSpriteRenderer.color = c;
        }

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null)
            {
                var sr = _segments[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }
    }
}