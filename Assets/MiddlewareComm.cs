using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;

public class MyListener : MonoBehaviour
{
    Thread thread;
    public int connectionPort = 25001;
    TcpListener server;
    TcpClient client;
    bool running;

    public GameObject card0;
    public GameObject card1;
    public GameObject card2;

    public string[] data;

    public string[] state = new string[3];


    void Start()
    {
        Debug.Log("Starting server");
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start(); 
        data = null;

        state[0] = "none";
        state[1] = "none";
        state[2] = "none";
    }

    void GetData()
    {
        server = new TcpListener(IPAddress.Any, connectionPort);
        server.Start();

        client = server.AcceptTcpClient();

        running = true;
        while (running)
        {
            Connection();
        }
        server.Stop();
    }

    void Connection()
    {
        NetworkStream nwStream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];
        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        if (dataReceived != null && dataReceived != "")
        {
            nwStream.Write(buffer, 0, bytesRead);
            Debug.Log("Data received: " + dataReceived);
        }

        //StringSplitOptions.RemoveEmptyEntries
        data = dataReceived.Split(' ');

        state[int.Parse(data[0])] = data[1].Trim();
    }

    void Update()
    {
        ChangeColour(card0, state[0]);
        ChangeColour(card1, state[1]);
        ChangeColour(card2, state[2]);
    }

    void ChangeColour(GameObject card, string colour)
    {
        switch (colour)
        {
            case "red":
                card.GetComponent<Renderer>().material.color = Color.red;
                break;
            case "blue":
                card.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case "green":
                card.GetComponent<Renderer>().material.color = Color.green;
                break;
            default:
                break;
        }
    }


}   