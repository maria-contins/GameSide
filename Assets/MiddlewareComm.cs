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
    // MIDDLWARE COMMUNICATION
    Thread thread;
    public int connectionPort = 25001;
    TcpListener server;
    TcpClient client;
    bool running;

    // NOTE: USELESS STUPID BC NOT 3 ANYMORE
    // NEED SOME SORT OF DYNAMIC READER COUNT WHEN CONFIG STEP IS IMPLEMENTED
    public GameObject card0;
    public GameObject card1;
    public GameObject card2;
    public GameObject[] cubes;

    // TUI CONFIG
    public int nr_modules; //needs to be dynamic
    public int[] nr_readers_per_module; //needs to be dynamic in size and values, position is important 
    public int nr_slots;
    public int[] reader_order; //dynamic, related to nr_readers_per_module positions
    
    // STATE
    public string[] data;
    public string[] state = new string[3];
    public Dictionary<string, string[][]> deck;
    public Game game;
    
    // GAME MODES Scores etc...
    enum GameMode
    {
        MasterMind,
        Memory
    }


    void Start()
    {
        startCommMiddleware();

        // TUI CONFIG MUST BE DYNAMIC 
        nr_modules = 2;
        nr_readers_per_module = new int[] {2, 1};
        nr_slots = calculateNrSlots(nr_modules, nr_readers_per_module);
        reader_order = new int[] {0, 1};
        
        data = null;
        
        //cubes = new GameObject[] { card0, card1, card2 };

        setInialState(nr_slots);
        game = new MasterMind(nr_slots);
    }

    int calculateNrSlots(int nr_modules, int[] nr_readers_per_module)
    {
        int nr_slots = 0;
        for (int i = 0; i < nr_modules; i++)
        {
            nr_slots += nr_readers_per_module[i];
        }
        return nr_slots;
    }

    void startCommMiddleware()
    {
        Debug.Log("Starting server");
        ThreadStart ts = new ThreadStart(GetData);
        thread = new Thread(ts);
        thread.Start(); 
    }

    void setInialState(int nr_slots)
    {
        for (int i = 0; i < nr_slots; i++)
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

        string dataReceived = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        string[] data = dataReceived.Split(',');
        Debug.Log("Data received: " + data);

        Debug.Log("Data size: " + data.Length);
        
        if (dataReceived != null)
        {
            nwStream.Write(buffer, 0, bytesRead);
            //Debug.Log("Data received: " + dataReceived);
        }

        // Debug.Log("Data ihihih: " + data[0]);
        // data = dataReceived.Split(' ');
        // state[int.Parse(data[0])] = data[1].Trim();
        //state = data;
        //Debug.Log("State: " + state[0] + " " + state[1] + " " + state[2]);
    }

    // 1 -> MasterMind
    // 2 -> Memory 
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            Debug.Log("Now playing MasterMind");
            //game = new MasterMind(nr_slots);
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            Debug.Log("Now playing Memory game");
            //game = new Memory(nr_slots, cubes);
    
        }

        if (state != null && state[0] != "none")
        {
            switch (game)
            {
                case MasterMind _:
                    //game.CheckValidity(cubes, state);
                    //game.FeedBack(cubes, state);
                    break;
                case Memory _:
                    //game.FeedBack(cubes, state);
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
        public abstract void StartNewGame(GameObject[] cubes);
    }

    public class MasterMind : Game
    {
        private string[][] currentConfig;
        private string[] currentSolution;
        private Dictionary<string, string[][]> modes;
        private string[] progress;
        private int nr_slots;

        public MasterMind(int nr_slots)
        {
            LoadMasterMindJson();

            this.nr_slots = nr_slots;
            currentConfig = modes["3readers"];
            progress = new string[nr_slots];

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
            for (int i = 0; i < nr_slots; i++)
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
                StartNewGame(cubes);
            }
        }

        public override void StartNewGame(GameObject[] cubes)
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
        private int nr_slots;
        private string[] lastState;

        public Memory(int nr_slots, GameObject[] cubes)
        {
            LoadMemoryJson();

            this.nr_slots = nr_slots;
            currentConfig = modes["3readers"];

            StartNewGame(cubes);
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
            
        }

        public void ShowOrder(GameObject[] cubes) // TODO: IF ASKED SHOW ORDER AGAIN
        { 
            Debug.Log("Showing order");
            for (int i = 0; i < nr_slots; i++)
            {
                Debug.Log(currentSolution[i]);
                ChangeColour(cubes[i], currentSolution[i]);
            }
            
            // wait

            HideOrder(cubes);
            Debug.Log("NOW GUESS");
        }

        private void HideOrder(GameObject[] cubes)
        {
            for (int i = 0; i < nr_slots; i++)
            {
                ChangeColour(cubes[i], "grey");
            }
        }

        private void ChangeColour(GameObject card, string colour)
        {
            Debug.Log("Changing colour to: " + colour);
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
                case "grey":
                    card.GetComponent<Renderer>().material.color = Color.grey;
                    break;
                default:
                    break;
            }
        }

        public override void FeedBack(GameObject[] cubes, string[] state) {

            for (int i = 0; i < nr_slots; i++)
            {
                ChangeColour(cubes[i], state[i]);
            }

            if (state == currentSolution)
            {
                Debug.Log("YOU WON!");
                StartNewGame(cubes);
            }
            else if (state != lastState)
            {
                Debug.Log("TRY AGAIN!");
                lastState = state;
            }
        }

        public override void StartNewGame(GameObject[] cubes)
        {
            System.Random rand = new Random();
            currentSolution = currentConfig[rand.Next(0, currentConfig.Length)];
            ShowOrder(cubes);
        }
    }
}
        