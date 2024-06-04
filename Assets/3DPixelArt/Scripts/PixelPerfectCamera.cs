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
    [SerializeField] private Vector2Int renderResolution = new Vector2Int(640,360);
    private Camera mainCamera;

    private float cameraHeight; //camera height in world units
    private float cameraWidth; //camera width in world units

    //Get this from the camera component's render texture
    
    private Vector3 targetLastPosition;


    private Vector3 origin = Vector3.zero;
    private float zdist =0f;

    Vector3 coord;
    void Start()
    {
        if (target != null)
        {
            targetLastPosition = target.transform.position;
            origin = transform.position;
            zdist = Vector3.Distance(target.transform.position, origin);
            coord = targetLastPosition;
        }
        mainCamera = GetComponent<Camera>();
        cameraHeight = mainCamera.orthographicSize * 2f;
        cameraWidth = cameraHeight * (16/9f);
    }

    void Update()
    {
        if (target == null)
        {
            return;
        }
        cameraHeight = mainCamera.orthographicSize * 2;
        cameraWidth = cameraHeight * (16/9f);

        float xPixelsPerUnit = (renderResolution.x/cameraWidth); 
        float yPixelsPerUnit = renderResolution.y/cameraHeight;
        
        //Debug.Log(xPixelsPerUnit +", " +  yPixelsPerUnit);
        Vector3 targetCoord = target.transform.position - origin;

        float xc =  Vector3.Dot(targetCoord, transform.right) * xPixelsPerUnit;
        float yc =  Vector3.Dot(targetCoord, transform.up) * yPixelsPerUnit;
        float z =  Vector3.Dot(targetCoord, transform.forward);

        float _xc = xc;
        float _yc = yc;

        if (pixelSnap)
        {
            _xc = Mathf.RoundToInt(xc);
            _yc = Mathf.RoundToInt(yc);
        }
        

        Vector3 correctedPos = (_xc/xPixelsPerUnit) * transform.right  
                                + (_yc/yPixelsPerUnit) * transform.up
                                + (z - zdist) * transform.forward ;


        Vector2 pixelOffset = new Vector2(_xc - xc,  _yc - yc);
        
        transform.position = correctedPos + origin;
        
        /////////////////////////////////////////////////////////////////////////////////////////
        // This is still needs one aditional correction. the other camera is rendering a texture which
        //has the size of (renderResolution - 1) (maybe renderResolution/(renderResolution - 1))
        //////////////////////////////////////////////////////////////////////////////////////////

        if (cameraSmooth)
        {
            pixelSmoother.SetPixelOffset(pixelOffset); 
        }
        
        

        origin = transform.position;
        
    }
}
