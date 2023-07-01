using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelCameraSmooth : MonoBehaviour
{
    private Camera canvasCamera;
    private Vector3 cameraCenter;
    
    void Start()
    {
        canvasCamera = GetComponent<Camera>();
        cameraCenter = transform.localPosition;
    }
    public void SetPixelOffset(Vector2 offset)
    {
        transform.localPosition = cameraCenter - new Vector3(offset.x * 16/9f , offset.y,0);
    }
}
