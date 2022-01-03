using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    UDP listener = null;

    //-- Console --
    private List<string> consoleMessages = new List<string>(); 
    private bool updateConsole = false;                        
    public Text consoleText;

    // --- Client ---
    struct Client
    {
        public Client(string name, string password, EndPoint remoteAddress)
        {
            this.name = name;
            this.password = password;

            GameObject newClient = new GameObject();
            newClient.name = name;
            if (Globals.localTesting)
                DontDestroyOnLoad(newClient);
            socket = newClient.AddComponent<UDP>();
            socket.remoteAddress = remoteAddress;

            state = State.UNINTERESTED;

            lobby = null;

            WAIT_TIME = 5.0f;
            waitToDisconnect = -1.0f;
        }

        public string name;
        public string password;
        public Ref<Lobby> lobby;

        public readonly float WAIT_TIME;
        public float waitToDisconnect;

        public enum State
        {
            DISCONNECTED,
            UNINTERESTED,
            INTERESTED,
            IN_GAME,
            DISCONNECTING
        }
        public State state;

        public UDP socket;
    }

    bool ClientConnect(Ref<Client> client, EndPoint remoteAddress)
    {
        if (client.value.state == Client.State.DISCONNECTING)
            ClientDisconnect(client);

        if (client.value.state != Client.State.DISCONNECTED)
            return false;

        GameObject newClient = new GameObject();
        newClient.name = client.value.name;
        if (Globals.localTesting)
            DontDestroyOnLoad(newClient);

        client.value.socket = newClient.AddComponent<UDP>();
        client.value.socket.remoteAddress = remoteAddress;
        client.value.state = Client.State.UNINTERESTED;

        return true;
    }

    bool ClientInGame(Ref<Client> client)
    {
        if (client.value.state != Client.State.INTERESTED)
            return false;

        client.value.state = Client.State.IN_GAME;

        return true;
    }

     void ClientDisconnect(Ref<Client> client)
    {
        if (client.value.state == Client.State.DISCONNECTED || client.value.socket == null)
            return;

        Destroy(client.value.socket.gameObject);

        client.value.state = Client.State.DISCONNECTED;
    }
    // --- !Client ---
    List<Ref<Client>> clients = new List<Ref<Client>>();

    // --- Lobby ---
    struct HitRequest
    {
        public HitRequest(int netId, Ref<Client> sender, int damage)
        {
            this.netId = netId;
            this.damage = damage;
            this.sender = sender;

            send = false;
            hitTime = Time.realtimeSinceStartup;
            waitTime = 0.05f;
        }
        public int netId;
        public bool send;
        public int damage;
        public Ref<Client> sender;

        public float hitTime;
        public float waitTime;
    }
    struct Lobby
    {
        public Lobby(int id)
        {
            this.id = id;
            full = false;

            player1 = null;
            player2 = null;

            hitRequests = new List<HitRequest>();
            endRequests = 0;
        }

        public int id;
        public bool full;

        public Ref<Client> player1;
        public Ref<Client> player2;

        public List<HitRequest> hitRequests;
        public int endRequests;
    }

    bool LobbyAddPlayer(Ref<Lobby> lobby, Ref<Client> client)
    {
        if (lobby.value.full || client.value.state != Client.State.UNINTERESTED)
            return false;

        client.value.lobby = lobby;
        if (lobby.value.player1 == null)
        {
            client.value.state = Client.State.INTERESTED;

            lobby.value.player1 = client;
        }
        else
        {
            lobby.value.player2 = client;

            lobby.value.player1.value.state = Client.State.IN_GAME;
            lobby.value.player2.value.state = Client.State.IN_GAME;

            lobby.value.full = true;
        }

        return true;
    }

    void LobbyDismatle(Ref<Lobby> lobby, Ref<Client> disconnected = null)
    {
        if (lobby.value.player1 != null)
        {
            if (lobby.value.player1 != disconnected)
            {
                lobby.value.player1.value.state = Client.State.UNINTERESTED;
                lobby.value.player1.value.socket.Send("exit lobby");
            }
            lobby.value.player1.value.lobby = null;
        }
        if (lobby.value.player2 != null)
        {
            if (lobby.value.player2 != disconnected)
            {
                lobby.value.player2.value.state = Client.State.UNINTERESTED;
                lobby.value.player2.value.socket.Send("exit lobby");
            }
            lobby.value.player2.value.lobby = null;
        }
        lobbies.Remove(lobby);
    }
    // --- !Lobby ---
    List<Ref<Lobby>> lobbies = new List<Ref<Lobby>>();

    void Start()
    {
        if (Globals.localTesting)
            DontDestroyOnLoad(gameObject);
        listener = gameObject.GetComponent<UDP>();
        listener.listenMode = true;
    }

    void Update()
    {
        // console
        if (updateConsole)
        {
            foreach (string message in consoleMessages)
            {
                consoleText.text += message + '\n';
            }

            consoleMessages.Clear();
            updateConsole = false;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Clients:");
            foreach (Ref<Client> client in clients)
                Debug.Log(client.value.name + " - " + client.value.state.ToString());
            Debug.Log("---");
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Lobbies:");
            foreach (Ref<Lobby> lobby in lobbies)
            {
                string n1 = "empty", n2 = "empty";
                if (lobby.value.player1 != null)
                    n1 = lobby.value.player1.value.name;
                if (lobby.value.player2 != null)
                    n2 = lobby.value.player2.value.name;
                Debug.Log(lobby.value.id + " - P1: " + n1 + " - P2: " + n2);
            }
            Debug.Log("---");
        }
        while (listener.CanReceive())
        {
            string received = "";
            switch (listener.Receive(ref received, true))
            {
                case UDP.RecvType.ERROR:
                    continue;
                case UDP.RecvType.FIN:
                    continue;
                case UDP.RecvType.MESSAGE:

                    //TODO
                    consoleMessages.Add(received);
                    updateConsole = true;

                    int separator = received.IndexOf(' ');
                    if (separator != -1)
                    {
                        string name = received.Substring(1, separator - 1);
                        string password = received.Substring(separator + 1);
                        int index = 0;
                        Ref<Client> client = FindClientByName(name, ref index);
                        if (received[0] == 'r')
                        {
                            if (client == null)
                            {
                                Ref<Client> newClient = new Ref<Client> { value = new Client(name, password, listener.remoteAddress) };
                                clients.Add(newClient);
                                newClient.value.socket.Send("registered");
                            }
                            else if (listener.remoteAddress.ToString() != client.value.socket.remoteAddress.ToString())
                                listener.Send("rename");
                        }
                        else if (received[0] == 'l')
                        {
                            if (client == null)
                                listener.Send("unknown");
                            else
                            {
                                if (password == client.value.password)
                                {
                                    if (ClientConnect(client, listener.remoteAddress))
                                        client.value.socket.Send("logged in");
                                    else
                                        listener.Send("imposter");
                                }
                                else
                                    listener.Send("wrong");
                            }
                        }
                    }
                    break;
            }
        }

        for (int index = 0; index < clients.Count; ++index)
        {
            Ref<Client> client = clients[index];
            if (client.value.state == Client.State.DISCONNECTED)
                continue;

            if (client.value.state == Client.State.DISCONNECTING &&
                 Time.realtimeSinceStartup >= client.value.waitToDisconnect + client.value.WAIT_TIME)
            {
                Debug.Log("Server Client " + client.value.name + " disconnected!");
                ClientDisconnect(client);
                continue;
            }

            while (client.value.socket.CanReceive())
            {
                byte[] received = null;
                switch (client.value.socket.Receive(ref received))
                {
                    case UDP.RecvType.ERROR:
                        Debug.Log("Server Client " + client.value.name + " received an Error!");
                        continue;
                    case UDP.RecvType.FIN:
                        Debug.Log("Server Client " + client.value.name + " received FIN packet! ");
                        client.value.state = Client.State.DISCONNECTING;
                        client.value.waitToDisconnect = Time.realtimeSinceStartup;
                        break;
                    case UDP.RecvType.MESSAGE:
                        if (Encoding.UTF8.GetString(received) == "quickmatch")
                        {
                            if (!Globals.localTesting)
                            {
                                Ref<Lobby> lobby = null;
                                for (int i = 0; i < lobbies.Count; ++i)
                                {
                                    lobby = lobbies[i];
                                    if (LobbyAddPlayer(lobby, client))
                                    {
                                        StartCoroutine(StartMatch(lobby.value.player1, lobby.value.player2));
                                        break;
                                    }
                                    lobby = null;
                                }
                                if (lobby == null)
                                {
                                    int newID = 1;
                                    if (lobbies.Count != 0)
                                        newID = lobbies[lobbies.Count - 1].value.id + 1;
                                
                                    lobbies.Add(new Ref<Lobby> { value = new Lobby(newID) });
                                
                                    if (!LobbyAddPlayer(lobbies[lobbies.Count - 1], client))
                                    {
                                        client.value.socket.Send("matchmaking error");
                                        lobbies.RemoveAt(lobbies.Count - 1);
                                    }
                                }
                            }
                            else
                            {
                                client.value.state = Client.State.IN_GAME;
                                client.value.socket.Send("match found");

                                InputStream toPlayer1 = new InputStream();
                                toPlayer1.AddInt(2);
                                toPlayer1.AddInt(0);
                                toPlayer1.AddVector3(new Vector3(-20.0f, 0.5f, 0));
                                toPlayer1.AddInt(1);
                                toPlayer1.AddVector3(new Vector3(20.0f, 0.5f, 0));

                                Debug.Log("Server sent match data");
                                client.value.socket.Send(toPlayer1.GetBuffer());
                            }
                        }
                        else
                        {
                            if (client.value.state == Client.State.IN_GAME && (client.value.lobby != null || Globals.localTesting))
                            {
                                Ref<Lobby> lobby = client.value.lobby;

                                Ref<Client> resendTo;
                                if (!Globals.localTesting)
                                {
                                    resendTo = lobby.value.player1;
                                    if (client == lobby.value.player1)
                                        resendTo = lobby.value.player2;
                                }
                                else
                                {
                                    resendTo = client;
                                }

                                NetworkStream.Data data = NetworkStream.Deserialize(received);

                                if (!Globals.localTesting)
                                    for (int h = 0; h < lobby.value.hitRequests.Count; ++h) // search the hit requests
                                    {
                                        HitRequest hitRequest = lobby.value.hitRequests[h];
                                        if (hitRequest.hitTime != -1 && Time.realtimeSinceStartup >= hitRequest.hitTime + hitRequest.waitTime) // find timed out hit request
                                        {
                                            Ref<Client> otherClient = lobby.value.player1;
                                            if (hitRequest.sender == lobby.value.player1) 
                                                otherClient = lobby.value.player2;
                                            float senderRTT = hitRequest.sender.value.socket.GetRoundTripTime(); // compare round trip time of the clients
                                            float otherRTT = otherClient.value.socket.GetRoundTripTime();

                                            if (senderRTT < otherRTT) // if the sender has better connection...
                                            {
                                                NetworkStream stream = new NetworkStream();
                                                stream.AddIdData(0, data.round);
                                                stream.AddHitFunction(hitRequest.netId, false, lobby.value.hitRequests[h].damage);
                                                byte[] buffer = stream.GetBuffer();

                                                lobby.value.player1.value.socket.Send(buffer); // send a hit function to both players
                                                lobby.value.player2.value.socket.Send(buffer);
                                                Debug.Log("Hit Server Decition");
                                            }

                                            lobby.value.hitRequests.RemoveAt(h); // remove the request
                                            --h;
                                        }
                                    }

                                if (data.functions.Count > 0)
                                {
                                    NetworkStream stream = new NetworkStream();
                                    stream.AddIdData(0, data.round);
                                    for (int f = 0; f < data.functions.Count; ++f)
                                    {
                                        switch (data.functions[f].functionType)
                                        {
                                            case NetworkStream.Keyword.FNC_BULLET:
                                                stream.AddBulletFunction(data.functions[f].netId, data.functions[f].owned, data.functions[f].position, data.functions[f].velocity);
                                                break;
                                            case NetworkStream.Keyword.FNC_MISSILE:
                                                stream.AddMissileFunction(data.functions[f].netId, data.functions[f].owned, data.functions[f].position, data.functions[f].time);
                                                break;
                                            case NetworkStream.Keyword.FNC_SHOTGUN:
                                                stream.AddShotgunFunction(data.functions[f].netId, data.functions[f].owned, data.functions[f].position, data.functions[f].velocity);
                                                break;
                                            case NetworkStream.Keyword.FNC_HIT: // when receiving a hit from a client
                                                if (Globals.localTesting)
                                                {
                                                    stream.AddHitFunction(data.functions[f].netId, false, data.functions[f].damage);
                                                    break;
                                                }
                                                List<HitRequest> hitRequests = lobby.value.hitRequests;
                                                bool found = false;
                                                for (int h = 0; h < hitRequests.Count; ++h) // search the hit requests
                                                {
                                                    if (hitRequests[h].netId == data.functions[f].netId && hitRequests[h].sender != client) // if a hit request to the same target was issued by the other player
                                                    {
                                                        stream.AddHitFunction(hitRequests[h].netId, false, hitRequests[h].damage); // send a hit request to the client and leave the function in data
                                                        found = true;                                                              // so that it is also sent to the other player later
                                                        Debug.Log("Hit Clients Agreement");
                                                        hitRequests.RemoveAt(h);
                                                        break;
                                                    }
                                                }
                                                if (!found) // if no hit request is found
                                                {
                                                    lobby.value.hitRequests.Add(new HitRequest(data.functions[f].netId, client, data.functions[f].damage)); // create a hit request

                                                    data.functions.RemoveAt(f); // remove the function from the deserialized data
                                                    --f;
                                                    received = new NetworkStream(data).GetBuffer(); // and update the byte array we resend to the other player
                                                }
                                                break;
                                            case NetworkStream.Keyword.FNC_END:
                                                ++lobby.value.endRequests;
                                                if (lobby.value.endRequests == 2)
                                                {
                                                    stream.AddEndFunction(0, false);
                                                    LobbyDismatle(lobby);
                                                }
                                                else
                                                {
                                                    data.functions.RemoveAt(f);
                                                    --f;
                                                    received = new NetworkStream(data).GetBuffer();
                                                }
                                                break;
                                        }
                                    }
                                    client.value.socket.Send(stream.GetBuffer());
                                }

                                resendTo.value.socket.Send(received);
                            }
                        }
                        break;
                }
            }
        }
    }

    IEnumerator StartMatch(Ref<Client> player1, Ref<Client> player2)
    {
        Debug.Log("Server notified match");
        player1.value.socket.Send("match found");
        player2.value.socket.Send("match found");

        yield return new WaitForSeconds(0.2f);
        
        InputStream toPlayer1 = new InputStream();
        // toPlayer1.AddInt(player2.value.name.Length);
        //toPlayer1.AddString(player2.value.name);
        toPlayer1.AddInt(2);
        toPlayer1.AddInt(0);
        toPlayer1.AddVector3(new Vector3(-20.0f, 5f, 0));
        toPlayer1.AddInt(1);
        toPlayer1.AddVector3(new Vector3(20.0f, 5f, 0));

        InputStream toPlayer2 = new InputStream();
       // toPlayer1.AddInt(player1.value.name.Length);
       // toPlayer1.AddString(player1.value.name);
        toPlayer2.AddInt(2);
        toPlayer2.AddInt(1);
        toPlayer2.AddVector3(new Vector3(20.0f, 5f, 0));
        toPlayer2.AddInt(0);
        toPlayer2.AddVector3(new Vector3(-20.0f, 5f, 0));

        Debug.Log("Server sent match data");
        player1.value.socket.Send(toPlayer1.GetBuffer());
        player2.value.socket.Send(toPlayer2.GetBuffer());
    }

    Ref<Client> FindClientByName(string name, ref int index)
    {
        for (int i = 0; i < clients.Count; ++i)
            if (clients[i].value.name == name)
            {
                index = i;
                return clients[i];
            }
        index = -1;
        return null;
    }

    private void OnDestroy()
    {
        foreach (Ref<Client> client in clients)
            ClientDisconnect(client);
        clients.Clear();
    }
}
