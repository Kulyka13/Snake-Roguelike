using NUnit.Framework;
using UnityEngine;

public class SnakeLogicTests
{
    private GameObject _testRoot;
    private ArenaSpawner _spawner;
    private SnakeBody _snakeBody;
    private SnakeEffectsManager _effects;

    [SetUp]
    public void SetUp()
    {
        _testRoot = new GameObject("TestSnakeRoot");

        _spawner = _testRoot.AddComponent<ArenaSpawner>();
        _spawner.arenaMinBounds = new Vector2Int(-10, -5);
        _spawner.arenaMaxBounds = new Vector2Int(10, 5);

        _effects = _testRoot.AddComponent<SnakeEffectsManager>();
        _snakeBody = _testRoot.AddComponent<SnakeBody>();
        _snakeBody.segmentSpacing = 1f;

        GameObject segPrefab = new GameObject("DummySegment");
        segPrefab.transform.SetParent(_testRoot.transform);
        _snakeBody.segmentPrefab = segPrefab;
        _snakeBody.Init(1f, Vector2Int.zero, _effects);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_testRoot);
    }

    // Тест 1 (Позитивний): телепортація через протилежні межі арени
    [Test]
    public void WrapPosition_WhenOutOfBounds_WrapsToOppositeSide()
    {
        Vector2Int wrappedRight = _spawner.WrapPosition(new Vector2Int(11, 2));
        Vector2Int wrappedBottom = _spawner.WrapPosition(new Vector2Int(-3, -6));

        Assert.AreEqual(new Vector2Int(-10, 2), wrappedRight);
        Assert.AreEqual(new Vector2Int(-3, 5), wrappedBottom);
    }

    // Тест 2 (Негативний / граничний): координати всередині меж не змінюються
    [Test]
    public void WrapPosition_WhenInsideBounds_RemainsUnchanged()
    {
        Vector2Int boundaryPos = new Vector2Int(10, -5);
        Vector2Int result = _spawner.WrapPosition(boundaryPos);

        Assert.AreEqual(boundaryPos, result);
    }

    // Тест 3 (Позитивний та негативний): детекція самоколізії з хвостом
    [Test]
    public void CheckSelfCollision_DetectsTailHitOnlyWhenLongEnough()
    {
        // Додаємо 4 сегменти та формуємо траєкторію кроку
        for (int i = 0; i < 4; i++) _snakeBody.AddSegment();

        _snakeBody.RecordHeadStep(new Vector2Int(1, 0));
        _snakeBody.RecordHeadStep(new Vector2Int(2, 0));
        _snakeBody.RecordHeadStep(new Vector2Int(2, 1));
        _snakeBody.RecordHeadStep(new Vector2Int(1, 1));

        // Позитивний сценарій: клітинка зайнята сегментом тіла
        Assert.IsTrue(_snakeBody.CheckSelfCollision(new Vector2Int(0, 0)));
        // Негативний сценарій: сусідня клітинка вільна
        Assert.IsFalse(_snakeBody.CheckSelfCollision(new Vector2Int(1, 2)));
    }

    // Тест 4 (Позитивний та негативний): робота зарядів щита "Міцний панцир"
    [Test]
    public void TryAbsorbHit_ConsumesShieldTimeAndReturnsFalseWhenInactive()
    {
        _effects.shieldTime = 5f;

        bool firstHitAbsorbed = _effects.TryAbsorbHit();
        bool secondHitAbsorbed = _effects.TryAbsorbHit();

        Assert.IsTrue(firstHitAbsorbed);
        Assert.AreEqual(0f, _effects.shieldTime);
        Assert.IsFalse(_effects.HasShield);
        Assert.IsFalse(secondHitAbsorbed);
    }

    // Тест 5 (Негативний): безпечне видалення більшої кількості сегментів, ніж існує
    [Test]
    public void RemoveLastSegments_WhenCountExceedsLength_ClampsToZeroWithoutException()
    {
        _snakeBody.AddSegment();
        _snakeBody.AddSegment();

        Assert.DoesNotThrow(() => _snakeBody.RemoveLastSegments(5));
        Assert.AreEqual(0, _snakeBody.SegmentCount);
    }
}