using System.Collections.Generic;
using UnityEngine;

public class ArenaSpawner : MonoBehaviour
{
    [Header("Межі арени")]
    public Vector2Int arenaMinBounds = new Vector2Int(-10, -5);
    public Vector2Int arenaMaxBounds = new Vector2Int(10, 5);

    [Header("Основна їжа та мутаген")]
    public Food food;
    public MutagenFood mutagenFood;
    public int applesPerMutagen = 3;

    [Header("Бонусні об'єкти та кастомні спрайти")]
    public GameObject bonusApplePrefab;
    public Sprite explosionAppleSprite;
    public Sprite rottenAppleSprite;
    public Sprite portalASprite;
    public Sprite portalBSprite;

    private readonly List<BonusApple> _bonusApples = new List<BonusApple>();
    private readonly List<PortalApplePair> _portalPairs = new List<PortalApplePair>();

    private float _gridSize = 1f;
    private int _applesEatenCount = 0;
    private SnakeEffectsManager _effects;

    public void Init(float gridSize, SnakeEffectsManager effects, List<Vector2Int> initialSnakePositions)
    {
        _gridSize = gridSize;
        _effects = effects;

        if (food != null)
        {
            food.Init(gridSize);
            food.Respawn(initialSnakePositions);
        }
        if (mutagenFood != null)
        {
            mutagenFood.Init(gridSize);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f || _effects == null) return;

        if (_effects.hasRottenApples && _effects.rottenSpawnTimer <= 0f)
        {
            _effects.rottenSpawnTimer = _effects.rottenSpawnInterval;
            SpawnRottenApple();
        }

        if (_effects.hasPortalApples)
        {
            if (_portalPairs.Count > 0)
            {
                _effects.portalSpawnTimer = _effects.portalSpawnInterval;
            }
            else if (_effects.portalSpawnTimer <= 0f)
            {
                _effects.portalSpawnTimer = _effects.portalSpawnInterval;
                SpawnPortalPair();
            }
        }
    }

