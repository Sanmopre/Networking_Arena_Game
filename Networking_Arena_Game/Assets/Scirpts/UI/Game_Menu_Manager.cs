using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game_Menu_Manager : MonoBehaviour
{
    [Header("General")]
    public GameObject GameMenu;
    public GameObject TextObject;
    private Text timerText;
    private Game_Manager gameManager;

    [Header("Player & Enemy")]
    public GameObject PlayerUIprefab;
    public GameObject EnemyUIprefab;

    //dont understand how ui works :(
    [Header("Lifes")]
    public Vector3 canvasOffset;   
    public GameObject UIPlayer1LiveCount;
    public GameObject UIPlayer2LiveCount;

    public float distance_offset_x;
    public float distance_offset_y;

    [Header("Audio")]
    public AudioSource  audioSource;
    public AudioClip    openMenuSFX;
    public AudioClip    closeMenuSFX;
    public AudioClip    clickedButtonSFX;
    
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
            
            if (GameMenu.activeSelf)
            {
                audioSource.clip = openMenuSFX;
            }
            else
            {
                audioSource.clip = closeMenuSFX;
            }
        
            audioSource.Play();
            
            GameMenu.SetActive(!GameMenu.activeSelf);
        }
    }

    public void Resume_Button() 
    {
        audioSource.clip = clickedButtonSFX;
        audioSource.Play();
        
        GameMenu.SetActive(false);
    }

    public void Options_Button() 
    {
        audioSource.clip = clickedButtonSFX;
        audioSource.Play();
    }

    public void Main_Menu_Button() 
    {
        audioSource.clip = clickedButtonSFX;
        audioSource.Play();
    }

    public void Quit_Button() 
    {
        Application.Quit();
    }

    public void Pause_Menu_Button()
    {
        if (GameMenu.activeSelf)
        {
            audioSource.clip = openMenuSFX;
        }
        else
        {
            audioSource.clip = closeMenuSFX;
        }
        
        audioSource.Play();
        
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

        for (int i = 0; i < gameManager.Player.lives; i++) 
        {
            var new_ui = Instantiate(PlayerUIprefab, new Vector3(UIPlayer1LiveCount.transform.position.x -  (i * distance_offset_x), UIPlayer1LiveCount.transform.position.y, UIPlayer1LiveCount.transform.position.z), Quaternion.identity);
            new_ui.transform.parent = UIPlayer1LiveCount.transform;
        }

        for (int i = 0; i < gameManager.Enemy.lives; i++)
        {
            var new_ui = Instantiate(EnemyUIprefab, new Vector3(UIPlayer2LiveCount.transform.position.x + (i * distance_offset_x), UIPlayer2LiveCount.transform.position.y, UIPlayer2LiveCount.transform.position.z), Quaternion.identity);
            new_ui.transform.parent = UIPlayer2LiveCount.transform;
        }
    }
}
