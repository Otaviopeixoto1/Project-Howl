using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public bool autoUpdate = true;

    public virtual void OnMapUpdate()
    {
        return;
    }
}
