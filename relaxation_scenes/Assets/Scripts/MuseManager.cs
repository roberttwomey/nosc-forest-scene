using UnityEngine;
using System;
using System.Collections.Generic;
using LSL;

public class MuseManager : MonoBehaviour {

    public static MuseManager Inst = null;

	public Texture2D sensorDisplayOut;
	public Texture2D sensorDisplayIn;

    // Local variables
    private bool useLSL = false; // Switch to NeuroScale pipeline with a data streaming over LSL
    private bool forceDataDisplay = false; // Get data even if not touching forehead
    private bool touchingForehead = false;
    private int offForeheadCounter = 0;
    private float lastConcentrationMeasure = 0f;
    private float lastMellowMeasure = 0f;
    private float batteryLevel = 1.0f;
    private List<int> headConnectionStatus = new List<int>() {0,0,0,0};
    private DateTime timeOfLastMessage = DateTime.Now - new TimeSpan(1, 1, 1);
    private Queue<int> blinkQueue = new Queue<int>();
    private HashSet<string> sensorMeasuresToTrack = new HashSet<string>(); // add metrics to this that have data for each sensor


    // Accessors
    public bool TouchingForehead{get{return forceDataDisplay || (touchingForehead && (MuseManager.Inst.SecondsSinceLastMessage < 1f));}}
    public float LastConcentrationMeasure{
        get {
            float concentration = lastConcentrationMeasure;
            return !invertConcentration? concentration : 1f - concentration;
        }
	}
	public float LastMellowMeasure { get { return lastMellowMeasure; } }
	public bool MuseDetected { get { return SecondsSinceLastMessage < 1f; } }
    public float SecondsSinceLastMessage { get { return (float)(DateTime.Now - timeOfLastMessage).TotalSeconds; } }
    public int NumBlinksInLastSecond { get { return Sum(blinkQueue); } }
    // float (0-1)
    public float BatteryPercentage { get { return batteryLevel; } }
    // 4 ints for the 4 sensors -- 1 = good, 2 = ok, >=3 bad
    public List<int> HeadConnectionStatus { get { return headConnectionStatus; } }


	bool invertConcentration = false;
	bool slowResponse = false;
	public bool SlowResponse {get{return slowResponse;}}


    // Events
    public delegate void Action<T1, T2, T3, T4, T5>(T1 p1, T2 p2, T3 p3, T4 p4, T5 p5);
    public Action<string, float> MetricUpdate; // concentration or mellow
    public Action<string, float, float, float, float> SensorMeasureUpdate; // alpha_relative, beta_relative, gamma_relative, theta_relative, delta_relative, etc.


    // liblsl vars
    private readonly int CHNS_MUSE = 22;
    private readonly float SAMPLE_RATE = 10f;
    liblsl.StreamInfo streamInfo;
    liblsl.StreamOutlet streamOutlet;
    private Dictionary<string, List<float>> currentSample;
    private List<float> processedSample;
    private float timeSinceLastUpdate = 0f;
    private List<string> channelList;

    void Awake(){
        Inst = this;

        sensorMeasuresToTrack = new HashSet<string>() {
            "alpha_relative",
            "beta_relative",
            "gamma_relative",
            "theta_relative",
            "delta_relative"
        };

        channelList = new List<string>()
        {
            "ar0", "ar1", "ar2", "ar3",
            "br0", "br1", "br2", "br3",
            "gr0", "gr1", "gr2", "gr3",
            "tr0", "tr1", "tr2", "tr3",
            "dr0", "dr1", "dr2", "dr3",
            "mellow", "concentration"
        };

        streamInfo = new liblsl.StreamInfo("Muse", "EEG", CHNS_MUSE, SAMPLE_RATE, liblsl.channel_format_t.cf_float32, "museid_1");
        liblsl.XMLElement channels = streamInfo.desc().append_child("channels");
        foreach(string c in channelList)
        {
            channels.append_child(c);
        }
        streamOutlet = new liblsl.StreamOutlet(streamInfo);

        currentSample = new Dictionary<string, List<float>>();
        currentSample.Add("alpha_relative", null);
        currentSample.Add("beta_relative", null);
        currentSample.Add("gamma_relative", null);
        currentSample.Add("theta_relative", null);
        currentSample.Add("delta_relative", null);
        currentSample.Add("mellow", null);
        currentSample.Add("concentration", null);

        processedSample = new List<float>();
    }