    // Перевіряє вхід голови в портал; якщо увійшла — повертає координати виходу
    public Vector2Int? TryEnterPortal(Vector2Int headPos)
    {
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
                if (headPos == pair.portalAPos)
                {
                    pair.BeginTraversal();
                    return pair.portalBPos;
                }
                if (headPos == pair.portalBPos)
                {
                    pair.BeginTraversal();
                    return pair.portalAPos;
                }
            }
        }
        return null;
    }

    // Закриває портали, з яких уже повністю вийшов хвіст змійки
    public void UpdateActivePortals(Vector2Int headPos, List<Vector2Int> positionHistory, int segmentCount, float segmentSpacing)
    {
        for (int i = _portalPairs.Count - 1; i >= 0; i--)
        {
            PortalApplePair pair = _portalPairs[i];
            if (pair != null && pair.isTraversing && !IsSnakeOccupyingPortal(pair, headPos, positionHistory, segmentCount, segmentSpacing))
            {
                pair.CleanupAndDestroy();
            }
        }
    }

    private bool IsSnakeOccupyingPortal(PortalApplePair pair, Vector2Int headPos, List<Vector2Int> history, int segmentCount, float spacing)
    {
        if (headPos == pair.portalAPos || headPos == pair.portalBPos)
            return true;

        int maxIndex = Mathf.Min(history.Count - 1, Mathf.CeilToInt(segmentCount * spacing));
        for (int i = 0; i <= maxIndex; i++)
        {
            if (history[i] == pair.portalAPos || history[i] == pair.portalBPos)
                return true;
        }
        return false;
    }

    // Обробляє підбір усіх яблук. Повертає false, якщо гниле яблуко вбило змійку
    public bool ProcessPickups(SnakeMovement snake, Vector2Int headPos)
    {
        // 1. Звичайне яблуко
        if (food != null && IsWithinPickupRange(headPos, food.gridPosition))
        {
            EatRegularFood(snake);
        }

        // 2. Мутагенне яблуко
        if (mutagenFood != null && mutagenFood.isActive && IsWithinPickupRange(headPos, mutagenFood.gridPosition))
        {
            mutagenFood.Consume(snake);
        }

        // 3. Міні-яблука та гнилі яблука
        for (int i = _bonusApples.Count - 1; i >= 0; i--)
        {
            BonusApple bApple = _bonusApples[i];
            if (bApple == null)
            {
                _bonusApples.RemoveAt(i);
                continue;
            }

            if (IsWithinPickupRange(headPos, bApple.gridPosition))
            {
                _bonusApples.RemoveAt(i);
                if (bApple.isRotten)
                {
                    snake.SetCoolStyle(false);
                    if (snake.SegmentCount < 2)
                    {
                        SpriteFadeOut.PlayAndDestroy(bApple.gameObject, 0.15f);
                        return false; // Сигнал смерті
                    }
                    snake.RemoveLastSegments(2);
                    Debug.Log("З'їдено гниле яблуко! Втрачено 2 сегменти та окуляри.");
                }
                else
                {
                    snake.AddSegment();
                }
                Destroy(bApple.gameObject);
            }
        }

        return true;
    }

    private void EatRegularFood(SnakeMovement snake)
    {
        Vector2Int eatenPos = food.gridPosition;

        int baseGrowth = (_effects != null && _effects.hasCoolStyle) ? 2 : 1;
        int segmentsToAdd = (_effects != null && _effects.HasInversion)
            ? baseGrowth * _effects.inversionRewardMultiplier
            : baseGrowth;

        for (int i = 0; i < segmentsToAdd; i++)
        {
            snake.AddSegment();
        }

        _applesEatenCount++;

        var occupied = GetAllOccupiedPositions(snake);
        food.Respawn(occupied);

        if (_effects != null && _effects.hasAppleExplosion)
        {
            TriggerAppleExplosion(snake, eatenPos, _effects.explosionAppleCount);
        }

        if (mutagenFood != null && !mutagenFood.isActive && _applesEatenCount % applesPerMutagen == 0)
        {
            occupied = GetAllOccupiedPositions(snake);
            mutagenFood.Spawn(occupied);
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

    private List<Vector2Int> GetAllOccupiedPositions(SnakeMovement snake)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>(snake.GetOccupiedGridPositions());
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

    private void TriggerAppleExplosion(SnakeMovement snake, Vector2Int center, int count)
    {
        if (bonusApplePrefab == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions(snake));
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = center.x - 2; x <= center.x + 2; x++)
        {
            for (int y = center.y - 2; y <= center.y + 2; y++)
            {
                if (x < arenaMinBounds.x || x > arenaMaxBounds.x || y < arenaMinBounds.y || y > arenaMaxBounds.y)
                    continue;

                Vector2Int pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos)) candidates.Add(pos);
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

            bonus.Init(spawnPos, _gridSize, _effects.explosionAppleDuration, false, explosionAppleSprite, b => _bonusApples.Remove(b));
            _bonusApples.Add(bonus);
        }
    }

    private void SpawnRottenApple()
    {
        var snake = FindFirstObjectByType<SnakeMovement>();
        if (bonusApplePrefab == null || snake == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions(snake));

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

                rotten.Init(pos, _gridSize, _effects.rottenAppleDuration, true, rottenAppleSprite, b => _bonusApples.Remove(b));
                _bonusApples.Add(rotten);
                return;
            }
        }
    }

    private void SpawnPortalPair()
    {
        var snake = FindFirstObjectByType<SnakeMovement>();
        if (bonusApplePrefab == null || snake == null) return;

        var occupied = new HashSet<Vector2Int>(GetAllOccupiedPositions(snake));
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
        pair.Init(posA, posB, bonusApplePrefab, portalASprite, portalBSprite, _gridSize, _effects.portalDuration, p => _portalPairs.Remove(p));
        _portalPairs.Add(pair);
    }

    public void ResetArena(List<Vector2Int> initialSnakePositions)
    {
        _applesEatenCount = 0;

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

        if (food != null)
        {
            food.Respawn(initialSnakePositions);
        }
    }
}