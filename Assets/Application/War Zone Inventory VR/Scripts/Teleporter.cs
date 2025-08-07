using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public GameObject mainObject;

    public GameObject[] targets;

    public void teleportTo(int target)
    {
        if (target < targets.Length && target >= 0)
        {
            mainObject.transform.position = targets[target].transform.position;
            mainObject.transform.rotation = targets[target].transform.rotation;
        }
    }
}
