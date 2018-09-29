using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class MuseGrapher : MonoBehaviour {

    public GameObject graphPrefab;
    public List<Color32> colors = new List<Color32>() { Color.white, Color.blue, Color.red, Color.cyan, Color.green, Color.magenta, Color.yellow };


    private Dictionary<string, LineGraph> graphs = new Dictionary<string, LineGraph>();
    private Dictionary<string, List<float>> graphUpdates = new Dictionary<string, List<float>>();

    // handle threads
    private Thread mainThread = null;

    // locks
    private static Object graphUpdateLock = new Object();

    private void Awake() {
        mainThread = Thread.CurrentThread;
    }

    private void Start() {
        MuseManager.Inst.MetricUpdate += HandleMetricUpdate;
        MuseManager.Inst.SensorMeasureUpdate += SensorMeasureUpdate;
    }

    private void Update() {
        lock (graphUpdateLock) {
            foreach (KeyValuePair<string, List<float>> kvp in graphUpdates) {
                for (int i = 0; i < kvp.Value.Count; ++i)
                    GetGraph(kvp.Key).sample = kvp.Value[i];
            }
            graphUpdates.Clear();
        }
    }

    private void HandleMetricUpdate(string metric, float value) {
        UpdateSample(metric, value);
    }

    private void SensorMeasureUpdate(string metric, float v1, float v2, float v3, float v4) {
        UpdateSample(metric + "-S1", v1);
        UpdateSample(metric + "-S2", v2);
        UpdateSample(metric + "-S3", v3);
        UpdateSample(metric + "-S4", v4);
    }

    private void UpdateSample(string metric, float sample) {
        if (Thread.CurrentThread != mainThread) {
            lock (graphUpdateLock) {
                if (!graphUpdates.ContainsKey(metric))
                    graphUpdates.Add(metric, new List<float>());
                graphUpdates[metric].Add(sample);
            }
        }
        else {
            GetGraph(metric).sample = sample;
        }
    }

    private LineGraph GetGraph(string name) {
        LineGraph graph = null;
        if(!graphs.TryGetValue(name, out graph)) {
            graph = CreateGraph(name);
            graph.color = colors[graphs.Count % colors.Count];
            graphs.Add(name, graph);
        }
        return graph;
    }

    private LineGraph CreateGraph(string name) {
        GameObject newGraphGO = GameObject.Instantiate(graphPrefab) as GameObject;
        newGraphGO.name = "Graph " + name;
        newGraphGO.transform.parent = transform;
        newGraphGO.transform.localPosition = Vector3.up * graphs.Count * 2f;
        newGraphGO.transform.rotation = transform.rotation;
        newGraphGO.transform.localScale = Vector3.one;

        return newGraphGO.GetComponent<LineGraph>();
    }

}
