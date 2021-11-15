using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using UnityEngine;

public class Networking : MonoBehaviour
{
    Socket thisSocket = null;
    IPEndPoint thisAddress = null;
    public void Start()
    {
        thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        thisSocket.Blocking = false;
        thisAddress = new IPEndPoint(IPAddress.Any, 0);

        thisSocket.Bind(thisAddress);
    }

    void onPacketReceived(byte[] inputPacket, Socket fromAddress)
    {

    }

    public void Update()
    {

    }

    void onConnectionReset(Socket fromAddress)
    {

    }

    void sendPacket(byte[] outputPacket, Socket toAddress)
    {
        int bytesSent = thisSocket.SendTo(outputPacket, toAddress.LocalEndPoint);
        if (bytesSent < outputPacket.Length)
            Debug.Log("Networking Error: Couldn't send all the data");
    }

    void onDisconnect()
    {

    }

    void reportError()
    {

    }
}
