using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

public class Networking : MonoBehaviour
{
    struct Message
    {
        public Message(int id, byte[] data)
        {
            this.id = id;
            this.data = data;
            startTime = Time.realtimeSinceStartup;
        }

        public bool IsTimedOut()
        {
            if (Time.realtimeSinceStartup > startTime + 2)
                return true;
            return false;
        }

        public int id;
        public byte[] data;
        float startTime;
    }

    Socket thisSocket = null;
    IPEndPoint thisAddress = null;

    EndPoint remoteAddress = null;

    int messageID = 0;
    int lastRecvID = 0;

    List<Message> toConfirm = new List<Message>();
    

    public void Start()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        thisAddress = new IPEndPoint(IPAddress.Any, 0);

        thisSocket.Bind(thisAddress);
    }

    public void Update()
    {
        if(thisSocket.Poll(0, SelectMode.SelectRead))
        {
            byte[] recvBuffer = new byte[1500];
            EndPoint recvIP = OnPacketReceived(ref recvBuffer);
            if (recvIP != null)
            {
                if (remoteAddress == null)
                    recvIP = remoteAddress;
                else if (remoteAddress != recvIP)
                {
                    // dunno, do sum
                }
                // Send to GameManager
            }
        }

        for (int i = 0; i < toConfirm.Count; ++i)
            if (toConfirm[i].IsTimedOut())
            {
                SendPacket(toConfirm[i].data);
                toConfirm.RemoveAt(i);
                --i;
                continue;
            }
    }

    void StartConnection(EndPoint toAddress)
    {
        if (remoteAddress != null)
            Disconnect();
        remoteAddress = toAddress;
    }

    EndPoint OnPacketReceived(ref byte[] inputPacket)
    {
        EndPoint from = new IPEndPoint(IPAddress.None, 0);

        int bytesRecv = 0;
        try
        {
            bytesRecv = thisSocket.ReceiveFrom(inputPacket, ref from);
        }
        catch (SocketException error)
        {
            ReportError("Networking Receive Error: " + error.Message);
        }
        if (bytesRecv == 0)
        {
            Disconnect();
            return null;
        }

        int iSize = sizeof(int);
        int recvID = BitConverter.ToInt32(inputPacket, 0);
        if (bytesRecv == iSize)
        {
            for (int i = 0; i < toConfirm.Count; ++i)
                if (toConfirm[i].id == recvID)
                {
                    toConfirm.RemoveAt(i);
                    break;
                }
            return null;
        }

        lastRecvID = recvID;
        byte[] toReceive = new byte[inputPacket.Length - iSize];
        for (int i = 0; i < toReceive.Length; ++i)
            toReceive[i] = inputPacket[i + iSize];

        inputPacket = toReceive;
        return from;
    }

    void SendPacket(byte[] outputPacket)
    {
        if (remoteAddress == null)
        {
            ReportError("Networking Send Error: Remote Address is null");
            return;
        }

        byte[] header = BitConverter.GetBytes(messageID);
        byte[] toSend = new byte[header.Length + outputPacket.Length];
        header.CopyTo(toSend, 0);
        outputPacket.CopyTo(toSend, header.Length);

        try
        {
            thisSocket.SendTo(toSend, remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Send Error: " + error.Message);
            Disconnect();
            return;
        }

        toConfirm.Add(new Message(messageID, outputPacket));
        ++messageID;
    }

    bool IsConnected()
    {
        if (remoteAddress == null)
            return false;
        return true;
    }

    void Disconnect()
    {
        if (remoteAddress == null)
            return;

        SendPacket(new byte[0]);
        remoteAddress = null;

        messageID = 0;
        lastRecvID = 0;
    }

    void ReportError(string error)
    {
        Debug.Log(error);
    }

    private void OnDestroy()
    {
        Disconnect();
        thisSocket.Close();
    }
}