	void Start () {
#if UNITY_STANDALONE
		OSCHandler.Instance.Init ();
        if (OSCHandler.Instance.Servers.ContainsKey("AssemblyOSC"))
            OSCHandler.Instance.Servers["AssemblyOSC"].server.PacketReceivedEvent += Server_PacketReceivedEvent;
#endif
    }

	void Update () {
        if (Input.GetKeyDown(KeyCode.RightAlt) || Input.GetKeyDown(KeyCode.LeftAlt))
			invertConcentration = !invertConcentration;
        if (Time.time > timeSinceLastUpdate + 1 / SAMPLE_RATE)
        {
            timeSinceLastUpdate = Time.time;

            processedSample.Clear();

            foreach (string metric in currentSample.Keys)
            {
                if (currentSample[metric] == null)
                {
                    continue;
                }

                foreach (float val in currentSample[metric])
                {
                    processedSample.Add(val);
                }
            }
            //Debug.Log("SAMPLE RECIEVE: " + processedSample.Count);
            
            if (processedSample.Count == CHNS_MUSE)
            {
            }
            else
            {
                //Debug.Log("NO SAMPLES YET");
            }
        }



		/*
		if(Input.GetKeyDown(KeyCode.S))
			slowResponse = !slowResponse;
        if (Input.GetKeyDown(KeyCode.L))
            useLSL = !useLSL;
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F))
            forceDataDisplay = !forceDataDisplay;
		*/

        //testing
        //HandleConcentrationSample(2f * UnityEngine.Random.value);
        //HandleMellowSample(2f * UnityEngine.Random.value);
        //SensorMeasureUpdate("alpha_relative", 2f * UnityEngine.Random.value, 2f * UnityEngine.Random.value, 2f * UnityEngine.Random.value, 2f * UnityEngine.Random.value);
    }
	

    void OnDestroy() {
        Inst = null;
    }

#if UNITY_STANDALONE

    private void Server_PacketReceivedEvent(UnityOSC.OSCServer sender, UnityOSC.OSCPacket packet)
    {
        //Debug.LogError("packet: " + packet.Address + " " + OSCHandler.DataToString(packet.Data));
        if (packet.Address.Contains("touching_forehead"))
            HandleTouchingForehead((int)packet.Data[0] != 0);
        else if (packet.Address.Contains("concentration"))
            HandleConcentrationSample((float)packet.Data[0]);
        else if (packet.Address.Contains("mellow"))
            HandleMellowSample((float)packet.Data[0]);
        else if (packet.Address.Contains("horseshoe"))
            HandleHeadConnectMessage(packet.Data);
        else if (packet.Address.Contains("batt"))
            HandleBatteryStatus(packet.Data);
        else if (packet.Address.Contains("blink"))
            HandleBlinkSample(packet.Data);
        foreach (string metric in sensorMeasuresToTrack)
        {
            if (packet.Address.Contains(metric))
            {
                currentSample[metric] = new List<float>(new float[] { (float)packet.Data[0], (float)packet.Data[1], (float)packet.Data[2], (float)packet.Data[3] });
                if (SensorMeasureUpdate != null)
                {
                    SensorMeasureUpdate(metric, (float)packet.Data[0], (float)packet.Data[1], (float)packet.Data[2], (float)packet.Data[3]);
                }
            }
        }
        

        timeOfLastMessage = DateTime.Now;
    }

    private void HandleTouchingForehead(bool touching) {
        if (touchingForehead && !touching) {
            offForeheadCounter++;
            if (offForeheadCounter >= 2) {
                touchingForehead = false;
                offForeheadCounter = 0;
            }
        }
        else {
            touchingForehead = touching;
            offForeheadCounter = 0;
        }
    }
#endif

