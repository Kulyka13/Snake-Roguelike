using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Налаштування очок")]
    public int regularApplePoints = 10;
    public int miniApplePoints = 5;
    public int mutagenPoints = 25;
    public int portalPoints = 15;

    [Header("UI (TextMeshPro)")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverSummaryText;

    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }

    private const string HighScoreKey = "SnakeRoguelike_HighScore";
    private bool _isNewRecordInRun = false;

    void Awake()
    {
        SetHudVisible(true);
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
    }

    public void AddScore(int basePoints, SnakeEffectsManager effects = null)
    {
        if (basePoints <= 0) return;

        int multiplier = 1;
        if (effects != null)
        {
            if (effects.HasInversion) multiplier *= effects.inversionRewardMultiplier;
            if (effects.hasCoolStyle) multiplier *= 2;
        }

        CurrentScore += basePoints * multiplier;

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            _isNewRecordInRun = true;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        UpdateUI();
    }

    public void ShowGameOver()
    {
        SetHudVisible(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (gameOverSummaryText != null)
        {
            string recordNote = _isNewRecordInRun ? "\n<color=#FFD700>НОВИЙ РЕКОРД!</color>" : "";
            gameOverSummaryText.text = $"Рахунок: {CurrentScore}\nРекорд: {HighScore}{recordNote}\n\nНатисни [R] для рестарту";
        }
    }

    public void ResetRun()
    {
        CurrentScore = 0;
        _isNewRecordInRun = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        SetHudVisible(true);
        UpdateUI();
    }

    public void SetHudVisible(bool visible)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
        if (highScoreText != null) highScoreText.gameObject.SetActive(visible);
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {CurrentScore}";
        if (highScoreText != null) highScoreText.text = $"Best: {HighScore}";
    }
}