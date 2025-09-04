using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    public void OnStartGameButtonPressed()
    {
        // Load the first game scene (replace "Level1" with your scene name)
        SceneManager.LoadScene("Game");

        // Reset time scale in case paused
        Time.timeScale = 1f;
    }
}
