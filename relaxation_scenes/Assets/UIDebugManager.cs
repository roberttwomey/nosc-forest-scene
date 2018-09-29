using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIDebugManager : MonoBehaviour {
    public Text textDebug;

    private float currentConcentration;
    private float currentMellow;
    private float currentHRV;

    private float smoothedConcentration;
    private float smoothedMellow;
    private float smoothedHRV;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        currentConcentration = BiometricResponse.Inst.currentMellow;
        currentMellow = BiometricResponse.Inst.currentMellow;
        currentHRV = BiometricResponse.Inst.currentHRV;

        smoothedConcentration = BiometricResponse.Inst.smoothedConcentration;
        smoothedMellow = BiometricResponse.Inst.smoothedMellow;
        smoothedHRV = BiometricResponse.Inst.smoothedHRV;

        textDebug.text = "cc: " + currentConcentration.ToString("0.##") + "   sc:" + smoothedConcentration.ToString("0.##") + "\n"
            + "cm: " + currentMellow.ToString("0.##") + "   sm:" + smoothedMellow.ToString("0.##") + "\n"
            + "ch: " + currentHRV.ToString("0.##") + "   sh:" + smoothedHRV.ToString("0.##");
    }
}
