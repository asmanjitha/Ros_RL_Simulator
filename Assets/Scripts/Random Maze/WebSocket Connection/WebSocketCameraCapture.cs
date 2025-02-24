using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using UnityEngine.UI;
using System.Numerics;

public class WebSocketCameraCapture : MonoBehaviour
{
    public Camera captureCamera;  
    // public RawImage displayImage; 
    public GameObject boundingBoxPrefab; 

    private ClientWebSocket ws;
    private bool isConnected = false;
    public List<GameObject> activeBoundingBoxes = new List<GameObject>();

    public GameObject canvas;

    public bool autoIdentifyHumans = true;
    private bool findHumanInView = false;

    private Queue<string> messageQueue = new Queue<string>();
    private object queueLock = new object();

    async Task Start()
    {
        SetupCanvas();
        await ConnectToServer();
        StartCoroutine(CaptureAndSend());
        _ = ReceiveMessages();  // Start listening for responses
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            findHumanInView = true;
        }

        // Process received messages on the main thread
        while (messageQueue.Count > 0)
        {
            string message;
            lock (queueLock)
            {
                message = messageQueue.Dequeue();
            }

            if (autoIdentifyHumans)
            {
                DrawBoundingBoxes(message);
            }
            else
            {
                UpdateWithoutDetection(message);
            }
        }
    }

    void SetupCanvas(){
        Canvas cv = FindObjectOfType<Canvas>();
        if(cv != null){
            canvas = cv.gameObject;
        }
    }

    async Task ConnectToServer()
    {
        ws = new ClientWebSocket();
        Uri serverUri = new Uri("ws://localhost:4200");

        try
        {
            await ws.ConnectAsync(serverUri, CancellationToken.None);
            isConnected = true;
            Debug.Log("✅ Connected to WebSocket server!");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ WebSocket connection error: " + e.Message);
        }
    }

    IEnumerator CaptureAndSend()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);

            if (isConnected)
            {
                Texture2D screenshot = CaptureCameraView();
                byte[] imageBytes = screenshot.EncodeToPNG();
                Destroy(screenshot);
                SendImageToServer(imageBytes);
            }
        }
    }

    Texture2D CaptureCameraView()
    {
        RenderTexture rt = new RenderTexture(1920, 1080, 16);
        captureCamera.targetTexture = rt;
        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenshot.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        return screenshot;
    }

    async void SendImageToServer(byte[] imageBytes)
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.SendAsync(new ArraySegment<byte>(imageBytes), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    async Task ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

        while (ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            lock (queueLock)
            {
                messageQueue.Enqueue(message);
            }
        }
    }

    void UpdateWithoutDetection(string detections)
    {
        UnityEngine.Vector3 pos = UnityEngine.Vector3.negativeInfinity;
        if (findHumanInView)
        {
            string[] lines = detections.Split('\n');

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                string[] parts = line.Split(' ');
                string label = parts[0];
                if (label == "person")
                {
                    float x1 = float.Parse(parts[1]);
                    float y1 = float.Parse(parts[2]);
                    float x2 = float.Parse(parts[3]);
                    float y2 = float.Parse(parts[4]);
                    
                    float normalizedX = (x1 + x2) / 2;
                    float normalizedY = (y1 + y2) / 2;
                    pos = GetHitPositionHumans(new UnityEngine.Vector2(normalizedX, normalizedY));

                    if (pos != UnityEngine.Vector3.negativeInfinity)
                    {
                        Debug.Log("!!!! Found Human in Scene!!!!" + pos);
                    }
                    else
                    {
                        Debug.Log("!!!! No Human in Scene, Try in Different Perspective!!!!");
                    }
                }
            }

            findHumanInView = false;
        }
    }

    void DrawBoundingBoxes(string detections)
    {
        foreach (GameObject box in activeBoundingBoxes)
        {
            box.SetActive(false);
            Destroy(box);
        }
        activeBoundingBoxes.Clear();

        string[] lines = detections.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            string[] parts = line.Split(' ');
            string label = parts[0];
            if (label == "person")
            {
                float x1 = float.Parse(parts[1]);
                float y1 = float.Parse(parts[2]);
                float x2 = float.Parse(parts[3]);
                float y2 = float.Parse(parts[4]);
                
                float normalizedX = (x1 + x2) / 2;
                float normalizedY = (y1 + y2) / 2;
                GameObject bbox = Instantiate(boundingBoxPrefab, canvas.transform);
                bbox.GetComponent<RectTransform>().sizeDelta = new UnityEngine.Vector3(x2 - x1, y2 - y1, 1);
                bbox.GetComponent<RectTransform>().anchoredPosition = new UnityEngine.Vector2(normalizedX, -normalizedY);
                
                activeBoundingBoxes.Add(bbox);
            }
        }
    }

    public UnityEngine.Vector3 GetHitPositionHumans(UnityEngine.Vector2 screenPosition)
    {
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider != null && hit.collider.transform.gameObject.tag == "Human")
            {
                return hit.collider.transform.position;
            }
        }
        return UnityEngine.Vector3.negativeInfinity;
    }
}
