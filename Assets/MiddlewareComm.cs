using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Random = System.Random;


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

    public GameObject[] cubes;


    public string[] data;
    public string[] state = new string[3];


    public Game game;
    int currentReaders;
    enum GameMode
    {
        MasterMind,
        Memory
    }


    void Start()
    {
        Debug.Log("Starting server");
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start(); 
        data = null;
        currentReaders = 3;
        cubes = new GameObject[] { card0, card1, card2 };

        setInialState(currentReaders);
        game = new MasterMind(currentReaders);
    }

    void setInialState(int currentReaders)
    {
        for (int i = 0; i < currentReaders; i++)
        {
            state[i] = "none";
        }
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

        data = dataReceived.Split(' ');
        state[int.Parse(data[0])] = data[1].Trim();

        Debug.Log("State: " + state[0] + " " + state[1] + " " + state[2]);
    }

    void Update()
    {
        game.CheckValidity(cubes, state);
        game.FeedBack(cubes, state);
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
    public abstract class Game
    {
        public abstract void CheckValidity(GameObject[] cubes, string[] state);
        public abstract void FeedBack(GameObject[] cubes, string[] state);
    }

    public class MasterMind : Game
    {
        private string[][] currentConfig;
        private string[] currentSolution;
        private Dictionary<string, string[][]> modes;
        private string[] progress;
        private int currentReaders;

        public MasterMind(int currentReaders)
        {
            LoadMasterMindJson();
            Debug.Log("CURRENT GAME MODE: MASTERMIND");

            this.currentReaders = currentReaders;
            currentConfig = modes["3readers"];
            progress = new string[currentReaders];

            System.Random rand = new Random();
            currentSolution = currentConfig[rand.Next(0, currentConfig.Length)];
            
            Debug.Log("CURRENT SOLUTION: " + currentSolution[0] + " " + currentSolution[1] + " " + currentSolution[2]);
        }

        public void LoadMasterMindJson()
        {
            using (StreamReader r = new StreamReader("MasterMind_3Readers.json"))
            {
                string json = r.ReadToEnd();
                modes = JsonConvert.DeserializeObject<Dictionary<string, string[][] >>(json);
            }
        }
        
        public override void CheckValidity(GameObject[] cubes, string[] state)
        {
            for (int i = 0; i < currentReaders; i++)
            {
                if (state[i] == currentSolution[i])
                {
                    progress[i] = "green";
                }
                else if (System.Array.IndexOf(currentSolution, state[i]) != -1)
                {
                    progress[i] = "yellow";
                }
                else
                {
                    progress[i] = "grey";
                }
            }
            
        }

        public void ShowProgress(GameObject card, string colour)
        {
            switch (colour)
            {
                case "grey":
                    card.GetComponent<Renderer>().material.color = Color.grey;
                    break;
                case "yellow":
                    card.GetComponent<Renderer>().material.color = Color.yellow;
                    break;
                case "green":
                    card.GetComponent<Renderer>().material.color = Color.green;
                    break;
                default:
                    break;
            }
        }

        public override void FeedBack(GameObject[] cubes, string[] state) {

            ShowProgress(cubes[0], progress[0]);
            ShowProgress(cubes[1], progress[1]);
            ShowProgress(cubes[2], progress[2]);

            if (progress == currentSolution)
            {
                Debug.Log("YOU WON!");
                StartNewGame();
            }
        }

        public void StartNewGame()
        {
            System.Random rand = new Random();
            currentSolution = currentConfig[rand.Next(0, currentConfig.Length)];
            Debug.Log("NEW SOLUTION: " + currentSolution[0] + " " + currentSolution[1] + " " + currentSolution[2]);
        }

    }

    public class Memory : Game
    {
        private string[][] currentConfig;
        private string[] currentSolution;
        private Dictionary<string, string[][]> modes;
        private int currentReaders;

        public Memory(int currentReaders)
        {
            LoadMemoryJson();
            Debug.Log("CURRENT GAME MODE: MEMORY");

            this.currentReaders = currentReaders;
            currentConfig = modes["3readers"];

            System.Random rand = new Random();
            currentSolution = currentConfig[rand.Next(0, currentConfig.Length)];

        }

        public void LoadMemoryJson()
        {
            using (StreamReader r = new StreamReader("Memory.json"))
            {
                string json = r.ReadToEnd();
                modes = JsonConvert.DeserializeObject<Dictionary<string, string[][] >>(json);
            }
        }
        
        public override void CheckValidity(GameObject[] cubes, string[] state)
        {
            if (state == currentSolution)
            {
                Debug.Log("YOU WON!");
                StartNewGame(cubes);
            } 
            else
            {
                Debug.Log("TRY AGAIN!");
            }
            
        }

        public void ShowOrder(GameObject[] cubes) // TODO: IF ASKED SHOW ORDER AGAIN
        {
            for (int i = 0; i < currentReaders; i++)
            {
                ChangeColour(cubes[i], currentSolution[i]);
            }

            Debug.Log("MEMORIZE!");
            Thread.Sleep(5000); //TODO Make it so that comms while sleep dont get read

            for (int i = 0; i < currentReaders; i++)
            {
                ChangeColour(cubes[i], "grey");
            }
        }

        private void ChangeColour(GameObject card, string colour)
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

        public override void FeedBack(GameObject[] cubes, string[] state) {

            if (state == currentSolution)
            {
                Debug.Log("YOU WON!");
                StartNewGame(cubes);
            }
            else
            {
                Debug.Log("TRY AGAIN!");
            }
        }

        public void StartNewGame(GameObject[] cubes)
        {
            System.Random rand = new Random();
            currentSolution = currentConfig[rand.Next(0, currentConfig.Length)];
            ShowOrder(cubes);
        }
}
        