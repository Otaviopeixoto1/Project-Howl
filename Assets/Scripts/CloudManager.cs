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
    private Bounds cookieBounds;

    private Vector4 textureOffset = Vector4.zero;

    void Start()
    {
        mainLight = GetComponent<Light>();

        UniversalAdditionalLightData lightData = GetComponent<UniversalAdditionalLightData>();
        cloudTileSize = lightData.lightCookieSize;
        if (viewer != null)
        {
            transform.position = viewer.transform.position - transform.forward * 10f;
        }

        cloudRenderMaterial.SetVector("_Offset", Vector4.zero);
        cookieBounds = new Bounds(Vector2.zero, new Vector3(cloudTileSize.x, cloudTileSize.y, 500) * 0.6f);
    }

    void Update()
    {
        Vector3 lightSpacePosition = transform.InverseTransformPoint(viewer.transform.position);
        lightSpacePosition.z = 0;


        float distanceSqr = cookieBounds.SqrDistance(lightSpacePosition);
        if (distanceSqr >  1)
        {
            transform.Translate(lightSpacePosition);
            textureOffset += new Vector4(lightSpacePosition.x/cloudTileSize.x, lightSpacePosition.y/cloudTileSize.y, 0, 0);
            cloudRenderMaterial.SetVector("_Offset", textureOffset);
        }

    }
    float GetHorizonAngle()
    {
        return Mathf.Abs(Vector3.SignedAngle(Vector3.up, transform.up, transform.forward));
    }
}
