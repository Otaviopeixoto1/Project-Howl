using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light), typeof(Volume))]
public class GodrayGizmos : MonoBehaviour
{
    private Light mainLight;
    private RenderParams rp;
    private Mesh quadMesh;
    [SerializeField] private Camera mainCamera;

    private Volume volume;
    

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) {return;}
        mainLight = GetComponent<Light>();
        volume = GetComponent<Volume>();
        GodrayVolumeComponent godraySettings;
        volume.profile.TryGet<GodrayVolumeComponent>(out godraySettings);
        if (godraySettings == null) {return;}

        int samples = (int)godraySettings.samples;
        float start = (float)godraySettings.start;
        float end = (float)godraySettings.end;

        Color tempColor = Gizmos.color;
        Matrix4x4 tempMat = Gizmos.matrix;

        //Drawing camera frustum:
        float cameraHeight = mainCamera.orthographicSize * 2.0f;
        float cameraWidth =  cameraHeight * mainCamera.aspect;

        Vector3 frustumScale = new Vector3(cameraWidth, cameraHeight, mainCamera.farClipPlane - mainCamera.nearClipPlane);
        Vector3 frustumCenter = (mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.farClipPlane)) 
                                + mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.nearClipPlane))) / 2;

        Gizmos.matrix = Matrix4x4.TRS(frustumCenter, mainCamera.transform.rotation, Vector3.one);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, frustumScale);

        
        Vector3 lightDir = mainLight.transform.forward;
        Vector3 tangent = Vector3.Cross(lightDir, -mainCamera.transform.forward); 
        Vector3 normal = Vector3.Cross(tangent, lightDir).normalized; 
        

        float planeSeparation = (end - start) * (mainCamera.farClipPlane - mainCamera.nearClipPlane)/samples;
        float planeOffset = start * (mainCamera.farClipPlane - mainCamera.nearClipPlane) + mainCamera.nearClipPlane;

        Quaternion rotation = Quaternion.LookRotation(normal);
        Vector3 planeCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, planeOffset));
        Vector3 planeScale = new Vector3(
            1f/Mathf.Abs(Vector3.Dot((normal - Vector3.Dot(normal, mainCamera.transform.up) * mainCamera.transform.up).normalized, mainCamera.transform.forward)), 
            1f/Mathf.Abs(Vector3.Dot((normal - Vector3.Dot(normal, mainCamera.transform.right) * mainCamera.transform.right).normalized, mainCamera.transform.forward)), 
            1f 
        );
        
        Gizmos.color = Color.red;
        for (int i = 0; i < samples; i++)
        {
            Gizmos.matrix = Matrix4x4.TRS(planeCenter + mainCamera.transform.forward * (planeSeparation * i), rotation, planeScale);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(cameraWidth, cameraHeight, 0.01f));
            Gizmos.DrawLine(new Vector3(-cameraWidth/2,-cameraHeight/2, 0), new Vector3(cameraWidth/2,cameraHeight/2, 0));
        }
        
        
        



        Gizmos.color = tempColor;
        Gizmos.matrix = tempMat;
        
    }

    void OnValidate()
    {
    }

    void Start()
    {
        mainLight = GetComponent<Light>();
    }


}
