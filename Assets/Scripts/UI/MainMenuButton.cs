using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void OnMainMenuButtonPressed()
    {
        // Load Main Menu scene
        SceneManager.LoadScene("MainMenu");

        // Reset time scale
        Time.timeScale = 1f;
    }
}
