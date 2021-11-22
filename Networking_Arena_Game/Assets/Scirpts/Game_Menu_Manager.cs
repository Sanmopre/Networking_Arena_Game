using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game_Menu_Manager : MonoBehaviour
{
    public GameObject GameMenu;
    public float initial_time = 90;
    public GameObject TextObject;
    private Text timerText;
    private void Awake()
    {
        GameMenu.SetActive(false);
        timerText = TextObject.GetComponent<Text>();
    }

    void Update()
    {
        initial_time -= Time.deltaTime;
        DisplayTime(initial_time);

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

    public void Pause_Menu_Button()
    {
        GameMenu.SetActive(!GameMenu.activeSelf);
    }

    void DisplayTime(float timeToDisplay) 
    {
        if(timeToDisplay < 0) 
        {
            timeToDisplay = 0;
        }

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
