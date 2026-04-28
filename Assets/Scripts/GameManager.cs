using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    private bool gameOver;

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

    private void HandleNPCAttack(NPCController attacker)
    {
        Debug.Log($"[GameManager] {attacker.name} alcanzó al jugador. GAME OVER.");
        OnPlayerLose();
    }

    public void OnPlayerWin()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("[GameManager] VICTORIA");
        winPanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnPlayerLose()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("[GameManager] DERROTA");
        Object.FindFirstObjectByType<PlayerController>()?.SetGameOver();
        losePanel?.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}