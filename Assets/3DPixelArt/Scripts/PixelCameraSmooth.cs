using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

[RequireComponent(typeof(Camera))]
public class PixelCameraSmooth : MonoBehaviour
{
    private Camera canvasCamera;
    private Vector3 cameraCenter;
    Vector2Int nextOffset = Vector2Int.zero;
    Vector2Int currentOffset = Vector2Int.zero;
    static int cameraMovementId = Shader.PropertyToID("mainCameraMotion");
    
    void Start()
    {
        canvasCamera = GetComponent<Camera>();
        cameraCenter = transform.localPosition;
    }
    public void SetPixelOffset(Vector2 subPixelOffset, Vector2Int pixelOffset)
    {
        nextOffset = pixelOffset;
        //Set as global
        Shader.SetGlobalVector(cameraMovementId, new Vector4(pixelOffset.x, pixelOffset.y, 0, 0));
        transform.localPosition = cameraCenter - new Vector3(subPixelOffset.x * 16.0f/9.0f, subPixelOffset.y, 0);
    }
}
