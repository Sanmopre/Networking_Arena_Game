using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game_Menu_Manager : MonoBehaviour
{
    public GameObject GameMenu;
    public GameObject TextObject;
    private Text timerText;
    private Game_Manager gameManager;

    public GameObject UIPlayerLifePrefab;

    //dont understand how ui works :(
    public Vector3 canvasOffSet;   
    public GameObject UIPlayer1LiveCount;
    public GameObject UIPlayer2LiveCount;

    public float distance_offset_x;
    public float distance_offset_y;
    private void Awake()
    {
        GameMenu.SetActive(false);
        timerText = TextObject.GetComponent<Text>();
        gameManager = GameObject.Find("GameManager").GetComponent<Game_Manager>();
    }

    private void Start()
    {
        UpdatePlayerLifesUI();
    }

    void Update()
    {
        DisplayTime(gameManager.initial_time);

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

    public void UpdatePlayerLifesUI() 
    {
        foreach (Transform child in UIPlayer1LiveCount.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (Transform child in UIPlayer2LiveCount.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        for (int i = 0; i < gameManager.Player1.lives; i++) 
        {
            var new_ui = Instantiate(UIPlayerLifePrefab, new Vector3(canvasOffSet.x -126 - (i * distance_offset_x), canvasOffSet.y + distance_offset_y, canvasOffSet.z ), Quaternion.identity);
            new_ui.transform.parent = UIPlayer1LiveCount.transform;
        }

        for (int i = 0; i < gameManager.Player2.lives; i++)
        {
            var new_ui = Instantiate(UIPlayerLifePrefab, new Vector3(canvasOffSet.x + 126 + (i * distance_offset_x), canvasOffSet.y + distance_offset_y, canvasOffSet.z), Quaternion.identity);
            new_ui.transform.parent = UIPlayer2LiveCount.transform;
        }
    }
}
