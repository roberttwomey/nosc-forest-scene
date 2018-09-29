using DigitalRuby.RainMaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// handles the biometric visual feedback loop
public class BiometricResponse : MonoBehaviour {

    public static BiometricResponse Inst;

    [Range(0f, 1f)]
    public float debugMetric = 0f;
    public bool debug = false;

    // ranges that define the valid values for the variables modified
    public Vector2 distanceMinMax;
    public Vector2 rainMinMax;
    public Vector2 sunlightMinMax;
    public Vector2 audioMinMax;
    public Vector2 windMinMax;
    public Vector2 grassSpeedMinMax;
    public Vector2 grassBendMinMax;
    public float levitateMax;

    // the 'activation' range between 0 and 1 in which the given variable interpolates
    // when the metric is below or above the metric range, the value of the variable will be min and max
    // respectively
    public Vector2 weatherMetricRange;
    public Vector2 audioMetricRange;
    public Vector2 windMetricRange;
    public Vector2 levitateMetricRange;

    // control interpolation
    public float lerpSpeed = 1f;
    public float lerpSpeedLevitate = 0.5f;
    public Vector2 metricSmoothing;

    public GameObject player;
    public RainScript RainScript;
    public Light sunlight;
    public AudioMixer biometricAudioMixer;
    public WindZone wind;
    public Terrain terrain;
    public string[] biometricAudioExposed;

    private float distanceRange;
    private float audioThresholdRange;

    public float currentConcentration { get; private set; }
    public float currentMellow { get; private set; }
    public float currentHRV { get; private set; }
    public float smoothedConcentration { get; private set; }
    public float smoothedMellow { get; private set; }
    public float smoothedHRV { get; private set; }
    private float levitateMin;

    private void Awake()
    {
        Inst = this;
    }

    // Use this for initialization
    void Start () {
        // calculates the range that each audio track's volume interpolates with
        audioThresholdRange = 1f / biometricAudioExposed.Length;

        smoothedConcentration = 0f;
        smoothedMellow = 0f;
        smoothedHRV = 0f;

        // set the lower limit
        levitateMin = player.transform.position.y;
	}
	
	// Update is called once per frame
	void Update () {
        // get the newest biometric data from the user
        UpdateBiometrics();

        // decide which metric to use for the visual feedback
        float updateMetric = smoothedConcentration;

        // update fog
        UpdateFog(updateMetric);

        // update rain
        UpdateRain(updateMetric);

        // update sunlight
        UpdateSunlight(updateMetric);

        // update audio
        UpdateAudio(updateMetric);

        // update wind
        UpdateWind(updateMetric);

        // update height
        UpdateLevitation(updateMetric);
    }

    private void UpdateBiometrics()
    {
        // grab new values from the muse manager
        if (!debug && MuseManager.Inst.MuseDetected)
        {
            currentConcentration = MuseManager.Inst.LastConcentrationMeasure;
            currentMellow = MuseManager.Inst.LastMellowMeasure;
        }
        else
        {
            currentConcentration = debugMetric;
            currentMellow = debugMetric;
        }

        // grab values for HRV
        currentHRV = HRVManager.Inst.LastNormalizedSample;

        // use smoothing factor depending on rise or decrease in metric to control how easy it is to increase/decrease in 
        // the metric
        float smoothingUsedConcentration = currentConcentration < smoothedConcentration ? metricSmoothing.x : metricSmoothing.y;
        smoothedConcentration = Mathf.Lerp(smoothedConcentration,
            currentConcentration,
            Time.deltaTime * smoothingUsedConcentration);

        float smoothingUsedMellow = currentMellow < smoothedMellow ? metricSmoothing.x : metricSmoothing.y;
        smoothedMellow = Mathf.Lerp(smoothedMellow,
            currentMellow,
            Time.deltaTime * smoothingUsedMellow);

        float smoothingUsedHRV = currentHRV < smoothedHRV ? metricSmoothing.x : metricSmoothing.y;
        smoothedHRV = Mathf.Lerp(smoothedHRV,
            currentHRV,
            Time.deltaTime * smoothingUsedHRV);
        //Debug.Log("C: " + smoothedConcentration + " M: " + smoothedMellow);
    }

