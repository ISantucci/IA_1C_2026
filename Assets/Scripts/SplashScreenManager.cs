using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SplashScreenManager : MonoBehaviour
{
    [Header("Datos")]
    public string[] developerNames = { "Nombre Apellido", "Nombre Apellido" };
    public string[] contactEmails = { "email@ejemplo.com", "email@ejemplo.com" };
    public string universityName = "UADE — Culturas Lúdicas";
    public string year = "2025";

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text developersText;
    public TMP_Text skipText;

    [Header("Tiempos")]
    public float displayDuration = 5f;
    public float fadeOutDuration = 1f;

    private CanvasGroup canvasGroup;
    private float timer;
    private bool fadingOut;

    private void Awake()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
        }
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        if (titleText != null)
            titleText.text = $"{universityName}\n{year}";

        if (developersText != null)
        {
            string devInfo = "Desarrollado por:\n\n";
            for (int i = 0; i < developerNames.Length; i++)
            {
                devInfo += $"{developerNames[i]}";
                if (i < contactEmails.Length)
                    devInfo += $"  —  {contactEmails[i]}";
                devInfo += "\n";
            }
            developersText.text = devInfo;
        }

        if (skipText != null)
            skipText.text = "Presioná cualquier tecla para continuar";
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!fadingOut && (timer >= displayDuration || Input.anyKeyDown))
            StartFadeOut();

        if (fadingOut)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeOutDuration;
            if (canvasGroup.alpha <= 0f)
                LoadMainMenu();
        }
    }

    private void StartFadeOut()
    {
        fadingOut = true;
        timer = 0f;
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}