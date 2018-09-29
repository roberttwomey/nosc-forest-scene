using UnityEngine;
using System.Collections;

public static class MathUtilities {

	public static Vector3 RandomVector3Cube(float radius){
	    return new Vector3(Random.Range(-radius, radius),
                           Random.Range(-radius, radius),
                           Random.Range(-radius, radius));
	} // End of RandomVector3Cube().


    // Rotates a vector in a random direction by the deviation.
    public static Quaternion SkewRot(Quaternion rot, float deviation){
        Vector3 randomVect = Random.rotation * Vector3.forward;
        Vector3 randomPerpVect = Vector3.Cross(rot * Vector3.forward, randomVect);
        return rot * Quaternion.AngleAxis(deviation, randomPerpVect);
    } // End of DeviateVector().


    public static Rect CenteredSquare(float x, float y, float size){
        return new Rect(x - (size * 0.5f), Screen.height - (y + (size * 0.5f)), size, size);
    }

	public static Rect CenteredRect(float centerX, float centerY, float width, float height){
        return new Rect(centerX - (width * 0.5f), Screen.height - (centerY + (height * 0.5f)), width, height);
    }

	/*
    public static Rect CenteredSquare(Node node){
        Vector3 nodeScreenPos = Camera.main.WorldToScreenPoint(node.worldPosition);
        return CenteredSquare(nodeScreenPos.x, nodeScreenPos.y, 2000f / Vector3.Distance(Camera.main.transform.position, node.worldPosition));
    }
    public static Rect CenteredSquare(Assembly assembly){
        Vector3 assemblyScreenPos = Camera.main.WorldToScreenPoint(assembly.physicsObject.transform.position);
        return CenteredSquare(assemblyScreenPos.x, assemblyScreenPos.y, 12000f / Vector3.Distance(Camera.main.transform.position, assembly.physicsObject.transform.position));
    }// End of CenteredSquare().
	*/


	public static Vector3[] FibonacciSphere(int samples, bool randomize = true){
		Vector3[] points = new Vector3[samples];

		float rnd = 1f;
		if(randomize)
			rnd = Random.Range(0f, samples);

		float offset = 2f / samples;
		float increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

		for(int i = 0; i < samples; i++){
			float y = ((i * offset) - 1f) + (offset / 2f);
			float r = Mathf.Sqrt(1f - Mathf.Pow(y, 2f));
			float phi = ((i + rnd) % samples) * increment;
			float x = Mathf.Cos(phi) * r;
			float z = Mathf.Sin(phi) * r;
			points[i] = new Vector3(x, y, z);
		}

		return points;
	} // End of FibonacciSphere().


	/*
	def fibonacci_sphere(samples=1,randomize=True):
    rnd = 1.
    if randomize:
        rnd = random.random() * samples

    points = []
    offset = 2./samples
    increment = math.pi * (3. - math.sqrt(5.));

    for i in range(samples):
        y = ((i * offset) - 1) + (offset / 2);
        r = math.sqrt(1 - pow(y,2))

        phi = ((i + rnd) % samples) * increment

        x = math.cos(phi) * r
        z = math.sin(phi) * r

        points.append([x,y,z])

    return points
	*/

	public static Color SetAlpha(this Color color, float alpha){
		return new Color(color.r, color.g, color.b, alpha);
	} // End of SetAlpha().

	// Converts Y value to Screen.height - Y. Useful for stupid Unity-native mouse positions.
	public static Vector3 ScreenFixY(this Vector3 vector){
		return new Vector3(vector.x, Screen.height - vector.y, vector.z);
	} // End of InvertX().


	public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3){
		float u = 1f - t;
		float tt = t * t;
		float uu = u * u;
		float uuu = uu * u;
		float ttt = tt * t;
 
		Vector3 p = uuu * p0; //first term
		p += 3 * uu * t * p1; //second term
		p += 3 * u * tt * p2; //third term
		p += ttt * p3; //fourth term
 
		return p;
	} // End of CalculateBezierPoint().


	public static Transform FindChildRecursively(this GameObject gameObject, string childName){
		Transform[] allChildren = gameObject.GetComponentsInChildren<Transform>();
		for(int i = 0; i < allChildren.Length; i++)
			if(allChildren[i].gameObject.name.Equals(childName))
				return allChildren[i];
		return null;
	} // End of FindChildRecursively().


	public static float LinToSmoothLerp(float input) {
		return 0.5f + ((Mathf.Sin(Mathf.PI * (input - 0.5f)) * 0.5f));
	} // End of LinearLerpToSmooth().


	public static float SmoothPingPong01(float input) {
		return Mathf.Sin(input * Mathf.PI);
	} // End of LinearLerpToSmooth().

} // End of MathUtilities.