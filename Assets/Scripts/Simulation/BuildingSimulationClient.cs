using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

/// <summary>
/// Handles TCP/IP communication with an external simulation server.
/// </summary>
public class BuildingSimulationClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8080;
    public bool connectOnStart = true;
    public float reconnectInterval = 5f;
    
    [Header("Debug")]
    public bool logMessages = true;
    
    // Connection state
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool shouldRun = true;
    
    // Message queues
    private ConcurrentQueue<string> sendQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> receiveQueue = new ConcurrentQueue<string>();
    
    // Public properties
    public bool connected { get; private set; } = false;
    
    // Events
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnMessageReceived;
    
    void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }
    
    void Update()
    {
        // Process received messages on main thread
        while (receiveQueue.Count > 0)
        {
            if (receiveQueue.TryDequeue(out string message))
            {
                OnMessageReceived?.Invoke(message);
                ProcessReceivedMessage(message);
            }
        }
        
        // Send queued messages
        SendQueuedMessages();
    }
    
    void OnDestroy()
    {
        shouldRun = false;
        Disconnect();
    }
    
    /// <summary>
    /// Connects to the simulation server
    /// </summary>
    public void Connect()
    {
        if (connected) return;
        
        try
        {
            client = new TcpClient();
            client.BeginConnect(serverIP, serverPort, ConnectCallback, null);
            LogDebug("Connecting to server...");
        }
        catch (Exception e)
        {
            LogDebug($"Connection error: {e.Message}");
            ScheduleReconnect();
        }
    }
    
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            client.EndConnect(ar);
            connected = true;
            stream = client.GetStream();
            
            LogDebug("Connected to server");
            
            // Start receive thread
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            // Notify on main thread
            MainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
            
            // Send registration message
            SendNetworkMessage("{\"type\":\"REGISTER\",\"clientType\":\"unity_vr\"}");
        }
        catch (Exception e)
        {
            LogDebug($"Connection failed: {e.Message}");
            ScheduleReconnect();
        }
    }
    
    private void ReceiveData()
    {
        byte[] buffer = new byte[4096];
        
        while (shouldRun && connected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        receiveQueue.Enqueue(message);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                LogDebug($"Error receiving data: {e.Message}");
                break;
            }
        }
        
        // Disconnect on thread exit
        MainThreadDispatcher.Enqueue(Disconnect);
    }
    
    private void SendQueuedMessages()
    {
        if (!connected || stream == null) return;
        
        while (sendQueue.Count > 0)
        {
            if (sendQueue.TryDequeue(out string message))
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    LogDebug($"Sent message: {message}");
                }
                catch (Exception e)
                {
                    LogDebug($"Error sending message: {e.Message}");
                    Disconnect();
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// Sends a message to the simulation server
    /// </summary>
    public void SendNetworkMessage(string message)
    {
        if (!connected) return;
        
        sendQueue.Enqueue(message);
    }
    
    private void ProcessReceivedMessage(string message)
    {
        // Override this in derived classes to process specific message types
        // Here we just log it
        LogDebug($"Received message: {message}");
        
        // Find the simulation manager to process the message
        BuildingSimulationManager manager = FindObjectOfType<BuildingSimulationManager>();
        if (manager != null)
        {
            // Process the message (would need JSON parsing based on your protocol)
            // For now, this is just a placeholder
        }
    }
    
    private void ScheduleReconnect()
    {
        if (!shouldRun) return;
        
        LogDebug($"Scheduling reconnect in {reconnectInterval} seconds");
        MainThreadDispatcher.Enqueue(() => Invoke("Connect", reconnectInterval));
    }
    
    /// <summary>
    /// Disconnects from the simulation server
    /// </summary>
    public void Disconnect()
    {
        if (!connected) return;
        
        connected = false;
        
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(500);
            receiveThread = null;
        }
        
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
        
        if (client != null)
        {
            client.Close();
            client = null;
        }
        
        LogDebug("Disconnected from server");
        OnDisconnected?.Invoke();
    }
    
    private void LogDebug(string message)
    {
        if (logMessages)
        {
            Debug.Log($"[BuildingSimulationClient] {message}");
        }
    }
}

/// <summary>
/// Utility class for executing actions on the main thread
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static Queue<Action> actionQueue = new Queue<Action>();
    private static MainThreadDispatcher instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        lock (actionQueue)
        {
            while (actionQueue.Count > 0)
            {
                actionQueue.Dequeue().Invoke();
            }
        }
    }
    
    /// <summary>
    /// Enqueues an action to be executed on the main thread
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (instance == null)
        {
            // Create the dispatcher if it doesn't exist
            GameObject dispatcherObject = new GameObject("MainThreadDispatcher");
            instance = dispatcherObject.AddComponent<MainThreadDispatcher>();
        }
        
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
        }
    }
}