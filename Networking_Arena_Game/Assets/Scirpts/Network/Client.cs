using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    EndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969);

    UDP toServer = null;

    bool toRegister;
    string username;
    string password;

    int notAcknowleged;

    enum State
    {
        SET_ID_DATA,
        SEND_ID_DATA,
        RECV_ID_CONF,
        SET_REMOTE,
        IN_MENU,
        WAITING_FOR_MATCH,
        IN_GAME,
        DISCONNECTING,
        DISCONNECTED,
        REPLACED
    }
    State state = State.SET_ID_DATA;

    // --- UI ---
    [HideInInspector]
    public Main_Menu_Behaviour menuScript = null;
    [HideInInspector]
    public Text errorLogText = null;
    [HideInInspector]
    public Text menuNameText = null;
    // ---

    void Start()
    {
        Client[] destObjects = UnityEngine.Object.FindObjectsOfType<Client>();
        for (int i = 0; i < destObjects.Length; ++i)
            if (destObjects[i] != this && destObjects[i].name == name)
            {
                destObjects[i].menuScript = FindCanvasObjectByName("Main_Menu").GetComponent<Main_Menu_Behaviour>();
                destObjects[i].errorLogText = FindCanvasObjectByName("ErrorLogText").GetComponent<Text>();
                destObjects[i].menuNameText = FindCanvasObjectByName("MenuNameText").GetComponent<Text>();

                state = State.REPLACED;
                Destroy(gameObject);
                return;
            }
        DontDestroyOnLoad(gameObject);

        menuScript = FindCanvasObjectByName("Main_Menu").GetComponent<Main_Menu_Behaviour>();
        errorLogText = FindCanvasObjectByName("ErrorLogText").GetComponent<Text>();
        menuNameText = FindCanvasObjectByName("MenuNameText").GetComponent<Text>();

        toServer = gameObject.GetComponent<UDP>();
        toServer.remoteAddress = serverAddress;
    }

    void Update()
    {
        switch (state)
        {
            case State.SET_ID_DATA:
                break;
            case State.SEND_ID_DATA:
                char type = 'l';
                if (toRegister)
                    type = 'r';
                toServer.remoteAddress = serverAddress;
                notAcknowleged = toServer.Send(type + username + " " + password);
                if (notAcknowleged != -1)
                    state = State.RECV_ID_CONF;
                break;
            case State.RECV_ID_CONF:
                while (toServer.CanReceive())
                {
                    string received = "";
                    switch (toServer.Receive(ref received, true))
                    {
                        case UDP.RecvType.ERROR:
                            continue;
                        case UDP.RecvType.FIN:
                            toServer.Close();
                            state = State.DISCONNECTED;
                            break;
                        case UDP.RecvType.MESSAGE:
                            if (received == "registered" || received == "logged in")
                            {
                                LoadMainMenu();

                                Log("Succesfully " + received + "!");
                                state = State.IN_MENU;
                            }
                            else
                            {
                                if (received == "rename")
                                {
                                    Log("The name you chose to register is already in use.");
                                    state = State.SET_ID_DATA;
                                }
                                else if (received == "unknown")
                                {
                                    Log("The username does not exist. If you are new go register!");
                                    state = State.SET_ID_DATA;
                                }
                                else if (received == "wrong")
                                {
                                    Log("Wrong password.");
                                    state = State.SET_ID_DATA;
                                }
                                else if (received == "imposter")
                                {
                                    Log("The server has deemed you SUS, get your imposter ass out of here!");
                                    state = State.SET_ID_DATA;
                                }
                                else
                                {
                                    Log("Unknown response from the server.");
                                    state = State.SET_ID_DATA;
                                }
                            }
                            break;
                    }
                }
                break;
            case State.IN_MENU:
                while (toServer.CanReceive())
                {
                    string received = "";
                    switch (toServer.Receive(ref received))
                    {
                        case UDP.RecvType.ERROR:
                            continue;
                        case UDP.RecvType.FIN:
                            toServer.Close();
                            state = State.DISCONNECTED;
                            break;
                        case UDP.RecvType.MESSAGE:
                            Log(received);
                            break;
                    }
                }
                break;
            case State.WAITING_FOR_MATCH:
                while (toServer.CanReceive())
                {
                    string received = "";
                    switch (toServer.Receive(ref received))
                    {
                        case UDP.RecvType.ERROR:
                            continue;
                        case UDP.RecvType.FIN:
                            toServer.Close();
                            state = State.DISCONNECTED;
                            break;
                        case UDP.RecvType.MESSAGE:
                            if (received == "match found")
                                SceneManager.LoadScene("Game_Scene");
                            else
                            {
                                state = State.IN_MENU;
                                LoadMainMenu();
                            }
                            break;
                    }
                }
                break;
            case State.DISCONNECTING:
                while (toServer.CanReceive())
                {
                    string received = "";
                    switch (toServer.Receive(ref received))
                    {
                        case UDP.RecvType.ERROR:
                            continue;
                        case UDP.RecvType.FIN:
                            toServer.Close();
                            state = State.DISCONNECTED;
                            break;
                    }
                }

                break;
            case State.DISCONNECTED:
                LoadIdentificationMenu();
                state = State.SET_ID_DATA;
                break;
        }
    }

    private void OnDestroy()
    {
        if (state == State.REPLACED || state == State.DISCONNECTED)
            return;

        if (state != State.DISCONNECTING)
            toServer.Send(null);
        toServer.Close();
    }

    // --- UI(Menu) ---
    public void InputUsername(string username)
    {
        if (state == State.SET_ID_DATA)
            this.username = username;
    }

    public void InputPassword(string password)
    {
        if (state == State.SET_ID_DATA)
            this.password = password;
    }

    public void LogIn()
    {
        if (state != State.SET_ID_DATA)
            return;

        if (CheckValidName(username))
        {
            toRegister = false;
            state = State.SEND_ID_DATA;
        }
        else
        {
            Log("This name contains a forviden character -> ' '");
        }
    }

    public void Register()
    {
        if (state != State.SET_ID_DATA)
            return;

        if (CheckValidName(username))
        {
            toRegister = true;
            state = State.SEND_ID_DATA;
        }
        else
        {
            Log("This name contains a forviden character -> ' '");
        }
    }

    public void LogOut()
    {
        if (state != State.IN_MENU)
            return;

        toServer.Send(null);
        state = State.DISCONNECTING;
    }

    public void RequestQuickMatch()
    {
        if (state != State.IN_MENU)
            return;

        toServer.Send("quickmatch");
        state = State.WAITING_FOR_MATCH;
        menuScript.Quickplay_Button();
    }

    bool CheckValidName(string name)
    {
        if (name.Contains(" ") || name.Length == 0)
            return false;
        return true;
    }

    void Log(string toLog, bool debug = true)
    {
        if (debug)
            Debug.Log(toLog);
        if (errorLogText != null)
            errorLogText.text = toLog;
    }

    void LoadMainMenu()
    {
        if (menuScript != null)
            menuScript.Log_In();
        if (menuNameText != null)
            menuNameText.text = username;

        Log("", false);
    }

    void LoadIdentificationMenu()
    {
        if (menuScript != null)
            menuScript.Log_Out();
        Log("", false);
    }
    // --- !UI(Menu) ---

    // --- WhyIsThisNecessaryUnityGetYourShitTogether ---
    // Aparently the GameObject.Find([name]) function doesn't find inactive objects so ... here we are.
    GameObject FindCanvasObjectByName(string name)
    {
        Transform[] transforms = GameObject.Find("Canvas").GetComponentsInChildren<Transform>(true);
        foreach (Transform transform in transforms)
            if (transform.name == name)
                return transform.gameObject;

        return null;
    }
    // --- !WhyIsThisNecessaryUnityGetYourShitTogether ---
}
