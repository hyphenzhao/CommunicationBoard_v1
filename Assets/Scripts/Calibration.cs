using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class Calibration : MonoBehaviour, IInputClickHandler
{
#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    [SerializeField]
    private GameObject point1;
    [SerializeField]
    private GameObject point2;
    [SerializeField]
    private GameObject point3;
    [SerializeField]
    private GameObject point4;
    [SerializeField]
    private GameObject activateObject;

    private int clickCounter;
    private int dataSize0, dataSize1;
    private bool clickable;
    private int sizeGoal;
    private float boardWidth;
    private float boardHeight;
    private PupilData[] pupil0Data = new PupilData[55];
    private PupilData[] pupil1Data = new PupilData[55];
    private float[] ox = new float[5];
    private float[] oy = new float[5];
    private float[] rx = new float[5];
    private float[] ry = new float[5];

    void IInputClickHandler.OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("Start calibration...");
        if (clickable)
        {
            if (clickCounter < 4)
            {
                clickable = false;
                clickCounter++;
            }
            else
            {
                activateObject.SetActive(true);
                gameObject.SetActive(false);
            }
        }
        Debug.Log(clickCounter);
    }

    // Use this for initialization
    void Start () {
        sizeGoal = 50;
        rx[1] = GlobalVars.p1x; ry[1] = GlobalVars.p1z;
        rx[2] = GlobalVars.p2x; ry[2] = GlobalVars.p2z;
        rx[3] = GlobalVars.p3x; ry[3] = GlobalVars.p3z;
        rx[4] = GlobalVars.p4x; ry[4] = GlobalVars.p4z;
    }

    void OnEnable()
    {
        clickCounter = 0;
        clickable = true;
        dataSize0 = 0;
        dataSize1 = 0;
        boardWidth = Mathf.Abs(GlobalVars.p2x - GlobalVars.p1x);
        boardHeight = Mathf.Abs(GlobalVars.p3z - GlobalVars.p1z);
        Debug.Log("Height: " + boardHeight);
        Debug.Log("Width: " + boardWidth);
        Connect(GlobalVars.remoteIP, GlobalVars.remotePort);
    }

    public void Connect(string host, string port)
    {
        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }

#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        errorStatus = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();
        
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);
        
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut) { AutoFlush = true };
        
            Stream streamIn = socket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchanging = false;
    private bool exchangeStopRequested = false;
    private string lastPacket = null;

    private string errorStatus = null;
    private string warningStatus = null;
    private string successStatus = null;
    private string unknownStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

            string received = null;

#if UNITY_EDITOR
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes, 0, client.SendBufferSize);
                received += Encoding.UTF8.GetString(bytes, 0, recv);
                if (received.EndsWith("\n")) break;
            }
#else
            received = reader.ReadLine();
#endif

            lastPacket = received;
            // Debug.Log(lastPacket);
            exchanging = false;
        }
    }

    public void StopExchange()
    {
        exchangeStopRequested = true;

#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;
        }
