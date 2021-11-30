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
    const int MAX_BUFFER = 1300;
    byte[] DISCONNECT = new byte[1]; 
    EndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969);

    Socket toServer = null;
    EndPoint remoteAddress = null;

    bool toRegister;
    string username;
    string password;

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

        toServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        toServer.Blocking = false;
        toServer.Bind(new IPEndPoint(IPAddress.Any, 0));

        remoteAddress = serverAddress;
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
                remoteAddress = serverAddress;
                if (Send(Encoding.UTF8.GetBytes(type + username + " " + password)))
                    state = State.RECV_ID_CONF;
                break;
            case State.RECV_ID_CONF:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive(true);
                    if (received == null)
                    {
                        state = State.DISCONNECTED;

                        break;
                    }

                    string message = Encoding.UTF8.GetString(received).TrimEnd('\0');
                    if (message == "registered" || message == "logged in")
                    {
                        LoadMainMenu();

                        Debug.Log("Succesfully " + message + "!");
                        state = State.IN_MENU;
                    }
                    else
                    {
                        if (message == "rename")
                        {
                            Log("The name you chose to register is already in use.");
                            state = State.SET_ID_DATA;
                        }
                        else if (message == "unknown")
                        {
                            Log("The username does not exist. If you are new go register!");
                            state = State.SET_ID_DATA;
                        }
                        else if (message == "wrong")
                        {
                            Log("Wrong password.");
                            state = State.SET_ID_DATA;
                        }
                        else if (message == "imposter")
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
                }
                break;
            case State.IN_MENU:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive();
                    if (received == null)
                    {
                        state = State.DISCONNECTED;

                        break;
                    }
                    if (received.Length == 0)
                        break;

                    Debug.Log(Encoding.UTF8.GetString(received).TrimEnd('\0'));
                }
                break;
            case State.WAITING_FOR_MATCH:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive();
                    if (received == null)
                    {
                        state = State.DISCONNECTED;

                        break;
                    }
                    if (received.Length == 0)
                        break;

                    string message = Encoding.UTF8.GetString(received).TrimEnd('\0');
                    if (message == "match found")
                        SceneManager.LoadScene("Game_Scene");
                    else
                    {
                        state = State.IN_MENU;
                        LoadMainMenu();
                    }
                }
                break;
            case State.DISCONNECTING:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive();
                    if (received == null)
                    {
                        state = State.DISCONNECTED;

                        break;
                    }
                    if (received.Length == 0)
                        break;
                }

                break;
            case State.DISCONNECTED:
                LoadIdentificationMenu();
                state = State.SET_ID_DATA;
                break;
        }
    }

    // --- Socket ---
    bool Send(byte[] toSend)
    {
        if (toSend.Length > MAX_BUFFER)
        {
            Debug.Log("Client Send Error: Message larger than " + MAX_BUFFER);
            return false;
        }    

        try
        {
            toServer.SendTo(toSend, remoteAddress);
        }
        catch (SocketException error)
        {
            Debug.Log("Client Send Error: " + error.Message);
            return false;
        }
        return true;
    }

    byte[] Receive(bool setRemote = false)
    {
        byte[] recvBuffer = new byte[MAX_BUFFER];
        EndPoint from = new IPEndPoint(IPAddress.None, 0);
        int bytesRecv;

        try
        {
            bytesRecv = toServer.ReceiveFrom(recvBuffer, ref from);
        }
        catch (SocketException error)
        {
            Debug.Log("Client Receive Error: " + error.Message);
            return null;
        }

        if (from.ToString() != remoteAddress.ToString())
            if (setRemote)
                remoteAddress = from;
            else
            {
                Debug.Log("Client Receive Error: Received a message from an unknown address");
                return new byte[0];
            }

        if (bytesRecv == 1)
        {
            Debug.Log("Server Disconnected");
            return null;
        }

        return recvBuffer;
    }
    // --- !Socket ---

    private void OnDestroy()
    {
        if (state == State.REPLACED || state == State.DISCONNECTED)
            return;

        if (state != State.DISCONNECTING)
            Send(DISCONNECT);
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

        Send(DISCONNECT);
        state = State.DISCONNECTING;
    }

    public void RequestQuickMatch()
    {
        if (state != State.IN_MENU)
            return;

        Send(Encoding.UTF8.GetBytes("quickmatch"));
        state = State.WAITING_FOR_MATCH;
        menuScript.Quickplay_Button();
    }

    bool CheckValidName(string name)
    {
        if (name.Contains(" "))
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
