using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using System;

[RequireComponent(typeof(Light))]
public class CloudManager : MonoBehaviour
{
    [SerializeField] private GameObject viewer;

    [SerializeField] private Vector2Int cloudTileSize = new Vector2Int(150, 150);
    public Vector2Int CloudTileSize 
    {
        get { return cloudTileSize; }
    }

    [SerializeField] private Vector2Int cloudNoiseSize;
    [SerializeField] private Shader cloudNoiseShader;
    private Material cloudNoiseMaterial;

    [SerializeField] private Vector2Int cloudCookieSize;
    
    [SerializeField] private Shader cloudCookieShader;
    private Material cloudCookieMaterial;

    private CustomRenderTexture cloudNoiseTexture;
    private CustomRenderTexture cloudCookieTexture;


    [SerializeField] private Vector2 windSpeed = new Vector2(0.4f, 0.4f);
    [Range(0,2)][SerializeField] private float amplitude = 0.68f; 
    [Range(1,8)][SerializeField] private int octaves = 4;
    [Range(1,100)][SerializeField] private float frequency = 22.6f;
    [Range(0.01f,10)][SerializeField] private float lacunarity = 0.47f;
    [Range(0.01f,2)][SerializeField] private float persistence = 1.56f;
    [Range(0,1)][SerializeField] private float dispersionSpeed = 0.407f;

    [SerializeField] private Texture2D cloudColorRamp;
    [Range(0,1)][SerializeField] private float cloudOpacity = 0.273f;
    [Range(0,1)][SerializeField] private float cloudDensity = 0.556f;


    private Light mainLight;
    private Bounds cookieBounds;

    private Vector2 lightSpaceOffset = Vector2.zero;

    private int offsetID  = Shader.PropertyToID("_Offset"),
                texelSizeId = Shader.PropertyToID("_TexelSize"),
                lightMatrixID = Shader.PropertyToID("_LightMatrix"),
                invLightMatrixID = Shader.PropertyToID("_InvLightMatrix"),
                amplitudeID = Shader.PropertyToID("_Amplitude"),
                octavesID  = Shader.PropertyToID("_Octaves"),
                frequencyID = Shader.PropertyToID("_Frequency"),
                lacunariryID  = Shader.PropertyToID("_Lacunarity"),
                persistenceID = Shader.PropertyToID("_Persistence"),
                dispersionSpeedID = Shader.PropertyToID("_DispersionSpeed"),

                opacityID = Shader.PropertyToID("_Opacity"),
                densityID = Shader.PropertyToID("_Density"),

                colorRampID = Shader.PropertyToID("_ColorRamp"),
                mainTexID = Shader.PropertyToID("_MainTex");

    void Start()
    {
        mainLight = GetComponent<Light>();

        UniversalAdditionalLightData lightData = GetComponent<UniversalAdditionalLightData>();
        lightData.lightCookieSize = cloudTileSize;
        if (viewer != null)
        {
            transform.position = viewer.transform.position - transform.forward * 10f;
        }
        
        cookieBounds = new Bounds(Vector2.zero, new Vector3(cloudTileSize.x, cloudTileSize.y, 500) * 0.6f);

        if (cloudNoiseShader != null && cloudCookieShader != null)
        {
            cloudNoiseMaterial = new Material(cloudNoiseShader);
            cloudNoiseMaterial.SetVector(offsetID, Vector4.zero);
            cloudNoiseMaterial.SetVector(texelSizeId, new Vector4(1/(float)cloudCookieSize.x, 1/(float)cloudCookieSize.y, cloudCookieSize.x, cloudCookieSize.y));
            cloudNoiseTexture = new CustomRenderTexture(cloudNoiseSize.x, cloudNoiseSize.y);
            cloudNoiseTexture.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            cloudNoiseTexture.depthStencilFormat = GraphicsFormat.None;
            cloudNoiseTexture.wrapMode = TextureWrapMode.Repeat;
            cloudNoiseTexture.filterMode = FilterMode.Bilinear;
            cloudNoiseTexture.material = cloudNoiseMaterial;
            cloudNoiseTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;
            

            cloudCookieMaterial = new Material(cloudCookieShader);
            cloudCookieMaterial.SetTexture(mainTexID, cloudNoiseTexture);
            cloudCookieMaterial.SetTexture(colorRampID, cloudColorRamp);
            cloudCookieMaterial.SetVector(texelSizeId, new Vector4(1/(float)cloudCookieSize.x, 1/(float)cloudCookieSize.y, cloudCookieSize.x, cloudCookieSize.y));
            cloudCookieTexture = new CustomRenderTexture(cloudCookieSize.x, cloudCookieSize.y, RenderTextureFormat.ARGB32);
            cloudCookieTexture.depthStencilFormat = GraphicsFormat.None;
            cloudCookieTexture.wrapMode = TextureWrapMode.Clamp;
            cloudCookieTexture.filterMode = FilterMode.Point;
            cloudCookieTexture.material = cloudCookieMaterial;
            cloudCookieTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;

            mainLight.cookie = cloudCookieTexture;

            UpdateParameters();
            Shader.SetGlobalTexture("_cloudNoiseTex", cloudNoiseTexture);
        }

        
    }
    void OnValidate()
    {
        if (cloudNoiseMaterial != null)
        {
            UpdateParameters();
        }
    }

