using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    // �� Inspector ������Ŀ�곡����
    public string targetSceneName;
    public Button Button;

    void Start()
    {
        Button.onClick.AddListener(LoadTargetScene);
        CollectableStar.CollectedCount = 0;
        CollectableCoin.CollectedCount = 0;
    }
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("δָ��Ŀ�곡�����ƣ�");
        }
    }

}
