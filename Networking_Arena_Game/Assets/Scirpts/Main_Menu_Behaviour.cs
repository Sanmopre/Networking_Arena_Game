using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main_Menu_Behaviour : MonoBehaviour
{

    public GameObject Lobby_Menu;
    public GameObject Main_Menu;
    public GameObject Searching_Menu;
    public GameObject First_Menu;
    public GameObject[] listOfLobbies;

    public int lists_per_page;
    public float lobbies_distance_UI;
    private int number_of_pages;
    private int current_page = 0;
     
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

    void RefreshLobbyList() 
    {
        number_of_pages = listOfLobbies.Length / lists_per_page;
    }

    void DisplayList(int list_page)
    { 
        for(int i = 0; i < lists_per_page; i++) 
        {
            Vector3 position = new Vector3(0,i * lobbies_distance_UI, 0);
            Instantiate(listOfLobbies[i + list_page * lists_per_page], position, Quaternion.identity);
            //under table parent gameobject
        }
    }
}