    void Update()
    {
        if (cloudNoiseMaterial == null) {return;}

        Vector3 lightSpacePosition = transform.InverseTransformPoint(viewer.transform.position + Vector3.up * 10);
        //lightSpacePosition.z = 0;

        cloudNoiseMaterial.SetMatrix(lightMatrixID, mainLight.transform.worldToLocalMatrix);
        cloudNoiseMaterial.SetMatrix(invLightMatrixID, mainLight.transform.localToWorldMatrix);
        
        // Move the light close to the player
        float distanceSqr = cookieBounds.SqrDistance(lightSpacePosition);
        if (distanceSqr >  1)
        {
            transform.Translate(lightSpacePosition);
            lightSpaceOffset.x += lightSpacePosition.x/cloudTileSize.x;
            lightSpaceOffset.y += lightSpacePosition.y/cloudTileSize.y;
        }
        
        // Transform world space speed into light space:
        Vector3 lightSpaceSpeed = transform.InverseTransformDirection(new Vector3(windSpeed.x, 0, windSpeed.y));
        lightSpaceOffset.x -= 0.001f * Time.deltaTime * lightSpaceSpeed.x;
        lightSpaceOffset.y -= 0.001f * Time.deltaTime * lightSpaceSpeed.y;
        
        // Snap the displacement to the nearest texel
        Vector4 lightSpaceDisplacement = Vector4.zero;
        lightSpaceDisplacement.x = Mathf.Round(lightSpaceOffset.x * (float)cloudCookieSize.x) / (float)cloudCookieSize.x;
        lightSpaceDisplacement.y = Mathf.Round(lightSpaceOffset.y * (float)cloudCookieSize.y) / (float)cloudCookieSize.y;
        
        //DO SAME FOR THE TIME AXIS
        //TIME EVOLUTION = time * 0.1f * _DispersionSpeed

        Vector3 diff = Vector3.zero;
        diff.x = (lightSpaceDisplacement.x - lightSpaceOffset.x) * cloudTileSize.x;
        diff.y = (lightSpaceDisplacement.y - lightSpaceOffset.y) * cloudTileSize.y;

        //Smooth out the snapped movement by moving the light (the light cookie position will also move the same amount) in subtexel displacements:
        transform.Translate(diff);
        cloudNoiseMaterial.SetVector(offsetID, lightSpaceDisplacement);
        
        //the subtexel displacements have to be applied to the total offset as well
        lightSpaceOffset.x += diff.x / (float)cloudTileSize.x;
        lightSpaceOffset.y += diff.y / (float)CloudTileSize.y;
    }

    void UpdateParameters()
    {
        //Doesnt matter since the texture is not regenerated anyway
        cloudCookieSize.x = Math.Max(cloudCookieSize.x, 1);
        cloudCookieSize.y = Math.Max(cloudCookieSize.y, 1);

        cloudNoiseSize.x = Math.Max(cloudNoiseSize.x, 1);
        cloudNoiseSize.y = Math.Max(cloudNoiseSize.y, 1);

        cloudTileSize.x = Math.Max(cloudTileSize.x, 1);
        cloudTileSize.y = Math.Max(cloudTileSize.y, 1);
        
        

        cloudNoiseMaterial.SetFloat(amplitudeID, amplitude);
        cloudNoiseMaterial.SetInt(octavesID, octaves);
        cloudNoiseMaterial.SetFloat(frequencyID, frequency);
        cloudNoiseMaterial.SetFloat(lacunariryID, lacunarity);
        cloudNoiseMaterial.SetFloat(persistenceID, persistence);
        cloudNoiseMaterial.SetFloat(dispersionSpeedID, dispersionSpeed);

        cloudCookieMaterial.SetFloat(opacityID, cloudOpacity);
        cloudCookieMaterial.SetFloat(densityID, cloudDensity);
    }

    void OnDestroy()
    {
        cloudNoiseTexture.Release();
        cloudCookieTexture.Release();
        Destroy(cloudNoiseMaterial);
        Destroy(cloudCookieMaterial);
    }

}
