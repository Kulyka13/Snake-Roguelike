using System.Collections.Generic;
using UnityEngine;

public class SnakeBody : MonoBehaviour
{
    [Header("Налаштування сегментів та візуалу")]
    public GameObject segmentPrefab;
    public float segmentSpacing = 0.9f;
    public Sprite normalHeadSprite;
    public Sprite coolStyleHeadSprite;
    public Sprite inversionHeadSprite;

    public int SegmentCount => _segments.Count;
    public List<Vector2Int> PositionHistory => _positionHistory;

    private readonly List<Transform> _segments = new List<Transform>();
    private readonly List<Vector2Int> _positionHistory = new List<Vector2Int>();

    private float _gridSize = 1f;
    private float _baseSegmentSpacing;
    private SpriteRenderer _headSpriteRenderer;
    private SnakeEffectsManager _effects;
    private bool _wasGhost = false;

    public void Init(float gridSize, Vector2Int startGridPos, SnakeEffectsManager effects)
    {
        _gridSize = gridSize;
        _baseSegmentSpacing = segmentSpacing;
        _effects = effects;
        _headSpriteRenderer = GetComponent<SpriteRenderer>();

        if (_headSpriteRenderer != null && normalHeadSprite == null)
        {
            normalHeadSprite = _headSpriteRenderer.sprite;
        }

        _positionHistory.Clear();
        _positionHistory.Add(startGridPos);
    }

    public void RecordHeadStep(Vector2Int newGridPos)
    {
        _positionHistory.Insert(0, newGridPos);

        int maxNeededHistory = Mathf.CeilToInt((_segments.Count + 1) * segmentSpacing) + 3;
        while (_positionHistory.Count > maxNeededHistory)
        {
            _positionHistory.RemoveAt(_positionHistory.Count - 1);
        }
    }

    public bool CheckSelfCollision(Vector2Int targetPosition)
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

    public List<Vector2Int> GetOccupiedGridPositions(Vector2Int headPos)
    {
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int> { headPos };

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

    public void SmoothVisuals(Vector2Int currentGridPos, float stepProgress)
    {
        float t = Mathf.Clamp01(stepProgress);
        Vector3 targetHeadWorld = new Vector3(currentGridPos.x * _gridSize, currentGridPos.y * _gridSize, 0f);

        if (_positionHistory.Count > 1)
        {
            Vector2Int prevGrid = _positionHistory[1];
            bool wasTeleport = Mathf.Abs(currentGridPos.x - prevGrid.x) > 1 || Mathf.Abs(currentGridPos.y - prevGrid.y) > 1;
            Vector3 prevHeadWorld = wasTeleport
                ? targetHeadWorld
                : new Vector3(prevGrid.x * _gridSize, prevGrid.y * _gridSize, 0f);

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

            if (Vector3.Distance(prevPos, targetPos) > _gridSize * 1.5f)
                _segments[i].position = targetPos;
            else
                _segments[i].position = Vector3.Lerp(prevPos, targetPos, t);
        }
    }

    public void UpdateGhostVisuals()
    {
        if (_effects == null) return;

        UpdateHeadSprite();

        if (_effects.HasGhost)
        {
            _wasGhost = true;
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
        else if (_wasGhost)
        {
            _wasGhost = false;
            SetSnakeAlpha(1f);
        }
    }

    public void SetCoolStyle(bool active)
    {
        if (_effects != null) _effects.hasCoolStyle = active;
        UpdateHeadSprite();
    }
    private void UpdateHeadSprite()
    {
        if (_headSpriteRenderer == null) return;

        Sprite targetSprite = normalHeadSprite;

        if (_effects != null && _effects.HasInversion && inversionHeadSprite != null)
        {
            targetSprite = inversionHeadSprite;
        }
        else if (_effects != null && _effects.hasCoolStyle && coolStyleHeadSprite != null)
        {
            targetSprite = coolStyleHeadSprite;
        }

        if (targetSprite != null && _headSpriteRenderer.sprite != targetSprite)
        {
            _headSpriteRenderer.sprite = targetSprite;
        }
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
                SpriteFadeOut.PlayAndDestroy(_segments[lastIndex].gameObject, 0.25f);
            }
            _segments.RemoveAt(lastIndex);
        }
    }

    public void ReverseBody(ref Vector2Int gridPos, ref Vector2Int gridDir, ref Vector2Int inputDir)
    {
        if (_segments.Count == 0 || _positionHistory.Count < 2)
        {
            gridDir = -gridDir;
            inputDir = gridDir;
            float singleAngle = Mathf.Atan2(gridDir.y, gridDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, singleAngle);
            return;
        }

        int bodyCellsCount = Mathf.CeilToInt(_segments.Count * segmentSpacing) + 1;
        if (_positionHistory.Count > bodyCellsCount)
        {
            _positionHistory.RemoveRange(bodyCellsCount, _positionHistory.Count - bodyCellsCount);
        }

        _positionHistory.Reverse();

        gridPos = _positionHistory[0];
        transform.position = new Vector3(gridPos.x * _gridSize, gridPos.y * _gridSize, 0f);

        Vector2Int newDir = _positionHistory[0] - _positionHistory[1];
        if (newDir == Vector2Int.zero) newDir = -gridDir;

        gridDir = newDir;
        inputDir = newDir;

        float angle = Mathf.Atan2(gridDir.y, gridDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        for (int i = 0; i < _segments.Count; i++)
        {
            _segments[i].position = GetPositionAtStepOffset((i + 1) * segmentSpacing);
        }
    }

    public void HideAll()
    {
        if (_headSpriteRenderer != null) _headSpriteRenderer.enabled = false;

        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null)
            {
                SpriteFadeOut.PlayAndDestroy(_segments[i].gameObject, 0.3f);
            }
        }
        _segments.Clear();
    }

    public void ResetBody(Vector2Int startGridPos)
    {
        segmentSpacing = _baseSegmentSpacing;
        ClearSegments();
        SetSnakeAlpha(1f);
        SetCoolStyle(false);

        if (_headSpriteRenderer != null) _headSpriteRenderer.enabled = true;

        _positionHistory.Clear();
        _positionHistory.Add(startGridPos);
    }

    private void ClearSegments()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null) Destroy(_segments[i].gameObject);
        }
        _segments.Clear();
    }

    private Vector3 GetPositionAtStepOffset(float stepOffset)
    {
        if (_positionHistory.Count == 0) return transform.position;

        int baseIndex = Mathf.FloorToInt(stepOffset);
        float fraction = stepOffset - baseIndex;

        if (baseIndex >= _positionHistory.Count - 1)
        {
            var lastPos = _positionHistory[_positionHistory.Count - 1];
            return new Vector3(lastPos.x * _gridSize, lastPos.y * _gridSize, 0f);
        }

        Vector2Int gridA = _positionHistory[baseIndex];
        Vector2Int gridB = _positionHistory[baseIndex + 1];

        if (Mathf.Abs(gridA.x - gridB.x) > 1 || Mathf.Abs(gridA.y - gridB.y) > 1)
        {
            return new Vector3(gridA.x * _gridSize, gridA.y * _gridSize, 0f);
        }

        Vector3 posA = new Vector3(gridA.x * _gridSize, gridA.y * _gridSize, 0f);
        Vector3 posB = new Vector3(gridB.x * _gridSize, gridB.y * _gridSize, 0f);

        return Vector3.Lerp(posA, posB, fraction);
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
                var fadeIn = _segments[i].GetComponent<SpriteFadeIn>();
                if (fadeIn != null)
                {
                    fadeIn.targetAlpha = alpha;
                }

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