#else
        if (exchangeTask != null) {
            exchangeTask.Wait();
            socket.Dispose();
            // writer.Dispose();
            // reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDisable()
    {
        StopExchange();
    }


    void CollectCalibrationDataAtScene(int scene)
    {
        GameObject point;
        switch(scene)
        {
            case 1: point = point1; break;
            case 2: point = point2; break;
            case 3: point = point3; break;
            case 4: point = point4; break;
            default: point = point1; break;
        }

        string rawdata = lastPacket;
        if (rawdata == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }
        String[] dataset = rawdata.Split('\n');
        foreach (var data in dataset)
        {
            if (String.IsNullOrEmpty(data)) continue;
            // Debug.Log("Read data: " + data);
            PupilData pupildata = JsonConvert.DeserializeObject<PupilData>(data);
            if (pupildata.confidence >= 0.60)
            {
                if(pupildata.topic.EndsWith(".0."))
                {
                    if(dataSize0 < sizeGoal)
                        pupil0Data[dataSize0++] = pupildata;
                } else
                {
                    if (dataSize1 < sizeGoal)
                        pupil1Data[dataSize1++] = pupildata;
                }
            }
        }

        int dataSize = dataSize0 + dataSize1;
        float ratio = (float)dataSize / ((float)sizeGoal * 2f);
        Color newColor = new Color(1 - ratio, ratio / 2, 0F);
        Material m = point.GetComponent<Renderer>().material;
        m.SetColor("_EmissionColor", newColor);
        Debug.Log("Scene: " + clickCounter + " | Frame: " + dataSize);
    }

    void FinishCalibrationAtScene(int scene)
    {
        GameObject point;
        switch (scene)
        {
            case 1: point = point1; break;
            case 2: point = point2; break;
            case 3: point = point3; break;
            case 4: point = point4; break;
            default: point = point1; break;
        }
        float tx = 0, ty = 0;
        for(int i = 0; i < sizeGoal; i++)
        {
            tx += pupil0Data[i].norm_pos[0];
            ty += pupil0Data[i].norm_pos[1];
            tx += 1 - pupil1Data[i].norm_pos[0];
            ty += 1 - pupil1Data[i].norm_pos[1];
        }
        ox[scene] = tx / ((float)sizeGoal * 2f) - 0.5f;
        oy[scene] = ty / ((float)sizeGoal * 2f) - 0.5f;
        ox[scene] *= boardWidth;
        oy[scene] *= boardHeight;

        if (scene == 4)
        {
            GlobalVars.k1 = ((rx[1] - rx[2]) * (oy[2] - oy[3]) - (rx[2] - rx[3]) * (oy[1] - oy[2])) /
                ((ox[1] - ox[2]) * (oy[2] - oy[3]) - (ox[2] - ox[3]) * (oy[1] - oy[2]));
            GlobalVars.k2 = ((rx[1] - rx[2]) * (ox[2] - ox[3]) - (rx[2] - rx[3]) * (ox[1] - ox[2])) /
                ((oy[1] - oy[2]) * (ox[2] - ox[3]) - (oy[2] - oy[3]) * (ox[1] - ox[2]));
            GlobalVars.k3 = rx[1] - GlobalVars.k1 * ox[1] - GlobalVars.k2 * oy[1];
            GlobalVars.k4 = ((ry[1] - ry[2]) * (oy[2] - oy[3]) - (ry[2] - ry[3]) * (oy[1] - oy[2])) /
                ((ox[1] - ox[2]) * (oy[2] - oy[3]) - (ox[2] - ox[3]) * (oy[1] - oy[2]));
            GlobalVars.k5 = ((ry[1] - ry[2]) * (ox[2] - ox[3]) - (ry[2] - ry[3]) * (ox[1] - ox[2])) /
                ((oy[1] - oy[2]) * (ox[2] - ox[3]) - (oy[2] - oy[3]) * (ox[1] - ox[2]));
            GlobalVars.k6 = ry[1] - GlobalVars.k4 * ox[1] - GlobalVars.k5 * oy[1];
            Debug.Log("k1: " + GlobalVars.k1);
            Debug.Log("k2: " + GlobalVars.k2);
            Debug.Log("k3: " + GlobalVars.k3);
            Debug.Log("k4: " + GlobalVars.k4);
            Debug.Log("k5: " + GlobalVars.k5);
            Debug.Log("k6: " + GlobalVars.k6);
        }

        Color newColor = new Color(0F, 1F, 0F);
        Material m = point.GetComponent<Renderer>().material;
        m.SetColor("_EmissionColor", newColor);
        dataSize0 = 0; dataSize1 = 0;
        clickable = true;
        if (clickCounter == 4)
        {
            GameObject textObject = GameObject.Find("Description");
            textObject.GetComponent<TextMesh>().text = "Click again to exit.";
        }
    }
    // Update is called once per frame
    void Update () {
        if (!clickable)
        {
            if (dataSize0 < sizeGoal || dataSize1 < sizeGoal)
            {
                CollectCalibrationDataAtScene(clickCounter);
            } else
            {
                FinishCalibrationAtScene(clickCounter);
            }
        }
	}
}
