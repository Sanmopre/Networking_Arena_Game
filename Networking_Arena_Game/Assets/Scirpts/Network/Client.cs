using System.Net;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Client : MonoBehaviour
{
    EndPoint serverAddress = new IPEndPoint(IPAddress.None, 6969);

    UDP toServer = null;

    bool toRegister;
    public string username;
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
        BACK_TO_MENU,
        DISCONNECTING,
        DISCONNECTED,
        REPLACED
    }
    State state = State.SET_ID_DATA;

    float loginTimer = 0.0f; // TODO: CUTRE NEED TO REMOVE
    float loginMaxTime = 2.0f;

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
    public Vector3 playerOriginalPos = Vector3.zero;

    public void RequestBullet(Vector3 position, Vector3 velocity)
    {
        sendStream.AddBulletFunction(playerID, true, position, velocity);
    }
    public void RequestMissile(Vector3 position, float time)
    {
        sendStream.AddMissileFunction(playerID, true, position, time);
    }
    public void RequestShotgun(Vector3 position, Vector3 direction)
    {
        sendStream.AddShotgunFunction(playerID, true, position, direction);
    }
    public void RequestHit(string name, int damage)
    {
        Debug.Log("Added hit request");
        NetObject netObj = FindNetObjectByName(name);
        if (netObj != null)
            sendStream.AddHitFunction(netObj.netID, netObj.owned, damage);
    }
    public void RequestEnd()
    {
        sendStream.AddEndFunction(playerID, true);
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
            enemyScript = null;
        }
        public int netID;
        public GameObject go;
        public Rigidbody rb;
        public bool owned;
        public Enemy_Controller enemyScript;
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
                GameObject serverTextGo = GameObject.Find("CurrentServer_Text");
                if (serverTextGo != null)
                {
                    Text serverText = serverTextGo.GetComponent<Text>();
                    if (serverText)
                        serverText.text = "Current Server: " + toServer.RemoteAddressStr();
                }
                toServer.remoteAddress = serverAddress;
                break;
            case State.SEND_ID_DATA:
                toServer.remoteAddress = serverAddress;
                char type = 'l';
                if (toRegister)
                    type = 'r';
                notAcknowleged = toServer.Send(type + username + " " + password);
                if (notAcknowleged != -1)
                {
                    state = State.RECV_ID_CONF;
                    loginTimer = Time.realtimeSinceStartup; // TODO REMOVE
                }
                break;
            case State.RECV_ID_CONF:
                if (Time.realtimeSinceStartup >= loginTimer + loginMaxTime) // TODO REMOVE
                {
                    state = State.SET_ID_DATA; // TODO REMOVE
                    Log("No response from server :(");
                }
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

                                string enemyName = oStream.GetString(oStream.GetInt());
                                playerAmount = oStream.GetInt();
                                playerID = lastNetID = oStream.GetInt();
                                playerOriginalPos = oStream.GetVector3();
                                int enemyNetID = oStream.GetInt();
                                Vector3 enemyPosition = oStream.GetVector3();

                                GameObject go = GameObject.Find("Player");
                                if (go != null)
                                {
                                    player = go.GetComponent<Player_Controller>();

                                    NetObject no = new NetObject(lastNetID, go, go.GetComponent<Rigidbody>(), true);
                                    go.tag = playerID.ToString();
                                    no.rb.position = playerOriginalPos;
                                    netObjects.Add(no);
                                    Debug.Log("Added player");
                                }
                                go = GameObject.Find("Enemy");
                                if (go != null)
                                {
                                    NetObject no = new NetObject(enemyNetID, go, go.GetComponent<Rigidbody>(), false);
                                    go.tag = enemyNetID.ToString();

                                    no.enemyScript = go.GetComponent<Enemy_Controller>();
                                    no.enemyScript.username = enemyName;

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
                    sendStream.AddIdData(sendID, game.round);
                    ++sendID;

                    foreach (NetObject netObj in netObjects)
                        if (netObj.owned)
                        {
                            int state = -1;
                            if (netObj.netID == playerID)
                                state = player.GetAnimationState();
                            sendStream.AddObject(netObj.netID, netObj.rb.position, netObj.rb.velocity, netObj.go.transform.rotation.eulerAngles, state);
                        }

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
                            if (data.round != game.round)
                                break;

                            for (int i = 0; i < data.functions.Count; ++i)
                            {
                                switch (data.functions[i].functionType)
                                {
                                    case NetworkStream.Keyword.FNC_BULLET:
                                        player.InstantiateBullet(data.functions[i].position, data.functions[i].velocity, data.functions[i].netId);
                                        break;
                                    case NetworkStream.Keyword.FNC_MISSILE:
                                        player.InstantiateMissile(data.functions[i].position, data.functions[i].time);
                                        break;
                                    case NetworkStream.Keyword.FNC_SHOTGUN:
                                        NetObject netObject = FindNetObject(data.functions[i].netId);
                                        player.InstantiateShotgun(data.functions[i].position, data.functions[i].velocity, netObject.go, netObject.netID);
                                        break;
                                    case NetworkStream.Keyword.FNC_HIT:
                                        NetObject netObj = FindNetObject(data.functions[i].netId);
                                        int pl = 1;
                                        if (netObj.netID != playerID)
                                            pl = 2;
                                        game.TakeDamage(data.functions[i].damage, pl);
                                        break;
                                    case NetworkStream.Keyword.FNC_END:
                                        SceneManager.LoadScene("MainMenuScene");
                                        state = State.BACK_TO_MENU;
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
                                   netObj.go.transform.SetPositionAndRotation(data.objects[i].position, Quaternion.Euler(data.objects[i].direction));
                                   netObj.rb.velocity = data.objects[i].velocity;
                                    if (netObj.enemyScript != null)
                                        netObj.enemyScript.SetAnimationState(data.objects[i].state);
                                }
                                lastRecvID = data.id;
                            }
                            break;
                    }
                }
                break;
            case State.BACK_TO_MENU:
                if (SceneManager.GetActiveScene().name == "MainMenuScene")
                {
                    player = null;
                    game = null;

                    sendStream = new NetworkStream();
                    lastDataSent = 0.0f;
                    sendID = 0;
                    lastRecvID = 0;
                    lastNetID = 0;
                    playerAmount = 2;
                    playerID = 0;
                    playerOriginalPos = Vector3.zero;

                    netObjects.Clear();

                    LoadMainMenu();
                    state = State.IN_MENU;
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
    public void InputServerIP(string serverIP)
    {
        if (state == State.SET_ID_DATA)
        {
            toServer.Close();
            serverAddress = new IPEndPoint(IPAddress.Parse(serverIP), 6969);
        }
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
            menuScript.LogScreen();
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
