using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public class Ref<T> where T : struct
    {
        public T value;
    }

    struct Client
    {
        public Client(string name, string password)
        {
            this.name = name;
            this.password = password;
            lobbyID = 0;

            state = State.UNINTERESTED;
        }

        public string name;
        public string password;
        public int lobbyID;

        public enum State
        {
            DISCONNECTED,
            UNINTERESTED,
            INTERESTED,
            IN_GAME
        }
        State state;
        public State GetState() { return state; }

        public bool Connect()
        {
            if (state != State.DISCONNECTED)
                return false;

            state = State.UNINTERESTED;

            return true;
        }

        public bool EnterLobby(int lobbyID)
        {
            if (state != State.UNINTERESTED)
                return false;

            this.lobbyID = lobbyID;
            state = State.INTERESTED;

            return true;
        }

        public bool InGame()
        {
            if (state != State.INTERESTED)
                return false;

            state = State.IN_GAME;

            return true;
        }

        public void Disconnect()
        {
            if (state == State.DISCONNECTED)
                return;

            state = State.DISCONNECTED;
        }
    }
    List<Ref<Client>> clients = new List<Ref<Client>>();

    void PrintClients()
    {
        Debug.Log("---");
        foreach (Ref<Client> client in clients)
        {
            Debug.Log(client.value.name + " State: " + client.value.GetState().ToString());
        }
    }

    void Start()
    {
        for (int i = 0; i < 5; ++i)
        {
            clients.Add(new Ref<Client> { value = new Client("Test" + i.ToString(), "Password" + i.ToString()) });
        }

        PrintClients();

        for (int i = 0; i < clients.Count; ++i)
        {
            Ref<Client> client = clients[i];

            if (i % 2 == 0)
            {
                client.value.name = "Changed";
            }
        }

        PrintClients();
    }

    void Update()
    {
    }
}
