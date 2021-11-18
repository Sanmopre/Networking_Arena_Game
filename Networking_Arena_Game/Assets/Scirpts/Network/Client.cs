using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client : MonoBehaviour
{
    const int MAX_BUFFER = 1300;
    EndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969);

    Socket toServer = null;
    EndPoint remoteAddress = null;

    public bool toRegister;
    public string username;
    public string password;

    enum SetUpState
    {
        SET_ID_DATA,
        SEND_ID_DATA,
        RECV_ID_CONF,
        SET_REMOTE,
        CONNECTED,
        DISCONNECTED
    }
    SetUpState setUp = SetUpState.SEND_ID_DATA;

    void Start()
    {
        toServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        toServer.Blocking = false;
        toServer.Bind(new IPEndPoint(IPAddress.Any, 0));

        remoteAddress = serverAddress;
    }

    void Update()
    {
        switch (setUp)
        {
            case SetUpState.SET_ID_DATA:
                setUp = SetUpState.SEND_ID_DATA;
                break;
            case SetUpState.SEND_ID_DATA:
                char type = 'l';
                if (toRegister)
                    type = 'r';
                if (Send(Encoding.UTF8.GetBytes(type + username + " " + password)))
                    setUp = SetUpState.RECV_ID_CONF;
                break;
            case SetUpState.RECV_ID_CONF:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive(true);
                    if (received == null)
                    {
                        setUp = SetUpState.DISCONNECTED;
                        break;
                    }

                    string message = Encoding.UTF8.GetString(received);
                    if (message == "registered" || message == "logged in")
                    {
                        Debug.Log("Succesfully " + message + "!");
                        setUp = SetUpState.CONNECTED;
                    }
                    else
                    {
                        if (message == "rename")
                        {
                            Debug.Log("The name you chose to register is already in use.");
                            setUp = SetUpState.SET_ID_DATA;
                        }
                        else if (message == "unknown")
                        {
                            Debug.Log("The username does not exist. If you are new go register!");
                            setUp = SetUpState.SET_ID_DATA;
                        }
                        else if (message == "wrong")
                        {
                            Debug.Log("Wrong password.");
                            setUp = SetUpState.SET_ID_DATA;
                        }
                        else
                        {

                        }
                    }
                }
                break;
            case SetUpState.CONNECTED:
                if (toServer.Poll(0, SelectMode.SelectRead))
                {
                    byte[] received = Receive();
                    if (received == null)
                    {
                        setUp = SetUpState.DISCONNECTED;
                        break;
                    }
                    if (received.Length == 0)
                        break;

                    Debug.Log(Encoding.UTF8.GetString(received));
                }
                break;
            case SetUpState.DISCONNECTED:
                break;
        }
    }

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
        int bytesRecv = 0;

        try
        {
            bytesRecv = toServer.ReceiveFrom(recvBuffer, ref from);
        }
        catch (SocketException error)
        {
            Debug.Log("Client Receive Error: " + error.Message);
            return null;
        }

        if (from != remoteAddress)
            if (setRemote)
                remoteAddress = from;
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

    // TODO: OnDestroy() and testing
}
