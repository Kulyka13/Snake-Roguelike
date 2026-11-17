using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SnakeEffectsManager))]
[RequireComponent(typeof(SnakeBody))]
public class SnakeMovement : MonoBehaviour
{
    [Header("Grid & Movement Settings")]
    public float gridSize = 1f;
    public float stepInterval = 0.2f;

    [Header("References")]
    public ArenaSpawner arenaSpawner;

    public int SegmentCount => _body != null ? _body.SegmentCount : 0;

    private bool _isDead = false;
    private Vector2Int _gridPosition;
    private Vector2Int _gridDirection = Vector2Int.right;
    private Vector2Int _inputDirection = Vector2Int.right;

    private float _stepProgress = 0f;
    private float _baseStepInterval;

    private SnakeEffectsManager _effects;
    private SnakeBody _body;

    void Start()
    {
        _baseStepInterval = stepInterval;
        _effects = GetComponent<SnakeEffectsManager>();
        _body = GetComponent<SnakeBody>();

        _gridPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x / gridSize),
            Mathf.RoundToInt(transform.position.y / gridSize)
        );
        transform.position = new Vector3(_gridPosition.x * gridSize, _gridPosition.y * gridSize, 0f);

        _body.Init(gridSize, _gridPosition, _effects);

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

        _body.SmoothVisuals(_gridPosition, _stepProgress);
        _body.UpdateGhostVisuals();
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

        bool hasSegments = _body.SegmentCount > 0;
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

        if (!isGhost && !isMidDash && _body.CheckSelfCollision(nextHeadPos))
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
                int segmentsToBite = Mathf.Max(1, _body.SegmentCount / 3);
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

        _body.RecordHeadStep(_gridPosition);

        if (arenaSpawner != null)
        {
            arenaSpawner.UpdateActivePortals(_gridPosition, _body.PositionHistory, _body.SegmentCount, _body.segmentSpacing);
        }

        float angle = Mathf.Atan2(_gridDirection.y, _gridDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Die()
    {
        _isDead = true;
        _body.HideAll();
        Debug.Log("Змійка загинула! Натисни 'R', щоб спробувати знову.");
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        stepInterval = _baseStepInterval;
        if (_effects != null) _effects.ResetAllEffects();

        _isDead = false;
        _gridPosition = Vector2Int.zero;
        transform.position = Vector3.zero;

        _gridDirection = Vector2Int.right;
        _inputDirection = Vector2Int.right;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        _stepProgress = 0f;

        _body.ResetBody(_gridPosition);

        if (arenaSpawner != null)
        {
            arenaSpawner.ResetArena(GetOccupiedGridPositions());
        }
    }

    // Публічні методи-обгортки для сумісності зі SnakeModifier та ArenaSpawner
    public void AddSegment() => _body.AddSegment();
    public void RemoveLastSegments(int count) => _body.RemoveLastSegments(count);
    public void SetCoolStyle(bool active) => _body.SetCoolStyle(active);
    public List<Vector2Int> GetOccupiedGridPositions() => _body.GetOccupiedGridPositions(_gridPosition);

    public void ReverseSnake()
    {
        _stepProgress = 0f;
        _body.ReverseBody(ref _gridPosition, ref _gridDirection, ref _inputDirection);
        Debug.Log("Реверс! Хвіст став головою.");
    }
}