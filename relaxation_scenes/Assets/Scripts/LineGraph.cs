using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineGraph : MonoBehaviour {

	private LineRenderer myLineRenderer = null;
	private Queue<float> dataPoints = new Queue<float>();
	[SerializeField] private int numDataPoints = 100;

	[SerializeField] private float takeDataRate = 10f;
	private float takeDataCooldown = 0f;

	public float sample = 0f;
    public Color32 color { set { if (myLineRenderer) { myLineRenderer.startColor = myLineRenderer.endColor = value; } } }


	private void Awake () {
		myLineRenderer = GetComponent<LineRenderer>();
	} // End of Awake().
	

	private void Update () {
		takeDataCooldown -= Time.deltaTime;
		while(takeDataCooldown <= 0f){
            if(takeDataRate > 0f)
    			takeDataCooldown += (1f / takeDataRate);

			dataPoints.Enqueue(sample);

			if(dataPoints.Count > numDataPoints)
				dataPoints.Dequeue();
		}

		Vector3[] graphPositions = new Vector3[dataPoints.Count];
        int i = 0;
        foreach(float dataPt in dataPoints) {
            graphPositions[i] = transform.TransformPoint((Vector3.right * i) + (Vector3.up * dataPt * 1f));
            ++i;
        }

		myLineRenderer.positionCount = graphPositions.Length;
		myLineRenderer.SetPositions(graphPositions);
		
	} // End of Update().

} // End of LineGraph.