    // blinks expected to come in at 10 samples per second
    // store the last 10 samples - 1 second of data
    void HandleBlinkSample(List<object> data)
    {
        blinkQueue.Enqueue((int)data[0]);
        if (blinkQueue.Count > 10)
            blinkQueue.Dequeue();
    }

    void HandleConcentrationSample(float sample)
    {
        lastConcentrationMeasure = sample;
        //Debug.Log("CONC");
        currentSample["concentration"] = new List<float>(new float[] { sample });
        if (MetricUpdate != null)
            MetricUpdate("concentration", sample);
    }

    void HandleMellowSample(float sample)
	{
        //Debug.Log("MELL");
        lastMellowMeasure = sample;
        currentSample["mellow"] = new List<float>(new float[] { sample });
        if (MetricUpdate != null)
            MetricUpdate("mellow", sample);
    }

    // These are status messages for connection to the user's head.
    // 1 = good, 2 = ok, >=3 bad
    void HandleHeadConnectMessage(List<object> data)
    {
        for (int i = 0; i < headConnectionStatus.Count && i < data.Count; ++i)
            headConnectionStatus[i] = (int)((float)data[i]);
    }

    void HandleBatteryStatus(List<object> data)
    {
        batteryLevel = ((int)data[0]) / 10000f;
    }

    int Sum(IEnumerable<int> q)
    {
        int sum = 0;
        foreach (int e in q)
            sum += e;
        return sum;
    }

	float[] sensorIndSizes = new float[4];
	float[] sensorIndSizeVels = new float[4];

	float wearingHeadsetIndication = 0f;
	float wearingHeadsetIndicationVel = 0f;

    void OnGUI()
    {
        wearingHeadsetIndication = Mathf.SmoothDamp(wearingHeadsetIndication, TouchingForehead? 1f : 0f, ref wearingHeadsetIndicationVel, 1f);

		// Sensor displays
		float sensorRingSize = 50f * wearingHeadsetIndication;
		float sensorRingSpacing = 10f;
		for(int i = 0; i < 4; i++){
			Vector2 sensorRectCenter = new Vector2(((Screen.width * 0.5f) - ((sensorRingSize * 1.5f) + (sensorRingSpacing * 1.5f))) + (i * (sensorRingSize + sensorRingSpacing)), (sensorRingSpacing + (sensorRingSize * 0.5f)));
			Rect sensorRect = MathUtilities.CenteredSquare(sensorRectCenter.x, sensorRectCenter.y, sensorRingSize);
			GUI.DrawTexture(sensorRect, sensorDisplayOut);

			Rect sensorStatusRect = MathUtilities.CenteredSquare(sensorRectCenter.x, sensorRectCenter.y, sensorRingSize * Mathf.InverseLerp(3f, 1f, sensorIndSizes[i]));
			GUI.DrawTexture(sensorStatusRect, sensorDisplayIn);

			sensorIndSizes[i] = Mathf.SmoothDamp(sensorIndSizes[i], HeadConnectionStatus[i], ref sensorIndSizeVels[i], 0.25f);
		}

        GUI.skin.label.fontSize = 14;
        GUI.skin.label.alignment = TextAnchor.LowerCenter;

		string statusStr = "";
		// Attention metric
		if(SecondsSinceLastMessage < 1f || forceDataDisplay) {
			if(TouchingForehead)
				statusStr += (LastConcentrationMeasure * 100f).ToString("F0") + "%";
			else
				statusStr += "EEG device ready.";

			if(invertConcentration)
				statusStr += " ~";

			if(slowResponse)
				statusStr += " s";

            if (useLSL)
                statusStr += " Q";

            if (forceDataDisplay)
                statusStr += ">";

            if (BatteryPercentage < 0.2f)
				statusStr += "\n" + (BatteryPercentage * 100f).ToString("F0") + "% battery remaining.";


			GUI.Label(MathUtilities.CenteredSquare(Screen.width * 0.5f, ((sensorRingSize + (sensorRingSpacing)) * Mathf.Sqrt(wearingHeadsetIndication)) + 505f, 1000f), statusStr);
		}
    }
}
