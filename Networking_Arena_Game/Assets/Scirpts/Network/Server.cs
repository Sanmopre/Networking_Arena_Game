using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


public class Server : MonoBehaviour
{
    UDP listener = null;

    // --- Client ---
    struct Client
    {
        public Client(string name, string password, EndPoint remoteAddress)
        {
            this.name = name;
            this.password = password;

            GameObject newClient = new GameObject();
            newClient.name = name;
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
        DontDestroyOnLoad(newClient); // DELETE

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
    struct Lobby
    {
        public Lobby(int id)
        {
            this.id = id;
            full = false;

            player1 = null;
            player2 = null;
        }

        public int id;
        public bool full;

        public Ref<Client> player1;
        public Ref<Client> player2;
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
        DontDestroyOnLoad(gameObject); // DELETE
        listener = gameObject.GetComponent<UDP>();
        listener.listenMode = true;
    }

    void Update()
    {
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
                        for (int a  = 0; a < received.Length; ++a)
                        {
                            if (received[a] == '\0')
                                received.SetValue(Encoding.UTF8.GetBytes("X")[0], a);
                        }
                        Debug.Log("Server Client " + client.value.name + " received FIN packet! " + Encoding.UTF8.GetString(received));
                        client.value.state = Client.State.DISCONNECTING;
                        client.value.waitToDisconnect = Time.realtimeSinceStartup;
                        break;
                    case UDP.RecvType.MESSAGE:
                        if (Encoding.UTF8.GetString(received) == "quickmatch")
                        {
                            Ref<Lobby> lobby = null;
                            for (int i = 0; i < lobbies.Count; ++i)
                            {
                                lobby = lobbies[i];
                                if (LobbyAddPlayer(lobby, client))
                                {
                                    StartCoroutine(StartMatch(lobby.value.player1.value.socket, lobby.value.player2.value.socket));
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
                            if (client.value.state == Client.State.IN_GAME && client.value.lobby != null)
                            {
                                Ref<Client> resendTo = client.value.lobby.value.player1;
                                if (client == client.value.lobby.value.player1)
                                    resendTo = client.value.lobby.value.player2;
                            
                                Debug.Log("Resent Serialized Data from client " + client.value.name + " to client " + resendTo.value.name);
                                resendTo.value.socket.Send(received);
                            }
                        }
                        break;
                }
            }
        }
    }

    IEnumerator StartMatch(UDP player1, UDP player2)
    {
        player1.Send("match found");
        player2.Send("match found");

        yield return new WaitForSeconds(5.0f);

        InputStream toPlayer1 = new InputStream();
        toPlayer1.AddInt(2);
        toPlayer1.AddInt(0);
        toPlayer1.AddVector3(new Vector3(-10.0f, 0.5f, 0));
        toPlayer1.AddInt(1);
        toPlayer1.AddVector3(new Vector3(10.0f, 0.5f, 0));

        InputStream toPlayer2 = new InputStream();
        toPlayer2.AddInt(2);
        toPlayer2.AddInt(1);
        toPlayer2.AddVector3(new Vector3(10.0f, 0.5f, 0));
        toPlayer2.AddInt(0);
        toPlayer2.AddVector3(new Vector3(-10.0f, 0.5f, 0));

        player1.Send(toPlayer1.GetBuffer());
        player2.Send(toPlayer2.GetBuffer());
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
