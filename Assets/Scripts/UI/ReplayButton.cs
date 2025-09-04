using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayButton : MonoBehaviour
{
    public void OnReplayButtonPressed()
    {
        // Reload the current scene
        SceneManager.LoadScene("Game");

        // Reset time scale in case it was paused
        Time.timeScale = 1f;
    }
}
