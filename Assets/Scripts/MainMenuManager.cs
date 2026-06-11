using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text gameTitleText;
    public TMP_Text subtitleText;
    public Button playButton;
    public TMP_Text playButtonText;

    [Header("Datos")]
    public string gameTitle = "Hide and Escape";
    public string gameSubtitle = "Un juego de sigilo y estrategia";

    private CanvasGroup canvasGroup;
    private float fadeInTimer;
    private bool fadingIn = true;
    private const float FadeInDuration = 1f;

    private void Awake()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        if (gameTitleText != null) gameTitleText.text = gameTitle;
        if (subtitleText != null) subtitleText.text = gameSubtitle;
        if (playButtonText != null) playButtonText.text = "PLAY";

        playButton?.onClick.AddListener(OnPlayClicked);
    }

    private void Update()
    {
        if (fadingIn)
        {
            fadeInTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(fadeInTimer / FadeInDuration);
            if (canvasGroup.alpha >= 1f) fadingIn = false;
        }
    }

    private void OnPlayClicked()
    {
        SceneManager.LoadScene(2);
    }
}