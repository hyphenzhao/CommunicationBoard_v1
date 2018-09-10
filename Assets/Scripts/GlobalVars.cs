using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVars : MonoBehaviour {
    [SerializeField]
    private GameObject point1;
    [SerializeField]
    private GameObject point2;
    [SerializeField]
    private GameObject point3;
    [SerializeField]
    private GameObject point4;

    public static float p1x, p1z;
    public static float p2x, p2z;
    public static float p3x, p3z;
    public static float p4x, p4z;
    public static float k1, k2, k3, k4, k5, k6;
    public static string remoteIP = "10.19.124.56";
    public static string remotePort = "5005";
    // Use this for initialization
    void Start () {
        k1 = 1; k2 = 0; k3 = 0;
        k4 = 0; k5 = 1; k6 = 0;
        p1x = point1.transform.localPosition.x;
        p2x = point2.transform.localPosition.x;
        p3x = point3.transform.localPosition.x;
        p4x = point4.transform.localPosition.x;
        p1z = point1.transform.localPosition.z;
        p2z = point2.transform.localPosition.z;
        p3z = point3.transform.localPosition.z;
        p4z = point4.transform.localPosition.z;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
