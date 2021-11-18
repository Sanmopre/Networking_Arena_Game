using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main_Menu_Behaviour : MonoBehaviour
{

    public GameObject Lobby_Menu;
    public GameObject Main_Menu;
    public GameObject Searching_Menu;
    public GameObject First_Menu;

    private void Awake()
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(false);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(true);
    }

    public void Quickplay_Button() 
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(false);
        Searching_Menu.SetActive(true);
        First_Menu.SetActive(false);
    }

    public void Join_Lobby()
    {
        Lobby_Menu.SetActive(true);
        Main_Menu.SetActive(false);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
    }

    public void Options_Button() 
    {
    
    }

    public void Buck_Button_Lobby()
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(true);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
    }

    public void Buck_Button_Searching()
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(true);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
    }

    public void Exit_Button() 
    {
        Application.Quit();
    }

    public void Log_In() 
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(true);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
    }

    public void Register()
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(true);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
    }

    public void Log_Out()
    {
        Lobby_Menu.SetActive(false);
        Main_Menu.SetActive(false);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(true);
    }

}