using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

public class UDP : MonoBehaviour
{
    [HideInInspector]
    public readonly int MAX_BUFFER = 1300;
    [HideInInspector]
    public readonly byte[] DISCONNECT = new byte[1];

    Socket thisSocket = null;
    EndPoint remoteAddress = null;

    struct Message
    {
        public Message(byte[] data)
        {
            id = Time.realtimeSinceStartup;
            this.data = data;
        }

        public bool IsTimedOut()
        {
            if (Time.realtimeSinceStartup > id + 2)
                return true;
            return false;
        }

        float id;
        public float GetID() { return id; }
        public byte[] data;
    }
    List<Message> notAcknoledged = new List<Message>();

    public void Start()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        IPEndPoint thisAddress = new IPEndPoint(IPAddress.Any, 0);

        thisSocket.Bind(thisAddress);
    }

    public void Update()
    {
        for (int i = 0; i < notAcknoledged.Count; ++i)
            if (notAcknoledged[i].IsTimedOut())
            {
                Resend(notAcknoledged[i]);
                notAcknoledged.RemoveAt(i);
                --i;
                continue;
            }
    }

    void SetRemoteAddress(EndPoint toAddress)
    {
        remoteAddress = toAddress;
    }

    EndPoint Receive(ref byte[] inputPacket) // TODO: REMAKE
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
            return null;
        }

        int iSize = sizeof(int);
        int recvID = BitConverter.ToInt32(inputPacket, 0);
        if (bytesRecv == iSize)
        {
            for (int i = 0; i < notAcknoledged.Count; ++i)
                if (notAcknoledged[i].GetID() == recvID)
                {
                    notAcknoledged.RemoveAt(i);
                    break;
                }
            return null;
        }

        //lastRecvID = recvID;
        byte[] toReceive = new byte[inputPacket.Length - iSize];
        for (int i = 0; i < toReceive.Length; ++i)
            toReceive[i] = inputPacket[i + iSize];

        inputPacket = toReceive;
        return from;
    }

    bool Send(string output)
    {
        if (output.Length > MAX_BUFFER)
        {
            Debug.Log("Client Send Error: Message larger than " + MAX_BUFFER);
            return false;
        }
        if (remoteAddress == null)
        {
            ReportError("Networking Send Error: Remote Address is null");
            return false;
        }

        byte[] outputPacket;
        if (output != null)
        {
            output.TrimEnd('\0');
            outputPacket = Encoding.UTF8.GetBytes(output);
        }
        else
            outputPacket = DISCONNECT;
        Message message = new Message(outputPacket);

        byte[] header = BitConverter.GetBytes(message.GetID());
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
            return false;
        }

        notAcknoledged.Add(message);
        return true;
    }

    bool Resend(Message outputMessage)
    {
        if (remoteAddress == null)
        {
            ReportError("Networking Send Error: Remote Address is null");
            return false;
        }

        try
        {
            thisSocket.SendTo(outputMessage.data, remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Send Error: " + error.Message);
            return false;
        }

        notAcknoledged.Add(outputMessage);
        return true;
    }

    void Close()
    {
        thisSocket.Close();
    }

    void ReportError(string error)
    {
        Debug.Log(error);
    }

    private void OnDestroy()
    {
        thisSocket.Close();
    }
}
