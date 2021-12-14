using System.Net;
using System.Collections.Generic;
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
        GAME_SETUP,
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
    // --- !UI ---

    // --- Game ---
    Player_Controller player = null;
    Game_Manager game = null;

    NetworkStream sendStream = new NetworkStream();
    float lastDataSent = 0.0f;
    int sendID = 0;
    int lastRecvID = 0;
    int lastNetID = 0;
    int playerAmount = 2;
    public int playerID = 0;

    public void RequestBullet(Vector3 position, Vector3 velocity)
    {
        sendStream.AddBulletFunction(playerID, true, position, velocity);
    }
    public void RequestMissile(Vector3 position, float time)
    {
        sendStream.AddMissileFunction(playerID, true, position, time);
    }
    public void RequestHit(string name, int damage)
    {
        Debug.Log("Added hit request");
        NetObject netObj = FindNetObjectByName(name);
        sendStream.AddHitFunction(netObj.netID, netObj.owned, damage);
    }
    // --- !Game ---

    // --- NetworkObjects ---
    class NetObject
    {
        public NetObject(int netID, GameObject go, Rigidbody rb, bool owned)
        {
            this.netID = netID;
            this.go = go;
            this.rb = rb;
            this.owned = owned;
        }
        public int netID;
        public GameObject go;
        public Rigidbody rb;
        public bool owned;
    }
    List<NetObject> netObjects = new List<NetObject>();

    NetObject FindNetObject(int netID)
    {
        foreach (NetObject netObj in netObjects)
            if (netObj.netID == netID)
                return netObj;
        return null;
    }
    NetObject FindNetObjectByName(string name)
    {
        foreach (NetObject netObj in netObjects)
            if (netObj.go.name == name)
                return netObj;
        return null;
    }
    // --- !NetworkObjects ---

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
                    bool exit = false;
                    string received = null;
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
                            {
                                state = State.GAME_SETUP;
                                SceneManager.LoadScene("Game_Scene");
                                Debug.Log("Got into a game");
                            }
                            else
                            {
                                toServer.Close();
                                state = State.DISCONNECTED;
                            }
                            exit = true;
                            break;
                    }
                    if (exit)
                        break;
                }
                break;
            case State.GAME_SETUP:
                if (SceneManager.GetActiveScene().name == "Game_Scene")
                {
                    Debug.Log("Loaded game scene");
                    while (toServer.CanReceive())
                    {
                        bool exit = false;
                        byte[] received = null;
                        switch (toServer.Receive(ref received))
                        {
                            case UDP.RecvType.ERROR:
                                continue;
                            case UDP.RecvType.FIN:
                                Debug.Log("Game Setup Error");
                                toServer.Close();
                                state = State.DISCONNECTED;
                                break;
                            case UDP.RecvType.MESSAGE:
                                OutputStream oStream = new OutputStream(received);

                                playerAmount = oStream.GetInt();
                                playerID = lastNetID = oStream.GetInt();
                                Vector3 playerPosition = oStream.GetVector3();
                                int enemyNetID = oStream.GetInt();
                                Vector3 enemyPosition = oStream.GetVector3();

                                GameObject go = GameObject.Find("Player");
                                if (go != null)
                                {
                                    player = go.GetComponent<Player_Controller>();

                                    NetObject no = new NetObject(lastNetID, go, go.GetComponent<Rigidbody>(), true);
                                    go.tag = playerID.ToString();
                                    no.rb.position = playerPosition;
                                    netObjects.Add(no);
                                    Debug.Log("Added player");
                                }
                                go = GameObject.Find("Enemy");
                                if (go != null)
                                {
                                    NetObject no = new NetObject(enemyNetID, go, go.GetComponent<Rigidbody>(), false);
                                    go.tag = enemyNetID.ToString();
                                    no.rb.position = enemyPosition;
                                    netObjects.Add(no);
                                    Debug.Log("Added enemy");
                                }
                                go = GameObject.Find("GameManager");
                                if (go != null)
                                {
                                    game = go.GetComponent<Game_Manager>();
                                }

                                state = State.IN_GAME;
                                exit = true;
                                break;
                        }
                        if (exit)
                            break;
                    }
                }
                break;
            case State.IN_GAME:
                if (Time.realtimeSinceStartup >= lastDataSent + UDP.SEND_RATE)
                {
                    sendStream.AddIdData(sendID);
                    ++sendID;

                    foreach (NetObject netObj in netObjects)
                        if (netObj.owned)
                            sendStream.AddObject(netObj.netID, netObj.rb.position, netObj.rb.velocity);

                    toServer.Send(sendStream.GetBuffer());
                    sendStream = new NetworkStream();

                    lastDataSent = Time.realtimeSinceStartup;
                }
                while (toServer.CanReceive())
                {
                    byte[] received = null;
                    switch (toServer.Receive(ref received))
                    {
                        case UDP.RecvType.EMPTY:
                            continue;
                        case UDP.RecvType.ERROR:
                            continue;
                        case UDP.RecvType.FIN:
                            toServer.Close();
                            state = State.DISCONNECTED;
                            break;
                        case UDP.RecvType.MESSAGE:
                            NetworkStream.Data data = NetworkStream.Deserialize(received);
                            if (data == null)
                                break;
                            for (int i = 0; i < data.functions.Count; ++i)
                            {
                                switch (data.functions[i].functionType)
                                {
                                    case NetworkStream.Keyword.FNC_BULLET:
                                        player.InstantiateBullet(data.functions[i].position, data.functions[i].velocity, data.functions[i].netId);
                                        break;
                                    case NetworkStream.Keyword.FNC_HIT:
                                        NetObject netObj = FindNetObject(data.functions[i].netId);
                                        int pl = 1;
                                        if (netObj.netID != playerID)
                                            pl = 2;
                                        game.TakeDamage(data.functions[i].damage, pl);
                                        break;
                                }
                            }

                            if (lastRecvID < data.id)
                            {
                                for (int i = 0; i < data.objects.Count; ++i)
                                {
                                   if (Globals.localTesting && data.objects[i].netId == 0)
                                   {
                                       NetworkStream.Object o = data.objects[i];
                                       o.netId = 1;
                                       data.objects[i] = o;
                                   }
                                   
                                   NetObject netObj = FindNetObject(data.objects[i].netId);
                                   
                                   if (netObj == null)
                                       continue;
                                   netObj.rb.position = data.objects[i].position;
                                   netObj.rb.velocity = data.objects[i].velocity;
                                }
                                lastRecvID = data.id;
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
            toServer.Send((byte[])null);
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

        toServer.Send((byte[])null);
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
        if (SceneManager.GetActiveScene().name == "Game_Scene")
        {
            SceneManager.LoadScene("MainMenuScene");
            Debug.Log("Back to menu");
            return;
        }
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
