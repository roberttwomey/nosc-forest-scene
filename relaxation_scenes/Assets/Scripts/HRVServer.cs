using System;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using UnityEngine;

// NOTE: the original code has been modified to now wait for a local connection to provide
// data from the HRV
// launch the game in the editor first, then run the python script outstream_hrv.py
public class HRVServer {

    static int port = 12346;
    static TcpListener hrvServer = null;

    public Action<HRVUpdate> MessageReceived;

    private static HRVServer mInst = null;
    public static HRVServer Inst {
        get {
            if (mInst == null)
                mInst = new HRVServer();
            return mInst;
        }
    }

    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    TcpClient connectedTcpClient;

    private HRVServer() {
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        /*
        Debug.Log("HRVServer Init");
        //IPAddress localAddr = IPAddress.Parse(GetLocalIPAddress());
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        Debug.Log(localAddr);
        try {
            hrvServer = new TcpListener(localAddr, port);
        } catch(Exception e) {
            Debug.LogError("Exception with TcpListener: " + e.ToString());
        }

        // Start listening for client requests.
        try { 
            hrvServer.Start();
        } catch(SocketException e) {
            Debug.LogError("Exception starting tcp listener: " + e.ErrorCode + " " + e.ToString());
        }

        ConnectToNextClient();*/
    }

    public static string GetLocalIPAddress() {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        string returnIP = "";
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                if (string.IsNullOrEmpty(returnIP) || returnIP.StartsWith("192"))
                    returnIP = ip.ToString();
            }
        }
        return returnIP;
    }

    private void ListenForIncommingRequests()
    {
        Debug.Log("HRVServer Init");
        //IPAddress localAddr = IPAddress.Parse(GetLocalIPAddress());
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        Debug.Log(localAddr);
        try
        {
            hrvServer = new TcpListener(localAddr, port);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception with TcpListener: " + e.ToString());
        }

        // Start listening for client requests.
        try
        {
            hrvServer.Start();
        }
        catch (SocketException e)
        {
            Debug.LogError("Exception starting tcp listener: " + e.ErrorCode + " " + e.ToString());
        }

        ConnectToNextClient();
    }

    private static void ConnectToNextClient() {
        try {
            hrvServer.BeginAcceptTcpClient(new AsyncCallback(AcceptConnection), hrvServer);
        }
        catch(Exception e) {
            Debug.LogError("Exception in Begin Accept TCP Client: " + e.ToString());
        }
    }

    private static void AcceptConnection(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;
        TcpClient client = hrvServer.EndAcceptTcpClient(ar);

        while (true)
        {
            NetworkStream stream = client.GetStream();
            string msg = ReadMessage(stream);
            HRVUpdate update = new HRVUpdate();
            update.rrMin = int.Parse(msg.Split(',')[0]);
            update.rrMax = int.Parse(msg.Split(',')[1]);
            update.rrLast = int.Parse(msg.Split(',')[2]);
            if (Inst.MessageReceived != null)
                Inst.MessageReceived(update);
        }

        // processing used to be done locally, now its done in the python script

        //string json = GetMessageFromHTTPPostRequest(msg);
        //HRVMessage hrvMsg = JsonUtility.FromJson<HRVMessage>(json);

        //if (Inst.MessageReceived != null)
        //    Inst.MessageReceived(hrvMsg);

        // wait for next message
        //ConnectToNextClient();
    }

    static string ReadMessage(NetworkStream stream) {
        StringBuilder message = new StringBuilder();
        if (stream.CanRead) {
            byte[] myReadBuffer = new byte[1024];
            
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size.
            do {
                numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                message.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
            }
            while (stream.DataAvailable);
        }
        else {
            Debug.LogError("Sorry.  You cannot read from this NetworkStream.");
        }
        return message.ToString();
    }

    static string GetMessageFromHTTPPostRequest(string msg) {

        // find the start of the data we are interested in. 
        int index = msg.IndexOf("Content-Length: ");
        if (index == -1)
            return "";
        index = msg.IndexOf("{", index);

        return index != -1 ? msg.Substring(index) : "";
    }
}
