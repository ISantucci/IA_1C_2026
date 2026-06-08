using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject timeoutPanel;

    [Header("Timer UI")]
    public TMP_Text timerText;
    public Image timerFill;

    [Header("Timer")]
    public float maxGameDuration = 180f;

    private bool gameOver;
    private float elapsedTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        foreach (var npc in Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None))
        {
            npc.OnAttackPlayer += HandleNPCAttack;
            Debug.Log($"[GameManager] NPC registrado: {npc.name} ({npc.enemyType} - {npc.groupName})");
        }
    }

    private void Update()
    {
        if (gameOver) return;

        elapsedTime += Time.deltaTime;

        UpdateTimerUI();

        if (elapsedTime >= maxGameDuration)
            OnTimeOut();
    }

    private void UpdateTimerUI()
    {
        float remaining = Mathf.Max(0f, maxGameDuration - elapsedTime);

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            timerText.color = remaining <= 30f
                ? (Mathf.Sin(Time.time * 5f) > 0f ? Color.red : Color.white)
                : Color.white;
        }

        if (timerFill != null)
            timerFill.fillAmount = 1f - (elapsedTime / maxGameDuration);
    }

    private void HandleNPCAttack(NPCController attacker)
    {
        Debug.Log($"[GameManager] {attacker.name} alcanzó al jugador. GAME OVER.");
        OnPlayerLose();
    }

    public void OnPlayerWin()
    {
        if (gameOver) return;

        if (elapsedTime < 40f)
        {
            Debug.Log($"[GameManager] Victoria demasiado rápida ({elapsedTime:F1}s). Mínimo 40s.");
            return;
        }

        gameOver = true;
        Debug.Log($"[GameManager] VICTORIA en {elapsedTime:F1}s");
        winPanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnPlayerLose()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log($"[GameManager] DERROTA en {elapsedTime:F1}s");
        Object.FindFirstObjectByType<PlayerController>()?.SetGameOver();
        losePanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnTimeOut()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log($"[GameManager] TIEMPO AGOTADO ({maxGameDuration}s)");
        Object.FindFirstObjectByType<PlayerController>()?.SetGameOver();

        if (timeoutPanel != null)
            timeoutPanel.SetActive(true);
        else
            losePanel?.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }
}