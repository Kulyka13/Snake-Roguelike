using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Якщо використовуєш звичайний Text замість TextMeshPro, заміни TMP_Text на Text

public class ModifierSelectionUI : MonoBehaviour
{
    [Header("UI Елементи")]
    public GameObject selectionPanel; // Панель на весь екран, яка ховається/показується
    public Button[] choiceButtons;    // Масив із 3 кнопок
    public TMP_Text[] titleTexts;     // Назви на кнопках (3 шт.)
    public TMP_Text[] descTexts;      // Описи на кнопках (3 шт.)

    public ScoreManager scoreManager;
    private SnakeMovement _targetSnake;
    private List<SnakeModifier> _currentChoices = new List<SnakeModifier>();

    void Start()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    public void ShowChoices(List<SnakeModifier> allModifiers, SnakeMovement snake)
    {
        if (scoreManager != null) scoreManager.SetHudVisible(false);
        if (allModifiers == null || allModifiers.Count == 0)
        {
            Debug.LogError("ModifierSelectionUI: список модифікаторів порожній!");
            return;
        }

        if (selectionPanel == null)
        {
            Debug.LogError("ModifierSelectionUI: поле selectionPanel не призначено!");
            return;
        }

        _targetSnake = snake;
        _currentChoices.Clear();

        // Відбираємо лише ті модифікатори, які зараз дозволено пропонувати
        List<SnakeModifier> pool = new List<SnakeModifier>();
        foreach (var mod in allModifiers)
        {
            if (mod != null && mod.CanBeOffered(snake))
            {
                pool.Add(mod);
            }
        }
        int choicesCount = Mathf.Min(3, pool.Count);

        for (int i = 0; i < choicesCount; i++)
        {
            int randIndex = Random.Range(0, pool.Count);
            _currentChoices.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
            {
                Debug.LogError($"ModifierSelectionUI: кнопка під індексом {i} не прив'язана в інспекторі!");
                continue;
            }

            if (i < _currentChoices.Count)
            {
                choiceButtons[i].gameObject.SetActive(true);

                if (i < titleTexts.Length && titleTexts[i] != null)
                    titleTexts[i].text = _currentChoices[i].modifierName;

                if (i < descTexts.Length && descTexts[i] != null)
                    descTexts[i].text = _currentChoices[i].description;

                int choiceIndex = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => SelectModifier(choiceIndex));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        selectionPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void SelectModifier(int index)
    {
        if (scoreManager != null) scoreManager.SetHudVisible(true);
        if (_targetSnake != null && index >= 0 && index < _currentChoices.Count)
        {
            _currentChoices[index].Apply(_targetSnake);
        }

        // Ховаємо вікно та знімаємо паузу
        selectionPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}