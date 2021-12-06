using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Text;

public class Test : MonoBehaviour
{
    readonly EndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6969);
    readonly EndPoint nullAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6970);

    UDP socket = null;

    public bool server = false;

    //string myName = "Server";
    string message = "FromServer";

    void Start()
    {
        //socket = gameObject.GetComponent<UDP>();
        //
        //if (!server)
        //{
        //    //myName = "Client";
        //    message = "FromClient";
        //    socket.SetRemoteAddress(serverAddress);
        //}
        //else
        //    socket.SetRemoteAddress(nullAddress);
    }
    //int messagesSent = 0;
    public int messagesToSend = 25;
    void Update()
    {
        //while (socket.CanReceive())
        //{
        //    string recv = socket.Receive(true);
        //    if (recv == null)
        //    {
        //        Debug.Log(myName + " receive Error");
        //        continue;
        //    }
        //    if (recv.Length == 0)
        //        continue;
        //}
        //if (messagesSent < messagesToSend)
        //{
        //    socket.Send(message);
        //    ++messagesSent;
        //}
    }

    private void OnDestroy()
    {
        socket.Close();
    }

    IEnumerator SendDelay()
    {
        yield return new WaitForSeconds(1.0f);
        socket.Send(message);
    }
}
