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

    Socket listener = null;

    class Client
    {
        private Client() { }

        public Client(string name, string password, EndPoint remoteAddress)
        {
            this.name = name;
            this.password = password;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            this.remoteAddress = remoteAddress;

            state = State.UNINTERESTED;
        }

        ~Client()
        {
            Disconnect();
        }

        public string name = null;
        public string password = null;
        public int lobbyID = 0;

        public enum State
        {
            DISCONNECTED,
            UNINTERESTED,
            INTERESTED,
            IN_GAME
        }
        State state = State.DISCONNECTED;
        public State GetState() { return state; }

        Socket socket = null;
        public Socket GetSocket() { return socket; }
        EndPoint remoteAddress = null;
        public EndPoint GetRemoteAddress() { return remoteAddress; }

        public void Connect(EndPoint remoteAddress)
        {
            if (state != State.DISCONNECTED)
                return;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Blocking = false;
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            this.remoteAddress = remoteAddress;
            state = State.UNINTERESTED;
        }
        public void Disconnect()
        {
            if (state == State.DISCONNECTED)
                return;

            socket.Close();
            socket = null;
            remoteAddress = null;
            state = State.DISCONNECTED;
        }
    }

    List<Client> clients = new List<Client>();
    List<int> lobbies = new List<int>();

    void Start()
    {
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        listener.Blocking = false;
        listener.Bind(new IPEndPoint(IPAddress.Any, 6969));
    }

    void Update()
    {
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
                    Client client = FindClientByName(name, ref index);
                    if (message[0] == 'r')
                    {
                        if (client == null)
                        {
                            Client newClient = new Client(name, password, fromAddress);
                            clients.Add(newClient);
                            Send(newClient.GetSocket(), newClient.GetRemoteAddress(), Encoding.UTF8.GetBytes("registered"));
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
                            if (password == client.password)
                            {
                                if (client.GetState() == Client.State.DISCONNECTED)
                                {
                                    client.Connect(fromAddress);
                                    Send(client.GetSocket(), client.GetRemoteAddress(), Encoding.UTF8.GetBytes("logged in"));
                                    clients[index] = client;
                                }
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
            Client client = clients[index];
            if (client.GetSocket().Poll(0, SelectMode.SelectRead))
            {
                EndPoint from = client.GetRemoteAddress();
                byte[] received = Receive(client.GetSocket(), ref from);
                if (received == null)
                {
                    Send(client.GetSocket(), client.GetRemoteAddress(), new byte[0]);
                    client.Disconnect();
                    clients[index] = client;
                    continue;
                }
                if (received.Length == 0)
                    continue;

                string message = Encoding.UTF8.GetString(received);
                if (message == "quickmatch")
                {
                    // TODO: MatchMaking
                }
                Debug.Log(Encoding.UTF8.GetString(received));
            }
        }
    }

    bool Send(Socket socket, EndPoint toAddress, byte[] toSend)
    {
        if (toSend.Length > MAX_BUFFER)
        {
            Debug.Log("Client Send Error: Message larger than " + MAX_BUFFER);
            return false;
        }

        try
        {
            socket.SendTo(toSend, toAddress);
        }
        catch (SocketException error)
        {
            Debug.Log("Client Send Error: " + error.Message);
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
            Debug.Log("Client Receive Error: " + error.Message);
            return null;
        }

        if (from != toAddress)
            if (setRemote)
                toAddress = from;
            else
            {
                Debug.Log("Client Receive Error: Received a message from an unknown address");
                return new byte[0];
            }

        if (bytesRecv == 0)
        {
            Debug.Log("Server Disconnected");
            return null;
        }

        return recvBuffer;
    }

    Client FindClientByName(string name, ref int index)
    {
        for (int i = 0; i < clients.Count; ++i)
            if (clients[i].name == name)
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
        clients.Clear();
    }
}
