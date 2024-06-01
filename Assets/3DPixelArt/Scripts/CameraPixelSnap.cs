using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraPixelSnap : MonoBehaviour
{
    [SerializeField]
    private GameObject target;

    private Camera mainCamera;
    [SerializeField]
    private PixelCameraSmooth pixelSmoother; 


    private float cameraHeight; //camera height in world units
    private float cameraWidth; //camera width in world units

    //Get this from the camera component's render texture
    [SerializeField]
    private Vector2Int renderResolution = new Vector2Int(640,360);
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

        int _xc = Mathf.RoundToInt(xc);
        int _yc = Mathf.RoundToInt(yc);

        Vector3 correctedPos = (_xc/xPixelsPerUnit) * transform.right  
                                        + (_yc/yPixelsPerUnit) * transform.up
                                        + (z - zdist) * transform.forward ;


        Vector2 pixelOffset = new Vector2(_xc - xc,  _yc - yc);
        
        transform.position = correctedPos + origin;
        
        //Debug.Log(xc + ", " + yc + "  " + _xc + ", " + _yc + " " + pixelOffset);
        pixelSmoother.SetPixelOffset(pixelOffset); //this is incorrect !!!!!
        //targetLastPosition = correctedDisplacement + targetLastPosition; 

        origin = transform.position;
        
    }
}
