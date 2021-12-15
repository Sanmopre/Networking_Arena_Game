using System.Collections;
using System.Collections.Generic;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using UnityEngine;

public class UDP : MonoBehaviour
{
    // TODO: ADD PING SYSTEM TO DETECT DISCONNECTIONS AUTOMATICALLY (POOOG)
    public int port = 0;

    [HideInInspector]
    static public readonly int MAX_BUFFER = 1300;
    [HideInInspector]
    static public readonly int MAX_RECV_IDS = 100;
    [HideInInspector]
    static public readonly byte[] DISCONNECT = BitConverter.GetBytes(-1);
    [HideInInspector]
    static public readonly float MSG_WAIT_TIME = 3.0f;
    [HideInInspector]
    static public readonly float SEND_RATE = 0.008f;
    [HideInInspector]
    static public readonly byte MESSAGE_SEPARATOR = Encoding.UTF8.GetBytes("\\")[0];

    Socket thisSocket = null;
    public EndPoint remoteAddress = null;
    int messageID = 0;
    public bool listenMode = false;

    float roundTrip = 0.0f;
    public float GetRoundTripTime() { return roundTrip; } // to jordi: mad? what r u gonna do? cry about it? boohoo

    public string BindAddressStr() { return thisSocket.LocalEndPoint.ToString(); }
    public string RemoteAddressStr() { return remoteAddress.ToString(); }

    // --- Recv ---
    public enum RecvType
    {
        MESSAGE,
        FIN,
        FIN_START,
        FIN_END,
        EMPTY,
        ERROR
    }
    struct RecvPacket
    {
        public RecvPacket(int id, RecvType type, byte[] message, EndPoint from)
        {
            this.id = id;
            this.type = type;
            this.message = message;
            this.from = from;
        }

        public readonly int id;
        public RecvType type;
        public byte[] message;
        public EndPoint from;
    }
    List<RecvPacket> recvPackets = new List<RecvPacket>();
    // --- !Recv ---

    // --- Send ---
    struct SendPacket
    {
        public SendPacket(byte[] data, EndPoint to, Ref<Message> message)
        {
            this.data = data;
            this.to = to;
            this.message = message;
        }

        public byte[] data;
        public EndPoint to;
        public Ref<Message> message;
    }

    void AddSendPacket(byte[] data, EndPoint to, Ref<Message> message)
    {
        currentBufferSize += data.Length + 1; // + 1 because of the separator
        if (currentBufferSize >= MAX_BUFFER)
        {
            SendPackets();
            currentBufferSize += data.Length + 1;
        }

        sendBuffer.Add(new SendPacket(data, to, message));
    }

    List<SendPacket> sendBuffer = new List<SendPacket>();
    int currentBufferSize = 0;
    float timeLastSent = 0.0f;
    // --- !Send ---

    // --- Message ---
    struct Message
    {
        public Message(int id, byte[] data, EndPoint from)
        {
            this.id = id;
            this.data = data;
            start = -1.0f;
            this.from = from;
        }

        readonly int id;

        public int GetID() { return id; }
        public byte[] data;
        public float start;

        public EndPoint from;
    }

    bool MessageIsTimedOut(Ref<Message> message)
    {
        if (message.value.start < 0)
            return false;
        if (Time.realtimeSinceStartup > message.value.start + MSG_WAIT_TIME)
            return true;
        return false;
    }

    void MessageRestartTimer(Ref<Message> message)
    {
        message.value.start = Time.realtimeSinceStartup;
    }

    public bool MessageIsAcknowleged(int id)
    {
        foreach(Ref<Message> message in notAcknowleged)
            if (message.value.GetID() == id)
                return false;
        return true;
    }

    Ref<Message> GetMessage(int id)
    {
        foreach (Ref<Message> message in notAcknowleged)
            if (message.value.GetID() == id)
                return message;
        return null;
    }

    List<Ref<Message>> notAcknowleged = new List<Ref<Message>>();
    int lastID = -1;
    List<int> receivedIDs = new List<int>();
    // --- !Message ---

    public void Awake()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        IPEndPoint thisAddress = new IPEndPoint(IPAddress.Any, port);
        thisSocket.Bind(thisAddress);

        remoteAddress = new IPEndPoint(IPAddress.None, 0);

