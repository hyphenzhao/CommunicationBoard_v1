using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class PupilData
{
    public string topic;
    public float[] norm_pos = new float[2];
    public float confidence;
    public float timestamp;
}

public class TCPClientSide : MonoBehaviour
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
    private GameObject pupil1;
    [SerializeField]
    private GameObject pupil0;

    private float boardWidth;
    private float boardHeight;
    private float tx = 0f, tz = 0f;

    public void Start()
    {
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

    public void Update()
    {
        if (lastPacket != null)
        {
            ReportDataToTrackingManager(lastPacket);
        }

        if (errorStatus != null)
        {
            Debug.Log("Error: " + errorStatus);
            errorStatus = null;
        }
        if (warningStatus != null)
        {
            Debug.Log("Warning: " + warningStatus);
            warningStatus = null;
        }
        if (successStatus != null)
        {
            Debug.Log("Succeed: " + successStatus);
            successStatus = null;
        }
        if (unknownStatus != null)
        {
            Debug.Log("Unknown: " + unknownStatus);
            unknownStatus = null;
        }
    }

    public void ExchangePackets()
    {
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

            // writer.Write("request_pupil");
            // Debug.Log("Sent data!");
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

            exchanging = false;
        }
    }

    private void ReportDataToTrackingManager(string rawdata)
    {
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
            // PupilData pupildata = JsonUtility.FromJson<PupilData>(data);
            if (pupildata.confidence >= 0.60)
                TrackPupil(pupildata);
        }
        
    }

    private void TrackPupil(PupilData pdata)
    {
        float nx = pdata.norm_pos[0] - 0.5f;
        float nz = pdata.norm_pos[1] - 0.5f;
        if (pdata.topic.EndsWith(".1."))
        {
            nx = -nx;
            nz = -nz;
        }
        nx *= boardWidth;
        nz *= boardHeight;
        if (pdata.topic.EndsWith(".0."))
        {
            nx = (tx + nx) / 2f;
            nz = (tz + nz) / 2f;
            nx = GlobalVars.k1 * nx + GlobalVars.k2 * nz + GlobalVars.k3;
            nz = GlobalVars.k4 * nx + GlobalVars.k5 * nz + GlobalVars.k6;
            float ny = -0.07f;
            pupil0.transform.localPosition = new Vector3(nx, ny, nz);
            // Debug.Log("X: " + nx + " Y: " + ny);
        }
        else
        {
            tx = nx; tz = nz;
            float ny = -0.07f;
            //pupil1.transform.localPosition = new Vector3(nx, ny, nz);
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
            writer.Dispose();
            reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();
    }

}