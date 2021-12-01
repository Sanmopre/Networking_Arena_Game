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
    public int port = 0;

    [HideInInspector]
    static public readonly int MAX_BUFFER = 1300;
    [HideInInspector]
    static public readonly byte[] DISCONNECT = new byte[1];
    [HideInInspector]
    static public readonly float MSG_WAIT_TIME = 3.0f;

    Socket thisSocket = null;
    EndPoint remoteAddress = null;
    int messageID = 0;

    // --- Message ---
    struct Message
    {
        public Message(int id, byte[] data)
        {
            this.id = id;
            this.data = data;
            start = Time.realtimeSinceStartup;
        }

        readonly int id;

        public int GetID() { return id; }
        public byte[] data;
        public float start;
    }

    bool MessageIsTimedOut(Ref<Message> message)
    {
        if (Time.realtimeSinceStartup > message.value.start + MSG_WAIT_TIME)
            return true;
        return false;
    }

    void MessageRestartTimer(Ref<Message> message)
    {
        message.value.start = Time.realtimeSinceStartup;
    }
    // --- !Message ---
    List<Ref<Message>> notAcknoledged = new List<Ref<Message>>();

    int lastID = -1;
    List<int> receivedIDs = new List<int>();

    public void Awake()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        IPEndPoint thisAddress = new IPEndPoint(IPAddress.Any, port);

        thisSocket.Bind(thisAddress);

        // --- Shady stuffy ---
        thread = new Thread(SendMessages);
        thread.Start();
    }

    public void Update()
    {
        for (int i = 0; i < notAcknoledged.Count; ++i)
            if (MessageIsTimedOut(notAcknoledged[i]))
            {
                Resend(notAcknoledged[i]);
                MessageRestartTimer(notAcknoledged[i]);
            }
    }

    public void SetRemoteAddress(EndPoint toAddress)
    {
        remoteAddress = toAddress;
    }

    public string Receive(bool setRemote = false)
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
                return "";
            }

        if (bytesRecv == 1)
        {
            ReportError("Disconnected from server");
            return null;
        }

        int idSize = sizeof(int);
        int recvID = BitConverter.ToInt32(inputPacket, 0);

        if (bytesRecv != idSize) // check if it is an acknoledgement package
        {
            Debug.Log(name + " Received Message -> " + recvID);
            if (recvID <= lastID)
            {
                SendAcknowledgement(recvID);
                return "";
            }
            for (int i = 0; i < receivedIDs.Count; ++i)
                if (recvID == receivedIDs[i])
                {
                    SendAcknowledgement(recvID);
                    return "";
                }

            if (receivedIDs.Count > 0)
            {
                for (int i = 0; i < receivedIDs.Count; ++i)
                {
                    if (receivedIDs[i] > recvID)
                    {
                        receivedIDs.Insert(i, recvID);
                        break;
                    }
                    if (i == receivedIDs.Count - 1)
                    {
                        receivedIDs.Add(recvID);
                        break;
                    }
                }
            }
            else
                receivedIDs.Add(recvID);

            int nextID = receivedIDs[0];
            while (nextID - 1 == lastID)
            {
                lastID = nextID;

                receivedIDs.RemoveAt(0);
                if (receivedIDs.Count == 0)
                    break;

                nextID = receivedIDs[0];
            }

            SendAcknowledgement(recvID);
        }
        else
        {
            Debug.Log(name + " Acknowledged -> " + recvID);
            for (int i = 0; i < notAcknoledged.Count; ++i)
                if (notAcknoledged[i].value.GetID() == recvID)
                {
                    notAcknoledged.RemoveAt(i);
                    break;
                }
            return "";
        }

        byte[] toReceive = new byte[inputPacket.Length - idSize];
        for (int i = 0; i < toReceive.Length; ++i)
            toReceive[i] = inputPacket[i + idSize];

        return Encoding.UTF8.GetString(toReceive).TrimEnd('\0');
    }

    public bool Send(string output)
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

        Ref<Message> message = new Ref<Message> { value = new Message(messageID, null) };

        byte[] header = BitConverter.GetBytes(message.value.GetID());
        byte[] toSend = new byte[header.Length + outputPacket.Length];
        header.CopyTo(toSend, 0);
        outputPacket.CopyTo(toSend, header.Length);

        message.value.data = toSend;

        Debug.Log(name + " Sent Message -> " + message.value.GetID());
        try
        {
            SendMessage(toSend, remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Send Error: " + error.Message);
            return false;
        }

        notAcknoledged.Add(message);
        messageID++;

        return true;
    }

    bool SendAcknowledgement(int id)
    {
        if (remoteAddress == null)
        {
            ReportError("Networking Send Acknowledgement Error: Remote Address is null");
            return false;
        }

        Debug.Log(name + " Sent Acknowledgement -> " + id);
        try
        {
            SendMessage(BitConverter.GetBytes(id), remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Send Acknowledgement Error: " + error.Message);
            return false;
        }

        return true;
    }

    bool Resend(Ref<Message> outputMessage)
    {
        if (remoteAddress == null)
        {
            ReportError("Networking Resend Error: Remote Address is null");
            return false;
        }

        Debug.Log(name + " Resent -> " + outputMessage.value.GetID());
        try
        {
            SendMessage(outputMessage.value.data, remoteAddress);
        }
        catch (SocketException error)
        {
            ReportError("Networking Resend Error: " + error.Message);
            return false;
        }

        return true;
    }

    public bool CanReceive()
    {
        if (thisSocket.Poll(0, SelectMode.SelectRead))
            return true;
        return false;
    }

    public bool CanSend()
    {
        if (thisSocket.Poll(0, SelectMode.SelectWrite))
            return true;
        return false;
    }

    public void Close()
    {
        if (thisSocket != null)
            thisSocket.Close();
    }

    void ReportError(string error)
    {
        Debug.Log(error);
    }

    private void OnDestroy()
    {
        Close();

        // --- Shady stuffy ---
        thread.Abort();
    }

    // --- Shady stuffy ---

    public bool jitter = true;
    public bool packetLoss = true;
    public int minJitt = 0;
    public int maxJitt = 800;
    public int lossThreshold = 90;

    static readonly object myLock = new object();
    Thread thread = null;

    public struct Packet
    {
        public byte[] packet;
        public DateTime time;
        public UInt32 id;
        public EndPoint ip;
    }
    public List<Packet> packetBuffer = new List<Packet>();

    void SendMessage(byte[] text, EndPoint ip)
    {
        System.Random ran = new System.Random();
        if (((ran.Next(0, 100) > lossThreshold) && packetLoss) || !packetLoss) // Don't schedule the message with certain probability
        {
            Packet pack = new Packet();
            pack.packet = text;

            if (jitter)
                pack.time = DateTime.Now.AddMilliseconds(ran.Next(minJitt, maxJitt)); // delay the message sending according to parameters
            else
                pack.time = DateTime.Now;

            pack.id = 0;
            pack.ip = ip;
            lock (myLock)
                packetBuffer.Add(pack);
        }
        else
            Debug.Log("Package Lost BOOHOO");
    }

    //Run this always in a separate Thread, to send the delayed messages
    void SendMessages()
    {
        while (true)
        {
            DateTime date = DateTime.Now;
            int i = 0;
            if (packetBuffer.Count > 0)
            {
                List<Packet> auxBuffer;
                lock (myLock)
                    auxBuffer = new List<Packet>(packetBuffer);

                foreach (var pack in auxBuffer)
                {
                    if (pack.time < date)
                    {
                        thisSocket.SendTo(pack.packet, pack.packet.Length, SocketFlags.None, pack.ip);
                        lock (myLock)
                            packetBuffer.RemoveAt(i);

                        i--;
                    }
                    i++;
                }
            }
        }
    }
}
