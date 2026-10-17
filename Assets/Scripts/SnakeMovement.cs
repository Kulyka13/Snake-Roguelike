using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SnakeMovement : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 6f;
    public float turnSpeed = 220f;
    public float segmentSpacing = 0.5f; 
    [Header("References")]
    public List<SnakeSegment> segments = new List<SnakeSegment>();

    private LinkedList<PathPoint> _pathHistory = new LinkedList<PathPoint>();
    private float _totalPathLength = 0f;
    private Vector2 _lastRecordedPos;

    private const float MinRecordDistance = 0.05f;

    void Start()
    {
        _lastRecordedPos = transform.position;
        _pathHistory.AddFirst(new PathPoint(transform.position, transform.rotation, 0f));
    }

    void Update()
    {
        HandleSteering();
        MoveForward();
        RecordPath();
        UpdateSegments();
        TrimHistory();
    }

    private void HandleSteering()
    {
        float horizontal = 0f;

        // Перевіряємо клавіатуру через новий Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }
        }

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            transform.Rotate(Vector3.forward, -horizontal * turnSpeed * Time.deltaTime);
        }
    }

    private void MoveForward()
    {
        transform.position += transform.up * (moveSpeed * Time.deltaTime);
    }

    private void RecordPath(bool isTeleport = false)
    {
        float dist = Vector2.Distance(transform.position, _lastRecordedPos);

        if (dist >= MinRecordDistance || isTeleport)
        {
            _pathHistory.AddFirst(new PathPoint(transform.position, transform.rotation, dist, isTeleport));
            _totalPathLength += dist;
            _lastRecordedPos = transform.position;
        }
    }

    private void UpdateSegments()
    {
        if (segments.Count == 0) return;

        float targetDistance = 0f;

        for (int i = 0; i < segments.Count; i++)
        {
            // Кожен сегмент може мати свій кастомний Spacing з модифікаторів
            targetDistance += segments[i].customSpacing > 0 ? segments[i].customSpacing : segmentSpacing;

            // Інтерполюємо позицію вздовж історії
            GetPointAtDistance(targetDistance, out Vector2 targetPos, out Quaternion targetRot);

            segments[i].transform.position = targetPos;
            segments[i].transform.rotation = targetRot;
        }
    }

    // Алгоритм пошуку точки на точній дистанції назад по траєкторії
    private void GetPointAtDistance(float targetDist, out Vector2 position, out Quaternion rotation)
    {
        // Якщо запитувана відстань ще не пройдена головою — беремо поточну точку хвоста
        if (_pathHistory.Count == 0)
        {
            position = transform.position;
            rotation = transform.rotation;
            return;
        }

        float accumulatedDist = 0f;
        var node = _pathHistory.First;

        // Враховуємо шлях від поточної позиції голови до найпершої записаної точки
        float distToFirst = Vector2.Distance(transform.position, node.Value.Position);
        if (targetDist <= distToFirst)
        {
            float t = targetDist / distToFirst;
            position = Vector2.Lerp(transform.position, node.Value.Position, t);
            rotation = Quaternion.Lerp(transform.rotation, node.Value.Rotation, t);
            return;
        }

        accumulatedDist += distToFirst;

        // Ітеруємося по вузлах історії
        while (node.Next != null)
        {
            var current = node.Value;
            var next = node.Next.Value;
            float segLength = next.DistanceFromLast;

            if (accumulatedDist + segLength >= targetDist)
            {
                float remain = targetDist - accumulatedDist;
                float t = remain / segLength;

                // Якщо між точками був телепорт — не робимо Lerp через усю мапу!
                if (next.IsTeleport)
                {
                    position = current.Position;
                    rotation = current.Rotation;
                }
                else
                {
                    position = Vector2.Lerp(current.Position, next.Position, t);
                    rotation = Quaternion.Lerp(current.Rotation, next.Rotation, t);
                }
                return;
            }

            accumulatedDist += segLength;
            node = node.Next;
        }

        // Якщо буфер закінчився — віддаємо останню доступну точку
        position = node.Value.Position;
        rotation = node.Value.Rotation;
    }

    private void TrimHistory()
    {
        // Потрібна довжина історії = сума відстаней усіх сегментів + невеликий запас
        float maxNeededDistance = (segments.Count + 2) * segmentSpacing;

        while (_totalPathLength > maxNeededDistance && _pathHistory.Last != null && _pathHistory.Count > 2)
        {
            _totalPathLength -= _pathHistory.Last.Value.DistanceFromLast;
            _pathHistory.RemoveLast();
        }
    }

    // Викликати цей метод, якщо підібрав фрукт-телепорт або стрибнув через портал
    public void TeleportTo(Vector2 newPosition)
    {
        transform.position = newPosition;
        _lastRecordedPos = newPosition;
        RecordPath(isTeleport: true);
    }
}