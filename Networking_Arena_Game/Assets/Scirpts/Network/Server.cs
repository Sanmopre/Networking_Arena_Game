using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


public class Server : MonoBehaviour
{
    const int MAX_BUFFER = 1300;
    byte[] DISCONNECT = new byte[1];

    Socket listener = null;

    public class Ref<T> where T : struct
    {
        public T value;
    }

    // --- Client ---
    struct Client
    {
        public Client(string name, string password, EndPoint remoteAddress)
        {
            this.name = name;
            this.password = password;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            this.remoteAddress = remoteAddress;

            state = State.UNINTERESTED;

            lobby = null;
        }

        public string name;
        public string password;
        public Ref<Lobby> lobby;

        public enum State
        {
            DISCONNECTED,
            UNINTERESTED,
            INTERESTED,
            IN_GAME
        }
        public State state;

        public Socket socket;
        public EndPoint remoteAddress;
    }

    bool ClientConnect(Ref<Client> client, EndPoint remoteAddress)
    {
        if (client.value.state != Client.State.DISCONNECTED)
            return false;

        client.value.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        client.value.socket.Blocking = false;
        client.value.socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        client.value.remoteAddress = remoteAddress;
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
        if (client.value.state == Client.State.DISCONNECTED)
            return;

        Send(client.value.socket, client.value.remoteAddress, DISCONNECT);

        client.value.socket.Close();
        client.value.socket = null;
        client.value.remoteAddress = null;
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
                Send(lobby.value.player1.value.socket, lobby.value.player1.value.remoteAddress, Encoding.UTF8.GetBytes("exit lobby"));
            }
            lobby.value.player1.value.lobby = null;
        }
        if (lobby.value.player2 != null)
        {
            if (lobby.value.player2 != disconnected)
            {
                lobby.value.player2.value.state = Client.State.UNINTERESTED;
                Send(lobby.value.player2.value.socket, lobby.value.player2.value.remoteAddress, Encoding.UTF8.GetBytes("exit lobby"));
            }
            lobby.value.player2.value.lobby = null;
        }
        lobbies.Remove(lobby);
    }
    // --- !Lobby ---
    List<Ref<Lobby>> lobbies = new List<Ref<Lobby>>();

    void Start()
    {
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        listener.Blocking = false;
        listener.Bind(new IPEndPoint(IPAddress.Any, 6969));
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
        if (listener.Poll(0, SelectMode.SelectRead))
        {
            EndPoint fromAddress = new IPEndPoint(IPAddress.None, 0);
            byte[] received = Receive(listener, ref fromAddress, true);
            if (received != null && received.Length != 0)
            {
                string message = Encoding.UTF8.GetString(received).TrimEnd('\0');

                int separator = message.IndexOf(' ');
                if (separator != -1)
                {
                    string name = message.Substring(1, separator - 1);
                    string password = message.Substring(separator + 1);
                    int index = 0;
                    Ref<Client> client = FindClientByName(name, ref index);
                    if (message[0] == 'r')
                    {
                        if (client == null)
                        {
                            Ref<Client> newClient = new Ref<Client> { value = new Client(name, password, fromAddress) };
                            clients.Add(newClient);
                            Send(newClient.value.socket, newClient.value.remoteAddress, Encoding.UTF8.GetBytes("registered"));
                        }
                        else
                            Send(listener, fromAddress, Encoding.UTF8.GetBytes("rename"));
                    }
                    else if (message[0] == 'l')
                    {
                        if (client == null)
                            Send(listener, fromAddress, Encoding.UTF8.GetBytes("unknown"));
                        else
                        {
                            if (password == client.value.password)
                            {
                                if (ClientConnect(client, fromAddress))
                                    Send(client.value.socket, client.value.remoteAddress, Encoding.UTF8.GetBytes("logged in"));
                                else
                                    Send(listener, fromAddress, Encoding.UTF8.GetBytes("imposter"));
                            }
                            else
                                Send(listener, fromAddress, Encoding.UTF8.GetBytes("wrong"));
                        }
                    }
                }
            }
        }

        for (int index = 0; index < clients.Count; ++index)
        {
            Ref<Client> client = clients[index];
            if (client.value.state == Client.State.DISCONNECTED)
                continue;

            if (client.value.socket.Poll(0, SelectMode.SelectRead))
            {
                EndPoint from = client.value.remoteAddress;
                byte[] received = Receive(client.value.socket, ref from);
                if (received == null)
                {
                    Debug.Log("Server Client " + client.value.name + " disconnected!");
                    ClientDisconnect(client);
                    continue;
                }
                if (received.Length == 0)
                    continue;

                string message = Encoding.UTF8.GetString(received).TrimEnd('\0');
                if (message == "quickmatch")
                {
                    Ref<Lobby> lobby = null;
                    for (int i = 0; i < lobbies.Count; ++i)
                    {
                        lobby = lobbies[i];
                        if (LobbyAddPlayer(lobby, client))
                        {
                            Send(lobby.value.player1.value.socket, lobby.value.player1.value.remoteAddress, Encoding.UTF8.GetBytes("match found"));
                            Send(lobby.value.player2.value.socket, lobby.value.player2.value.remoteAddress, Encoding.UTF8.GetBytes("match found"));
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
                            Send(client.value.socket, client.value.remoteAddress, Encoding.UTF8.GetBytes("matchmaking error"));
                            lobbies.RemoveAt(lobbies.Count - 1);
                        }
                    }
                }
                else
                {
                    Debug.Log(Encoding.UTF8.GetString(received));
                }
            }
        }
    }

    bool Send(Socket socket, EndPoint toAddress, byte[] toSend)
    {
        if (toSend.Length > MAX_BUFFER)
        {
            Debug.Log("Server Send Error: Message larger than " + MAX_BUFFER);
            return false;
        }

        try
        {
            socket.SendTo(toSend, toAddress);
        }
        catch (SocketException error)
        {
            Debug.Log("Server Send Error: " + error.Message);
            return false;
        }
        return true;
    }

    byte[] Receive(Socket socket, ref EndPoint toAddress, bool setRemote = false)
    {
        byte[] recvBuffer = new byte[MAX_BUFFER];
        EndPoint from = new IPEndPoint(IPAddress.None, 0);
        int bytesRecv;

        try
        {
            bytesRecv = socket.ReceiveFrom(recvBuffer, ref from);
        }
        catch (SocketException error)
        {
            Debug.Log("Server Receive Error: " + error.Message);
            return null;
        }

        if (from.ToString() != toAddress.ToString())
            if (setRemote)
                toAddress = from;
            else
            {
                Debug.Log("Server Receive Error: Received a message from an unknown address");
                return new byte[0];
            }

        if (bytesRecv == 1)
        {
            Debug.Log("Server Disconnected");
            return null;
        }

        return recvBuffer;
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
        listener.Close();
        foreach (Ref<Client> client in clients)
            ClientDisconnect(client);
        clients.Clear();
    }
}
