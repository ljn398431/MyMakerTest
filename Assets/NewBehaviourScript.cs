using UnityEngine;
using System.Collections;

public class NewBehaviourScript : ScriptableObject {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnMyClick(int a)
    {
        Debug.Log(a);
    }
    public int MyValue(int a)
    {
        return a;
    }
}
