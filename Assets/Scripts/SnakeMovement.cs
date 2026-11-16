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

    [Header("Arena Bounds")]
    public Vector2Int arenaMinBounds = new Vector2Int(-10, -5);
    public Vector2Int arenaMaxBounds = new Vector2Int(10, 5);

    [Header("Segments & Cool Style")]
    public GameObject segmentPrefab;
    public Sprite normalHeadSprite;
    public Sprite coolStyleHeadSprite;
    private readonly List<Transform> _segments = new List<Transform>();

    [Header("Food / Spawning & Custom Sprites")]
    public Food food;
    public GameObject bonusApplePrefab;
    public Sprite explosionAppleSprite;
    public Sprite rottenAppleSprite;
    public Sprite portalASprite;
    public Sprite portalBSprite;

    private readonly List<BonusApple> _bonusApples = new List<BonusApple>();
    private readonly List<PortalApplePair> _portalPairs = new List<PortalApplePair>();

    [Header("Mutagen Settings")]
    public MutagenFood mutagenFood;
    public int applesPerMutagen = 3;
    private int _applesEatenCount = 0;

    private bool _isDead = false;

    private Vector2Int _gridPosition;
    private Vector2Int _gridDirection = Vector2Int.right;
    private Vector2Int _inputDirection = Vector2Int.right;

    private readonly List<Vector2Int> _positionHistory = new List<Vector2Int>();

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

        if (food != null)
        {
            food.Init(gridSize);
            food.Respawn(GetOccupiedGridPositions());
        }
        if (mutagenFood != null)
        {
            mutagenFood.Init(gridSize);
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

        if (_effects != null)
        {
            if (_effects.hasRottenApples && _effects.rottenSpawnTimer <= 0f)
            {
                _effects.rottenSpawnTimer = _effects.rottenSpawnInterval;
                SpawnRottenApple();
            }

            if (_effects.hasPortalApples && _effects.portalSpawnTimer <= 0f)
            {
                _effects.portalSpawnTimer = _effects.portalSpawnInterval;
                SpawnPortalPair();
            }
        }
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

        // Перевірка входу в яблука-портали
        for (int i = _portalPairs.Count - 1; i >= 0; i--)
        {
            PortalApplePair pair = _portalPairs[i];
            if (pair == null)
            {
                _portalPairs.RemoveAt(i);
                continue;
            }

            if (!pair.isTraversing)
            {
                if (_gridPosition == pair.portalAPos)
                {
                    _gridPosition = pair.portalBPos;
                    AddSegment();
                    pair.BeginTraversal();
                    break;
                }
                else if (_gridPosition == pair.portalBPos)
                {
                    _gridPosition = pair.portalAPos;
                    AddSegment();
                    pair.BeginTraversal();
                    break;
                }
            }
        }

        // 1. Звичайне яблуко
        if (food != null && IsWithinPickupRange(_gridPosition, food.gridPosition))
        {
            EatFood();
        }

        // 2. Мутагенне яблуко
        if (mutagenFood != null && mutagenFood.isActive && IsWithinPickupRange(_gridPosition, mutagenFood.gridPosition))
        {
            mutagenFood.Consume(this);
        }

        // 3. Бонусні міні-яблука та гнилі яблука
        for (int i = _bonusApples.Count - 1; i >= 0; i--)
        {
            BonusApple bApple = _bonusApples[i];
            if (bApple == null)
            {
                _bonusApples.RemoveAt(i);
                continue;
            }

            if (IsWithinPickupRange(_gridPosition, bApple.gridPosition))
            {
                _bonusApples.RemoveAt(i);
                if (bApple.isRotten)
                {
                    SetCoolStyle(false);
                    if (_segments.Count < 2)
                    {
                        Destroy(bApple.gameObject);
                        Die();
                        return;
                    }
                    RemoveLastSegments(2);
                    Debug.Log("З'їдено гниле яблуко! Втрачено 2 сегменти та окуляри.");
                }
                else
                {
                    AddSegment();
                }
                Destroy(bApple.gameObject);
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

        // Закриваємо пройдені портали, щойно останній сегмент вийшов із них
        for (int i = _portalPairs.Count - 1; i >= 0; i--)
        {
            PortalApplePair pair = _portalPairs[i];
            if (pair != null && pair.isTraversing && !IsSnakeOccupyingPortal(pair))
            {
                pair.CleanupAndDestroy();
            }
        }

        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private bool IsSnakeOccupyingPortal(PortalApplePair pair)
    {
        if (_gridPosition == pair.portalAPos || _gridPosition == pair.portalBPos)
            return true;

        int maxIndex = Mathf.Min(_positionHistory.Count - 1, Mathf.CeilToInt(_segments.Count * segmentSpacing));
        for (int i = 0; i <= maxIndex; i++)
        {
            Vector2Int pos = _positionHistory[i];
            if (pos == pair.portalAPos || pos == pair.portalBPos)
                return true;
        }

        return false;
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

    private bool IsWithinPickupRange(Vector2Int headPos, Vector2Int itemPos)
    {
        if (headPos == itemPos) return true;

        if (_effects != null && _effects.HasMagnet)
        {
            int distX = Mathf.Abs(headPos.x - itemPos.x);
            int distY = Mathf.Abs(headPos.y - itemPos.y);
            return Mathf.Max(distX, distY) <= _effects.magnetRadius;
        }

        return false;
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
        _applesEatenCount = 0;
        SetSnakeAlpha(1f);
        SetCoolStyle(false);

        if (_effects != null) _effects.ResetAllEffects();
        if (mutagenFood != null) mutagenFood.Hide();

        for (int i = 0; i < _bonusApples.Count; i++)
        {
            if (_bonusApples[i] != null) Destroy(_bonusApples[i].gameObject);
        }
        _bonusApples.Clear();

        for (int i = _portalPairs.Count - 1; i >= 0; i--)
        {
            if (_portalPairs[i] != null) _portalPairs[i].CleanupAndDestroy();
        }
        _portalPairs.Clear();

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

        if (food != null)
        {
            food.Respawn(GetOccupiedGridPositions());
        }
    }

    private void EatFood()
    {
        Vector2Int eatenPos = food.gridPosition;

        int baseGrowth = (_effects != null && _effects.hasCoolStyle) ? 2 : 1;
        int segmentsToAdd = (_effects != null && _effects.HasInversion)
            ? baseGrowth * _effects.inversionRewardMultiplier
            : baseGrowth;

        for (int i = 0; i < segmentsToAdd; i++)
        {
            AddSegment();
        }

        _applesEatenCount++;

        var occupied = GetAllOccupiedPositions();
        food.Respawn(occupied);

        if (_effects != null && _effects.hasAppleExplosion)
        {
            TriggerAppleExplosion(eatenPos, _effects.explosionAppleCount);
        }

        if (mutagenFood != null && !mutagenFood.isActive && _applesEatenCount % applesPerMutagen == 0)
        {
            occupied = GetAllOccupiedPositions();
            mutagenFood.Spawn(occupied);
        }
    }

    private List<Vector2Int> GetAllOccupiedPositions()
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>(GetOccupiedGridPositions());
        if (food != null) occupied.Add(food.gridPosition);
        if (mutagenFood != null && mutagenFood.isActive) occupied.Add(mutagenFood.gridPosition);

        foreach (var b in _bonusApples)
        {
            if (b != null) occupied.Add(b.gridPosition);
        }
        foreach (var p in _portalPairs)
        {
            if (p != null)
            {
                occupied.Add(p.portalAPos);
                occupied.Add(p.portalBPos);
            }
        }
        return new List<Vector2Int>(occupied);
    }

    private List<Vector2Int> GetOccupiedGridPositions()
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

    private void TriggerAppleExplosion(Vector2Int center, int count)
    {
        if (bonusApplePrefab == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions());

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = center.x - 2; x <= center.x + 2; x++)
        {
            for (int y = center.y - 2; y <= center.y + 2; y++)
            {
                if (x < arenaMinBounds.x || x > arenaMaxBounds.x || y < arenaMinBounds.y || y > arenaMaxBounds.y)
                    continue;

                Vector2Int pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }
        }

        int spawnCount = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            Vector2Int spawnPos = candidates[idx];
            candidates.RemoveAt(idx);

            GameObject obj = Instantiate(bonusApplePrefab, Vector3.zero, Quaternion.identity);
            var baseFood = obj.GetComponent<Food>();
            if (baseFood != null) baseFood.enabled = false;

            BonusApple bonus = obj.GetComponent<BonusApple>();
            if (bonus == null) bonus = obj.AddComponent<BonusApple>();

            bonus.Init(spawnPos, gridSize, _effects.explosionAppleDuration, false, explosionAppleSprite, b => _bonusApples.Remove(b));
            _bonusApples.Add(bonus);
        }
    }

    private void SpawnRottenApple()
    {
        if (bonusApplePrefab == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions());

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2Int pos = new Vector2Int(
                Random.Range(arenaMinBounds.x, arenaMaxBounds.x + 1),
                Random.Range(arenaMinBounds.y, arenaMaxBounds.y + 1)
            );

            if (!occupied.Contains(pos))
            {
                GameObject obj = Instantiate(bonusApplePrefab, Vector3.zero, Quaternion.identity);
                var baseFood = obj.GetComponent<Food>();
                if (baseFood != null) baseFood.enabled = false;

                BonusApple rotten = obj.GetComponent<BonusApple>();
                if (rotten == null) rotten = obj.AddComponent<BonusApple>();

                rotten.Init(pos, gridSize, _effects.rottenAppleDuration, true, rottenAppleSprite, b => _bonusApples.Remove(b));
                _bonusApples.Add(rotten);
                return;
            }
        }
    }

    private void SpawnPortalPair()
    {
        if (bonusApplePrefab == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions());
        List<Vector2Int> freeCells = new List<Vector2Int>();

        for (int x = arenaMinBounds.x; x <= arenaMaxBounds.x; x++)
        {
            for (int y = arenaMinBounds.y; y <= arenaMaxBounds.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos)) freeCells.Add(pos);
            }
        }

        if (freeCells.Count < 2) return;

        int idxA = Random.Range(0, freeCells.Count);
        Vector2Int posA = freeCells[idxA];
        freeCells.RemoveAt(idxA);

        int idxB = Random.Range(0, freeCells.Count);
        Vector2Int posB = freeCells[idxB];

        GameObject pairObj = new GameObject("PortalApplePair");
        PortalApplePair pair = pairObj.AddComponent<PortalApplePair>();
        pair.Init(posA, posB, bonusApplePrefab, portalASprite, portalBSprite, gridSize, _effects.portalDuration, p => _portalPairs.Remove(p));
        _portalPairs.Add(pair);
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