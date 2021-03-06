﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void ReloadScene()
    {
    	GameSession.Instance.StopHost();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitTitleScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}
