using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public GameObject gameRulePanel;
    public void StartGameButton()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void ReStartGameButton()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 重新加载当前场景
        SceneManager.LoadScene(currentSceneIndex);
    }
    public void RuleButton()
    {
        gameRulePanel.SetActive(true);
    }
    public void CloseRuleButton()
    {
        gameRulePanel.SetActive(false);
    }
        
}
