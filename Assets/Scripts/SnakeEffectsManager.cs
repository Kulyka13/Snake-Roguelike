using UnityEngine;

public class SnakeEffectsManager : MonoBehaviour
{
    [Header("Таймери ефектів (у секундах)")]
    public float magnetTime = 0f;
    public float inversionTime = 0f;
    public float bulletTime = 0f;
    public float shieldTime = 0f;
    public float ghostTime = 0f;

    [Header("Заряди та здібності")]
    public int uroborosCharges = 0;

    public bool hasDashAbility = false;
    public float dashCooldownTimer = 0f;
    public float maxDashCooldown = 3f;
    public int dashDistance = 3;
    public float dashSpeedMultiplier = 4.5f;

    [HideInInspector] public int dashStepsRemaining = 0;

    [Header("Етап 3: Модифікатори поля та стилю")]
    public bool hasAppleExplosion = false;
    public int explosionAppleCount = 3;
    public float explosionAppleDuration = 5f;

    public bool hasRottenApples = false;
    public float rottenSpawnInterval = 7f;
    public float rottenAppleDuration = 8f;
    [HideInInspector] public float rottenSpawnTimer = 0f;

    public bool hasCoolStyle = false;

    public bool hasPortalApples = false;
    public float portalSpawnInterval = 9f;
    public float portalDuration = 10f;
    [HideInInspector] public float portalSpawnTimer = 0f;

    [Header("Налаштування балансу")]
    public int magnetRadius = 2;
    public float shieldSpeedMultiplier = 0.85f;
    public float shieldBreakMinSpeed = 0.15f;
    public float shieldBreakSlowDuration = 1.2f;
    public float bulletTimeMinSpeed = 0.35f;
    public float bulletTimeRecoverySpeed = 1.5f;
    public int inversionRewardMultiplier = 3;

    private float _currentBulletSpeedFactor = 1f;
    private float _shieldBreakSlowTimer = 0f;

    public bool HasMagnet => magnetTime > 0f;
    public bool HasInversion => inversionTime > 0f;
    public bool HasBulletTime => bulletTime > 0f;
    public bool HasShield => shieldTime > 0f;
    public bool HasGhost => ghostTime > 0f;
    public bool HasUroboros => uroborosCharges > 0;
    public bool IsDashing => dashStepsRemaining > 0;
    public bool CanDash => hasDashAbility && dashCooldownTimer <= 0f && !IsDashing;

    void Update()
    {
        if (Time.timeScale == 0f) return;

        float dt = Time.deltaTime;
        if (magnetTime > 0f) magnetTime = Mathf.Max(0f, magnetTime - dt);
        if (inversionTime > 0f) inversionTime = Mathf.Max(0f, inversionTime - dt);
        if (bulletTime > 0f) bulletTime = Mathf.Max(0f, bulletTime - dt);
        if (shieldTime > 0f) shieldTime = Mathf.Max(0f, shieldTime - dt);
        if (ghostTime > 0f) ghostTime = Mathf.Max(0f, ghostTime - dt);
        if (dashCooldownTimer > 0f) dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - dt);

        if (hasRottenApples) rottenSpawnTimer -= dt;
        if (hasPortalApples) portalSpawnTimer -= dt;

        if (_shieldBreakSlowTimer > 0f)
        {
            _shieldBreakSlowTimer = Mathf.Max(0f, _shieldBreakSlowTimer - dt);
        }

        if (_currentBulletSpeedFactor < 1f)
        {
            _currentBulletSpeedFactor = Mathf.MoveTowards(_currentBulletSpeedFactor, 1f, bulletTimeRecoverySpeed * dt);
        }
    }

    public void StartDash()
    {
        dashStepsRemaining = dashDistance;
        dashCooldownTimer = maxDashCooldown;
    }

    public void TriggerBulletTimeTurn()
    {
        if (HasBulletTime)
        {
            _currentBulletSpeedFactor = bulletTimeMinSpeed;
        }
    }

    public float GetSpeedMultiplier()
    {
        if (IsDashing) return dashSpeedMultiplier;

        float mult = _currentBulletSpeedFactor;
        if (HasShield) mult *= shieldSpeedMultiplier;

        if (_shieldBreakSlowTimer > 0f)
        {
            float progress = 1f - (_shieldBreakSlowTimer / shieldBreakSlowDuration);
            float shieldBreakFactor = Mathf.Lerp(shieldBreakMinSpeed, 1f, progress);
            mult = Mathf.Min(mult, shieldBreakFactor);
        }

        return mult;
    }

    public void OnSnakeStep()
    {
        if (dashStepsRemaining > 0)
        {
            dashStepsRemaining--;
        }
    }

    public bool TryAbsorbHit()
    {
        if (HasShield)
        {
            shieldTime = 0f;
            _shieldBreakSlowTimer = shieldBreakSlowDuration;
            Debug.Log("Броньований панцир поглинув удар! Час сповільнено для маневру.");
            return true;
        }
        return false;
    }

    public bool TryConsumeUroboros()
    {
        if (HasUroboros)
        {
            uroborosCharges--;
            return true;
        }
        return false;
    }

    public void ResetAllEffects()
    {
        magnetTime = 0f;
        inversionTime = 0f;
        bulletTime = 0f;
        shieldTime = 0f;
        ghostTime = 0f;
        uroborosCharges = 0;
        hasDashAbility = false;
        dashCooldownTimer = 0f;
        dashStepsRemaining = 0;
        _currentBulletSpeedFactor = 1f;
        _shieldBreakSlowTimer = 0f;

        hasAppleExplosion = false;
        hasRottenApples = false;
        rottenSpawnTimer = 0f;
        hasCoolStyle = false;
        hasPortalApples = false;
        portalSpawnTimer = 0f;
    }
}