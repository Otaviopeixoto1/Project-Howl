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
    
    //private Vector3 viewerLastPos;


    private Light mainLight;
    private Vector2 cloudTileSize; //world Size of the cloud texture
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
            //viewerLastPos = viewer.transform.position;
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
        if (distanceSqr >  1) // 15 * 22.175789973 = 332,64 (15 and 22.175789973 = 10% of the tile x and y sizes)
        {
            Vector3 offset = viewer.transform.position - transform.position;
            transform.position = viewer.transform.position;
            textureOffset += new Vector4( offset.x/projectedTileSize.x, offset.z/projectedTileSize.y, 0, 0);
            cloudRenderMaterial.SetVector("_Offset", textureOffset);
        }

    }
    float GetHorizonAngle()
    {
        return Mathf.Abs(Vector3.SignedAngle(Vector3.up, transform.up, transform.forward));
    }
}
