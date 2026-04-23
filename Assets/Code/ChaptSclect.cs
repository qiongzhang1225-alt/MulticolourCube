using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class chapt1 : MonoBehaviour
{
    public GameObject Chapt1Panel;
    public Button Chapt1Button;
    public Button retryButton;
    public Button backToSelectButton;   // 新增：返回关卡选择按钮（可选，不拖也不报错）
    private bool isPaused = false;

    void Start()
    {
        Chapt1Panel.SetActive(false);
        Chapt1Button.onClick.AddListener(OnPauseButtonClicked);
        retryButton.onClick.AddListener(RestartLevel);

        if (backToSelectButton != null)
            backToSelectButton.onClick.AddListener(BackToLevelSelect);
    }

    void Update() { }

    public void OnPauseButtonClicked()
    {
        if (!isPaused)
            PauseGame();
        else
            ResumeGame();
    }

    void PauseGame()
    {
        isPaused = true;
        Chapt1Panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Chapt1Panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void BackToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }
}