        // --- Shady stuffy ---
        thread = new Thread(SendMessages);
        thread.Start();
    }

    public void Update()
    {
        if (sendBuffer.Count > 0 && Time.realtimeSinceStartup >= timeLastSent + SEND_RATE)
            SendPackets();

        while (thisSocket.Poll(0, SelectMode.SelectRead))
            ReceivePackets();

        if (!listenMode)
            for (int i = 0; i < notAcknowleged.Count; ++i)
                if (MessageIsTimedOut(notAcknowleged[i]))
                {
                    Resend(notAcknowleged[i]);
                    MessageRestartTimer(notAcknowleged[i]);
                }
    }

    public RecvType Receive(ref string message, bool setRemote = false)
    {
        byte[] inputPacket = null;
        RecvType output =  Receive(ref inputPacket, setRemote);
        if (inputPacket != null)
            message = Encoding.UTF8.GetString(inputPacket);
        return output;
    }

    public RecvType Receive(ref byte[] message, bool setRemote = false)
    {
        if (recvPackets.Count == 0)
            return RecvType.EMPTY;

        RecvPacket packet = recvPackets[0];
        recvPackets.RemoveAt(0);

        if (packet.from.ToString() != remoteAddress.ToString())
            if (setRemote)
            {
                Close();
                remoteAddress = packet.from;
            }
            else
            {
                ReportError("UDP_" + name + " Receive Error: Received a message from an unknown address");
                message = Encoding.UTF8.GetBytes("unknown address");
                return RecvType.EMPTY;
            }

        if (packet.type == RecvType.MESSAGE || packet.type == RecvType.FIN_START)
        {
            SendAcknowledgement(packet.id, packet.from);

            if (!listenMode)
            {
                if (packet.id <= lastID)
                {
                    Debug.Log(name + " already received: " + Encoding.UTF8.GetString(packet.message));
                    return RecvType.EMPTY;
                }
                for (int i = 0; i < receivedIDs.Count; ++i)
                    if (packet.id == receivedIDs[i])
                    {
                        Debug.Log(name + " already received: " + Encoding.UTF8.GetString(packet.message));
                        return RecvType.EMPTY;
                    }

                if (receivedIDs.Count > 0)
                {
                    for (int i = 0; i < receivedIDs.Count; ++i)
                    {
                        if (receivedIDs[i] > packet.id)
                        {
                            receivedIDs.Insert(i, packet.id);
                            break;
                        }
                        if (i == receivedIDs.Count - 1)
                        {
                            receivedIDs.Add(packet.id);
                            break;
                        }
                    }
                }
                else
                    receivedIDs.Add(packet.id);

                while (receivedIDs.Count > MAX_RECV_IDS)
                {
                    lastID = receivedIDs[0];
                    receivedIDs.RemoveAt(0);
                }

                int nextID = receivedIDs[0];
                while (nextID - 1 == lastID)
                {
                    lastID = nextID;

                    receivedIDs.RemoveAt(0);
                    if (receivedIDs.Count == 0)
                        break;

                    nextID = receivedIDs[0];
                }
            }
        }

        if (packet.type == RecvType.FIN_START || packet.type == RecvType.FIN_END)
            packet.type = RecvType.FIN;

        message = packet.message;
        return packet.type;
    }

    void ReceivePackets()
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
            ReportError("UDP_" + name + " Receive Error: " + error.Message);
            recvPackets.Add(new RecvPacket(-1, RecvType.ERROR, null, from));
            return;
        }

        inputPacket = ByteArray.TrimEnd(inputPacket);
        OutputStream stream = new OutputStream(inputPacket);
        List<byte[]> messages = new List<byte[]>();

        while (!stream.ReachedEnd())
        {
            int size = stream.GetInt();
            if (size < 0)
                break;
            messages.Add(stream.GetBytes(size));
        }

        for (int m = 0; m < messages.Count; ++m)
        {
            byte[] message = messages[m];

            int idSize = sizeof(int);
            int recvID;
            try
            {
                recvID = BitConverter.ToInt32(message, 0);
            }
            catch
            {
                recvID = -1;
                byte[] p = inputPacket;
                for (int a = 0; a < p.Length; ++a)
                {
                    if (p[a] == '\0')
                        p.SetValue(Encoding.UTF8.GetBytes("X")[0], a);
                }
                Debug.Log("Server Client " + name + " message procesing ERROR -> Current Message num: " + m + " | Current Message Lenght: " + message.Length);
                Debug.Log("Server Client " + name + " message procesing ERROR -> Packet Received: " + Encoding.UTF8.GetString(p));
                if (message.Length == 0)
                    return;
            }

            if (message.Length != idSize) // check if it is an acknoledgement package
            {
                ReportError("UDP_" + name + " Received Message -> " + recvID);
            }
            else
            {
                ReportError("UDP_" + name + " Acknowledged -> " + recvID);
                bool exit = false;
                for (int i = 0; i < notAcknowleged.Count; ++i)
                    if (notAcknowleged[i].value.GetID() == recvID)
                    {
                        try
                        {
                            if (ByteArray.Compare(notAcknowleged[i].value.data, DISCONNECT, idSize))
                                recvPackets.Add(new RecvPacket(recvID, RecvType.FIN_END, notAcknowleged[i].value.data, from));
                        }
                        catch 
                        {
                            for (int a = 0; a < notAcknowleged[i].value.data.Length; ++a)
                            {
                                if (notAcknowleged[i].value.data[a] == '\0')
                                    notAcknowleged[i].value.data.SetValue(Encoding.UTF8.GetBytes("X")[0], a);
                            }
                            Debug.Log("Server Client " + name + " aknowledgemet ERROR " + Encoding.UTF8.GetString(notAcknowleged[i].value.data));
                        }

                        roundTrip = Time.realtimeSinceStartup - notAcknowleged[i].value.start;
                        notAcknowleged.RemoveAt(i);
                        exit = true;
                        break;
                    }
                if (exit)
                    continue;
            }

            byte[] toReceive = new byte[message.Length - idSize];
            for (int i = 0; i < toReceive.Length; ++i)
                toReceive[i] = message[i + idSize];
            
            if (ByteArray.Compare(toReceive, DISCONNECT))
            {
                recvPackets.Add(new RecvPacket(recvID, RecvType.FIN_START, toReceive, from));
                continue;
            }
            
            toReceive = ByteArray.TrimEnd(toReceive);
            recvPackets.Add(new RecvPacket(recvID, RecvType.MESSAGE, toReceive, from));
        }
    }

    public int Send(string outputPacket)
    {
        return Send(Encoding.UTF8.GetBytes(outputPacket));
    }

    public int Send(byte[] outputPacket)
    {
        if (outputPacket != null)
            outputPacket = ByteArray.TrimEnd(outputPacket);
        else
            outputPacket = DISCONNECT;

        if (outputPacket.Length > MAX_BUFFER)
        {
            ReportError("UDP_" + name + "Send Error: Message larger than " + MAX_BUFFER);
            return -1;
        }
        if (remoteAddress == null)
        {
            ReportError("UDP_" + name + " Send Error: Remote Address is null");
            return -1;
        }

        Ref<Message> message = new Ref<Message> { value = new Message(messageID, null, remoteAddress) };

        byte[] header = BitConverter.GetBytes(message.value.GetID());
        byte[] toSend = new byte[header.Length + outputPacket.Length];
        header.CopyTo(toSend, 0);
        outputPacket.CopyTo(toSend, header.Length);

        message.value.data = toSend;

        ReportError("UDP_" + name  + " Sent Message -> " + message.value.GetID());
        AddSendPacket(toSend, remoteAddress, message);

        if (!listenMode)
            notAcknowleged.Add(message);
        ++messageID;

        return message.value.GetID();
    }

    bool SendAcknowledgement(int id, EndPoint to)
    {
        if (remoteAddress == null)
        {
            ReportError("UDP_" + name + " Send Acknowledgement Error: Remote Address is null");
            return false;
        }
        
        ReportError("UDP_" + name + " Sent Acknowledgement -> " + id);
        AddSendPacket(BitConverter.GetBytes(id), to, null);

        return true;
    }

    bool Resend(Ref<Message> outputMessage)
    {
        if (remoteAddress == null)
        {
            ReportError("UDP_" + name + " Resend Error: Remote Address is null");
            return false;
        }
        
        ReportError("UDP_" + name + " Resent -> " + outputMessage.value.GetID());
        AddSendPacket(outputMessage.value.data, outputMessage.value.from, outputMessage);

        return true;
    }

    void SendPackets()
    {
        InputStream stream;
        while (sendBuffer.Count != 0)
        {
            stream = new InputStream();
            EndPoint currentRemote = sendBuffer[0].to;
            for (int i = 0; i < sendBuffer.Count; ++i)
            {
                SendPacket packet = sendBuffer[i];
                if (currentRemote.ToString() == packet.to.ToString())
                {
                    stream.AddInt(packet.data.Length);
                    stream.AddBytes(packet.data);

                    if (packet.message != null)
                        MessageRestartTimer(packet.message);

                    sendBuffer.RemoveAt(i);
                    --i;
                }
            }

            SendMessage(stream.GetBuffer(true), currentRemote);
        }

        currentBufferSize = 0;
        timeLastSent = Time.realtimeSinceStartup;
    }

    public bool CanReceive()
    {
        if (recvPackets.Count != 0)
            return true;
        return false;
    }

    public void Close()
    {
        notAcknowleged.Clear();
        receivedIDs.Clear();
        lastID = -1;
        messageID = 0;

        remoteAddress = new IPEndPoint(IPAddress.None, 0);
    }

    void ReportError(string error)
    {
        //Debug.Log(error);
    }

    private void OnDestroy()
    {
        Close();

        if (thisSocket != null)
            thisSocket.Close();

        // --- Shady stuffy ---
        thread.Abort();
    }

    // --- Shady stuffy ---

    bool jitter = true;
    bool packetLoss = false;
    int minJitt = 10;
    int maxJitt = 70;
    int lossThreshold = 50;

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
            ReportError("UDP_" + name + "Package Lost BOOHOO");
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
