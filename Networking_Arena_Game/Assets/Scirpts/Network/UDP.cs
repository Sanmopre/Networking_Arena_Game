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
    static public readonly int MAX_BUFFER = 1300;
    [HideInInspector]
    static public readonly byte[] DISCONNECT = new byte[1];
    [HideInInspector]
    static public readonly float MSG_WAIT_TIME = 2.0f;

    Socket thisSocket = null;
    EndPoint remoteAddress = null;
    int messageID = 0;

    struct Message
    {
        public Message(int id, byte[] data)
        {
            this.id = id;
            this.data = data;
            start = Time.realtimeSinceStartup;
        }

        public bool IsTimedOut()
        {
            if (Time.realtimeSinceStartup > start + MSG_WAIT_TIME)
                return true;
            return false;
        }

        public void RestartTimer()
        {
            start = Time.realtimeSinceStartup;
        }

        readonly int id;
        public int GetID() { return id; }
        public byte[] data;
        float start;
    }
    List<Message> notAcknoledged = new List<Message>();
    int allReceivedLast = -1;
    List<Message> alreadyReceived = new List<Message>();

    public void Start()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        IPEndPoint thisAddress = new IPEndPoint(IPAddress.Any, 0);

        thisSocket.Bind(thisAddress);

        // ---

        alreadyReceived.Add(new Message(-2, null));
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

        for (int i = 1; i < alreadyReceived.Count; ++i)
            if (alreadyReceived[i].IsTimedOut())
            {
                Resend(alreadyReceived[i]);
                alreadyReceived.RemoveAt(i);
                --i;
                continue;
            }
    }

    public void SetRemoteAddress(EndPoint toAddress)
    {
        remoteAddress = toAddress;
    }

    public byte[] Receive(bool setRemote = false)
    {
        byte[] inputPacket = new byte[MAX_BUFFER];
        EndPoint from = new IPEndPoint(IPAddress.None, 0);

        int bytesRecv;
        try
        {
            bytesRecv = thisSocket.ReceiveFrom(inputPacket, ref from);
        }
        catch (SocketException error)
        {
            ReportError("Receive Error: " + error.Message);
            return null;
        }

        if (from.ToString() != remoteAddress.ToString())
            if (setRemote)
                remoteAddress = from;
            else
            {
                ReportError("Receive Error: Received a message from an unknown address");
                return new byte[0];
            }

        if (bytesRecv == 1)
        {
            ReportError("Disconnected from server");
            return null;
        }

        int idSize = sizeof(int);
        int recvID = BitConverter.ToInt32(inputPacket, idSize);

        if (bytesRecv != idSize) // check if it is an acknoledgement package
        {
            if (recvID <= allReceivedLast)
                return new byte[0];
            for (int i = 1; i < alreadyReceived.Count; ++i)
            {
                if (recvID == alreadyReceived[i].GetID())
                {
                    alreadyReceived[i].RestartTimer();
                    return new byte[0];
                }
            }

            if (alreadyReceived.Count > 1)
            {
                for (int i = 1; i < alreadyReceived.Count; ++i)
                {
                    if (alreadyReceived[i].GetID() > recvID)
                    {
                        alreadyReceived.Insert(i - 1, new Message(recvID, null));
                        break;
                    }
                }
            }
            else
                alreadyReceived.Add(new Message(recvID, null));

            int nextID = recvID;
            while (nextID - 1 == allReceivedLast)
            {
                allReceivedLast = nextID;

                alreadyReceived.RemoveAt(1);
                if (alreadyReceived.Count == 1)
                    break;

                nextID = alreadyReceived[1].GetID();
            }

            Send(Encoding.UTF8.GetString(BitConverter.GetBytes(recvID)), true);
        }
        else
        {
            for (int i = 0; i < notAcknoledged.Count; ++i)
                if (notAcknoledged[i].GetID() == recvID)
                {
                    notAcknoledged.RemoveAt(i);
                    break;
                }
            return null;
        }

        byte[] toReceive = new byte[inputPacket.Length - idSize];
        for (int i = 0; i < toReceive.Length; ++i)
            toReceive[i] = inputPacket[i + idSize];

        return toReceive;
    }

    public bool Send(string output, bool acknoledgePacket = false)
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
        Message message = new Message(messageID, outputPacket);

        byte[] toSend;
        if (!acknoledgePacket)
        {
            byte[] header = BitConverter.GetBytes(message.GetID());
            toSend = new byte[header.Length + outputPacket.Length];
            header.CopyTo(toSend, 0);
            outputPacket.CopyTo(toSend, header.Length);
        }
        else
            toSend = outputPacket;

        try
        {
            thisSocket.SendTo(toSend, remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Send Error: " + error.Message);
            return false;
        }

        if (!acknoledgePacket)
        {
            notAcknoledged.Add(message);
            messageID++;
        }
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

        outputMessage.RestartTimer();
        notAcknoledged.Add(outputMessage);
        return true;
    }

    public void Close()
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
