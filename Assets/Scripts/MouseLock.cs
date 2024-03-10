using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MouseLock : MonoBehaviour
{
    private ClimbingSystem inputControl;
    public GameObject winGamePanel;
    public bool isLocked = true;
    void Update()
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        if(winGamePanel.activeSelf == true)
        {
            isLocked = false;
        }
        else
        {
            isLocked = true;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // 重新加载当前场景
            SceneManager.LoadScene(currentSceneIndex);
        }
    }

}
