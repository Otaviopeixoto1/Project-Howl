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
    private Vector2Int renderResolution = new Vector2Int(642,362);
    private Vector3 targetLastPosition;

    void Start()
    {
        if (target != null)
        {
            targetLastPosition = target.transform.position;
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
        

        Vector3 displacement = target.transform.position - targetLastPosition;

        float dist = displacement.magnitude;
        //if (dist > 0.1f)
        {
            //displacement amount in pixels:
            float x =  Vector3.Dot(displacement, transform.right) * xPixelsPerUnit;
            float y =  Vector3.Dot(displacement, transform.up) * xPixelsPerUnit;
        
            //floor
            int _x = Mathf.RoundToInt( Vector3.Dot(displacement, transform.right) * xPixelsPerUnit);
            int _y = Mathf.RoundToInt( Vector3.Dot(displacement, transform.up) * yPixelsPerUnit);


            float _z = Vector3.Dot(displacement, transform.forward);
            //Debug.Log(displacement);
            Vector3 correctedDisplacement = (_x/xPixelsPerUnit) * transform.right  
                                            + (_y/yPixelsPerUnit) * transform.up
                                            + (_z) * transform.forward;

            //Vector3 pixelDisplacement = transform.InverseTransformDirection(displacement);
            //pixelDisplacement.x *= xPixelsPerUnit; //displacement amount in screen pixels
            //pixelDisplacement.y *= yPixelsPerUnit;

            //pixelSmoother

            transform.Translate(correctedDisplacement, Space.World);
            Vector2 pixelOffset = new Vector2(_x - x, _y - y);
            //Debug.Log(x + ", " + y + "  " + _x + ", " + _y);
            pixelSmoother.SetPixelOffset(pixelOffset);

            //corretct this. the target last position changes when snapping and so it changes the displacement
            targetLastPosition = correctedDisplacement + targetLastPosition; 
        }
    }
}
