using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Header("胜利 UI")]
    public VictoryUI victoryUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("到达终点！");

        if (victoryUI != null)
        {
            victoryUI.ShowVictory();  // 只展示UI和播放BGM
        }
        else
        {
            Debug.LogWarning("VictoryUI 未绑定！");
        }
    }
}
