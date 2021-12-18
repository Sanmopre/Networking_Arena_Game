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
    public GameObject parent_list;

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

        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().RequestQuickMatch();
    }

    public void Join_Lobby()
    {
        Lobby_Menu.SetActive(true);
        Main_Menu.SetActive(false);
        Searching_Menu.SetActive(false);
        First_Menu.SetActive(false);
        DisplayList(current_page);
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
        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().LogIn();
    }

    public void Register()
    {
        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().Register();
    }

    public void InputUsername(string username)
    {
        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().InputUsername(username);
    }

    public void InputPassword(string password)
    {
        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().InputPassword(password);
    }

    public void InputServerIP(string serverIP)
    {
        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().InputServerIP(serverIP);
    }

    public void LogScreen()
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

        if (!Globals.singlePlayer)
            GameObject.Find("Client").GetComponent<Client>().LogOut();
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
            var myLobby = Instantiate(listOfLobbies[i + list_page * lists_per_page], position, Quaternion.identity);
            myLobby.transform.parent = parent_list.transform;
        }
    }

    public void NextPage()
    {
        CleanRoom();
        current_page++;
        if (current_page > number_of_pages)
        {
            current_page = number_of_pages;
        }
        DisplayList(current_page);
    }
    public void PreviousPage() 
    {
        CleanRoom();
        current_page--;
        if (current_page < 0)
        {
            current_page = 0;
        }
        DisplayList(current_page);
    }

    void CleanRoom()
    {
        foreach (Transform child in parent_list.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}
