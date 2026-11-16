using UnityEngine;

public enum ModifierType
{
    SpeedUp,
    SlowDown,
    DoubleGrowth,
    TrimTail,
    GhostMode,
    Magnet,
    Inversion,
    BulletTime,
    Shield,
    Uroboros,
    Reverse,
    Dash,
    AppleExplosion,
    RottenApples,
    CoolStyle,
    PortalApples
}

[CreateAssetMenu(fileName = "NewModifier", menuName = "Snake/Modifier")]
public class SnakeModifier : ScriptableObject
{
    public string modifierName;
    [TextArea] public string description;
    public ModifierType type;
    public float value;

    [Header("Правила випадання")]
    public bool isUnique = false;

    public bool CanBeOffered(SnakeMovement snake)
    {
        if (!isUnique) return true;

        var effects = snake.GetComponent<SnakeEffectsManager>();
        if (effects == null) return true;

        switch (type)
        {
            case ModifierType.Dash:
                return !effects.hasDashAbility;
            case ModifierType.Uroboros:
                return !effects.HasUroboros;
            case ModifierType.Shield:
                return !effects.HasShield;
            case ModifierType.BulletTime:
                return !effects.HasBulletTime;
            case ModifierType.AppleExplosion:
                return !effects.hasAppleExplosion;
            case ModifierType.RottenApples:
                return !effects.hasRottenApples;
            case ModifierType.CoolStyle:
                return !effects.hasCoolStyle;
            case ModifierType.PortalApples:
                return !effects.hasPortalApples;
            default:
                return true;
        }
    }

    public void Apply(SnakeMovement snake)
    {
        var effects = snake.GetComponent<SnakeEffectsManager>();

        switch (type)
        {
            case ModifierType.SpeedUp:
                snake.stepInterval = Mathf.Max(0.06f, snake.stepInterval - value);
                break;
            case ModifierType.SlowDown:
                snake.stepInterval += value;
                break;
            case ModifierType.DoubleGrowth:
                snake.AddSegment();
                snake.AddSegment();
                break;
            case ModifierType.TrimTail:
                snake.RemoveLastSegments(Mathf.RoundToInt(value));
                break;
            case ModifierType.GhostMode:
                if (effects != null) effects.ghostTime += value;
                break;
            case ModifierType.Magnet:
                if (effects != null) effects.magnetTime += value;
                break;
            case ModifierType.Inversion:
                if (effects != null) effects.inversionTime += value;
                break;
            case ModifierType.BulletTime:
                if (effects != null) effects.bulletTime += value;
                break;
            case ModifierType.Shield:
                if (effects != null) effects.shieldTime += value;
                break;
            case ModifierType.Uroboros:
                if (effects != null) effects.uroborosCharges = Mathf.Max(1, Mathf.RoundToInt(value));
                break;
            case ModifierType.Reverse:
                snake.ReverseSnake();
                break;
            case ModifierType.Dash:
                if (effects != null)
                {
                    effects.hasDashAbility = true;
                    effects.dashCooldownTimer = 0f;
                    if (value > 0) effects.dashDistance = Mathf.RoundToInt(value);
                }
                break;
            case ModifierType.AppleExplosion:
                if (effects != null)
                {
                    effects.hasAppleExplosion = true;
                    if (value > 0) effects.explosionAppleCount = Mathf.RoundToInt(value);
                }
                break;
            case ModifierType.RottenApples:
                if (effects != null)
                {
                    effects.hasRottenApples = true;
                    effects.rottenSpawnTimer = 2f;
                }
                break;
            case ModifierType.CoolStyle:
                snake.SetCoolStyle(true);
                break;
            case ModifierType.PortalApples:
                if (effects != null)
                {
                    effects.hasPortalApples = true;
                    effects.portalSpawnTimer = 1.5f;
                }
                break;
        }

        Debug.Log($"Активовано мутацію: {modifierName}");
    }
}