    private void UpdateFog(float updateMetric)
    {
        // adjust the metric to fit within the specified concentration range
        // the value is still between 0 and 1, except anything below or above the range
        // gets clamped to 0 and 1 respectively
        float updateMetricAdjusted = (updateMetric - weatherMetricRange.x) / (weatherMetricRange.y - weatherMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);

        // calculate and update the new distance
        float endDistanceNew = (distanceMinMax.y - distanceMinMax.x) * updateMetricAdjusted + distanceMinMax.x;
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, endDistanceNew, Time.deltaTime * lerpSpeed);
    }

    private void UpdateRain(float updateMetric)
    {
        // adjust the metric
        float updateMetricAdjusted = (updateMetric - weatherMetricRange.x) / (weatherMetricRange.y - weatherMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);
        // update the new intensity
        RainScript.RainIntensity = Mathf.Lerp(RainScript.RainIntensity, (float)1.0 - updateMetricAdjusted, Time.deltaTime * lerpSpeed);
    }

    private void UpdateSunlight(float updateMetric)
    {
        // adjust the metric
        float updateMetricAdjusted = (updateMetric - weatherMetricRange.x) / (weatherMetricRange.y - weatherMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);

        float sunlightIntensity = (sunlightMinMax.y - sunlightMinMax.x) * updateMetricAdjusted + sunlightMinMax.x;
        sunlight.intensity = Mathf.Lerp(sunlight.intensity, sunlightIntensity, Time.deltaTime * lerpSpeed);
    }

    private void UpdateAudio(float updateMetric)
    {
        // adjust the metric
        float updateMetricAdjusted = (updateMetric - audioMetricRange.x) / (audioMetricRange.y - audioMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);

        for (int i = 0; i < biometricAudioExposed.Length; i++)
        {
            float threshold = audioThresholdRange * i;
            float newVolumeFraction = Mathf.Clamp((updateMetricAdjusted - threshold) / audioThresholdRange, 0.0001f, 1f);

            // this allows the music volume to change intensity linearly
            float newVolumeDB = 20 * Mathf.Log10(newVolumeFraction);

            //float newVolumeDB = (audioMinMax.y - audioMinMax.x) * newVolumeFraction + audioMinMax.x;
            biometricAudioMixer.SetFloat(biometricAudioExposed[i], newVolumeDB);
        }
    }

    private void UpdateLevitation(float updateMetric)
    {
        // adjust the metric
        float updateMetricAdjusted = (updateMetric - levitateMetricRange.x) / (levitateMetricRange.y - levitateMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);

        float newHeight = (levitateMax - levitateMin) * updateMetricAdjusted + levitateMin;
        newHeight = Mathf.Lerp(player.transform.position.y, newHeight, Time.deltaTime * lerpSpeedLevitate);
        player.transform.position = new Vector3(
            player.transform.position.x,
            newHeight,
            player.transform.position.z);
    }

    private void UpdateWind(float updateMetric)
    {
        // adjust the metric
        float updateMetricAdjusted = (updateMetric - windMetricRange.x) / (windMetricRange.y - windMetricRange.x);
        updateMetricAdjusted = Mathf.Clamp(updateMetricAdjusted, 0f, 1f);

        // change the wind settings
        float newWindSpeed = (windMinMax.y - windMinMax.x) * updateMetricAdjusted + windMinMax.x;
        float newGrassSpeed = (grassSpeedMinMax.y - grassSpeedMinMax.x) * updateMetric + grassSpeedMinMax.x;
        float newGrassBend = (grassBendMinMax.y - grassBendMinMax.x) * updateMetric + grassSpeedMinMax.x;
        wind.windPulseMagnitude = newWindSpeed;
        terrain.terrainData.wavingGrassStrength = newGrassSpeed;
        terrain.terrainData.wavingGrassAmount = newGrassBend;
    }
}
