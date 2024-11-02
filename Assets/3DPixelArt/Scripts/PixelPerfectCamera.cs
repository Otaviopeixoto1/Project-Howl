using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCamera : MonoBehaviour
{
    [SerializeField] private bool pixelSnap = true;
    [SerializeField] private bool cameraSmooth = true;
    [SerializeField] private GameObject target;
    [SerializeField] private PixelCameraSmooth pixelSmoother; 
    private Camera mainCamera;

    private float cameraHeight; //camera height in world units
    private float cameraWidth; //camera width in world units

    //Get this from the camera component's render texture
    
    private Vector3 origin = Vector3.zero;
    private float zdist = 0.0f;


    void Start()
    {
        if (target != null)
        {
            origin = transform.position;
            zdist = Vector3.Distance(target.transform.position, origin);
        }
        mainCamera = GetComponent<Camera>();
        cameraHeight = mainCamera.orthographicSize * 2f;
        cameraWidth = cameraHeight * (16/9f);
    }

    void OnValidate()
    {
        if (!pixelSnap) {cameraSmooth = false;}
    }
    void Update()
    {
        if (target == null)
        {
            return;
        }
        cameraHeight = mainCamera.orthographicSize * 2;
        cameraWidth = cameraHeight * (16/9f);

        float xPixelsPerUnit = (mainCamera.targetTexture.width/cameraWidth); 
        float yPixelsPerUnit = mainCamera.targetTexture.height/cameraHeight;
        
        Vector3 targetCoord = target.transform.position - origin;

        float xc =  Vector3.Dot(targetCoord, transform.right) * xPixelsPerUnit;
        float yc =  Vector3.Dot(targetCoord, transform.up) * yPixelsPerUnit;
        float z =  Vector3.Dot(targetCoord, transform.forward);

        if (pixelSnap)
        {
            float _xc = Mathf.RoundToInt(xc);
            float _yc = Mathf.RoundToInt(yc);

            Vector3 correctedPos = (_xc/xPixelsPerUnit) * transform.right  
                                + (_yc/yPixelsPerUnit) * transform.up
                                + (z - zdist) * transform.forward ;


            Vector2 subPixelOffset = new Vector2(_xc - xc,  _yc - yc);
            
            transform.position = correctedPos + origin;
            
            /////////////////////////////////////////////////////////////////////////////////////////
            // This is still needs one aditional correction. the other camera is rendering a texture which
            //has the size of (renderResolution - 1) (maybe renderResolution/(renderResolution - 1))
            //////////////////////////////////////////////////////////////////////////////////////////

            if (cameraSmooth)
            {
                pixelSmoother.SetPixelOffset(subPixelOffset, new Vector2Int((int)_xc, (int)_yc)); 
            }

            origin = transform.position;
        }
        
    }
}
