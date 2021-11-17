using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game_Menu_Manager : MonoBehaviour
{
    public GameObject GameMenu;
    private void Awake()
    {
        GameMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape key was pressed");
            GameMenu.SetActive(!GameMenu.activeSelf);
        }
    }

    public void Resume_Button() 
    {
        GameMenu.SetActive(false);
    }

    public void Options_Button() 
    {

    }

    public void Main_Menu_Button() 
    {
    
    }

    public void Quit_Button() 
    {
        Application.Quit();
    }
}
