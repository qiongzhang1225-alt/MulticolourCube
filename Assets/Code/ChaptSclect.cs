using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class chapt1 : MonoBehaviour
{
    public GameObject Chapt1Panel;
    public Button Chapt1Button;
    public Button retryButton;
    private bool isPaused = false;
    // Start is called before the first frame update
    void Start()
    {
        Chapt1Panel.SetActive(false);
        Chapt1Button.onClick.AddListener(OnPauseButtonClicked);
        retryButton.onClick.AddListener(RestartLevel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
}
