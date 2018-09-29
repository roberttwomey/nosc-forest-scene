using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HRVManager : MonoBehaviour {
    public static HRVManager Inst = null;

    int rrMin = int.MaxValue;
    int rrMax = int.MinValue;
    int lastRR = 0;

    public float LastNormalizedSample { get { return (float)(lastRR - rrMin) / (float)(rrMax - rrMin); } }

    private void Awake()
    {
        Inst = this;
        HRVServer.Inst.MessageReceived += UpdateMetrics;

        rrMin = 0;
        rrMax = 0;
        lastRR = 0;
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (rrMin < rrMax)
        {
            Debug.Log("last RR: " + lastRR);
        }
    }

    void UpdateMetrics(HRVUpdate update)
    {
        rrMin = update.rrMin;
        rrMax = update.rrMax;
        lastRR = update.rrLast;
    }
}
