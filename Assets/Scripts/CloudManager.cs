using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class CloudManager : MonoBehaviour
{
    [SerializeField]
    private GameObject viewer;
    [SerializeField]
    private Material cloudRenderMaterial;


    private Light mainLight;
    private Vector2 cloudTileSize; //Size of the cloud texture in world units
    private Vector2 projectedTileSize;

    private float horizonAngle;
    private Bounds tileBounds;

    private Vector4 textureOffset = Vector4.zero;

    void Start()
    {
        mainLight = GetComponent<Light>();
        UniversalAdditionalLightData lightData = GetComponent<UniversalAdditionalLightData>();
        cloudTileSize = lightData.lightCookieSize;
        if (viewer != null)
        {
            transform.position = viewer.transform.position;
        }
        horizonAngle = GetHorizonAngle();
        projectedTileSize = new Vector2(cloudTileSize.x, cloudTileSize.y/ Mathf.Sin(horizonAngle * Mathf.PI/180));

        cloudRenderMaterial.SetVector("_Offset", Vector4.zero);
        tileBounds = new Bounds(Vector2.zero, new Vector3(projectedTileSize.x, 500, projectedTileSize.y) * 0.6f);
    }

    void Update()
    {

        float newAngle = GetHorizonAngle();
        if (newAngle != horizonAngle)
        {
            horizonAngle = GetHorizonAngle();
            projectedTileSize = new Vector2(cloudTileSize.x, cloudTileSize.y/Mathf.Sin(horizonAngle * Mathf.PI/180));
            tileBounds = new Bounds(Vector2.zero, new Vector3(projectedTileSize.x, 500, projectedTileSize.y) * 0.6f);
        }
        
        //adjust the texture sampling and the offset for y rotations !!!
        
        //Rotate the displacement vector accordingly
        Vector3 displacement = viewer.transform.position - transform.position;
        displacement.y = 0;

        float distanceSqr = tileBounds.SqrDistance(displacement);
        if (distanceSqr >  1)
        {
            //Vector3 offset = viewer.transform.position - transform.position;
            //transform.position = viewer.transform.position;
            ///////////////////////////////////////////////////////////////////////////////
            //the y displacement has to be taken into account. it becomes very appearent on steep inclinations
            //this is caused by the shadow projection of the cookie 
            //////////////////////////////////////////////////////////////////////////////
            
            transform.position += displacement;
            textureOffset += new Vector4( displacement.x/projectedTileSize.x, displacement.z/projectedTileSize.y, 0, 0);
            cloudRenderMaterial.SetVector("_Offset", textureOffset);
        }

    }
    float GetHorizonAngle()
    {
        return Mathf.Abs(Vector3.SignedAngle(Vector3.up, transform.up, transform.forward));
    }
}
