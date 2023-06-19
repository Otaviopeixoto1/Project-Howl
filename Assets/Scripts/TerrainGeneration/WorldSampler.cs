using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//In threads, all textures must be converted to arrays before sampling
//bake the heightmaps into the textures as well !
//At runtime, only use the heightmaps

//Sample the maps using the avarage height based on distance from sampling point and map pixel center
//("bilinear filtering")

//add a debug mode with a different color to each biome to identify them at runtime



//Samples the entire map biomes and height for generating meshes. 
[RequireComponent(typeof(BiomeManager))]
public class WorldSampler : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer displayRenderer;
    [SerializeField]
    private MeshFilter displayFilter;
    [SerializeField]
    private Material displayMaterial;




    [SerializeField]
    private BiomeManager biomeManager;
     

    [SerializeField]
    [Range(1,20)]
    private float heightMultiplier = 1f;

    [SerializeField]
    private int biomeMapSize = 240;

    [SerializeField]
    [Range(1,241)]
    private int meshSize = 241;
    [SerializeField]
    [Range(0.01f,5f)]
    private float meshScale = 1f;
    [SerializeField]
    [Range(0.01f,1)]
    private float biomeMapScale = 1f;






    //this method will be a problem in threads because of heightCurves. 
    //Implement heightCurves from Scratch
    public float SampleHeight(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return 0f;
        }

        BiomeSampler fullBiomeMap = biomeManager.GetFullBiomeSampler();


                                            //use encoder/decoder for these values
        int cellId = Mathf.RoundToInt(fullBiomeMap.SampleBiomeNearest(x,y).r * 24f);
        BiomeSampler biomeSampler = biomeManager.GetBiomeSampler(cellId);
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        float finalHeight = biomeSampler.SampleHeight(x,y) * cellValue; 
                                        //use the width and height offset for sampling heightmap
        float totalValue = cellValue;


        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeManager.GetNeighbours(cellId))
            {
                BiomeSampler neighbourSampler = biomeManager.GetBiomeSampler(neighbourId);
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                float nheight = neighbourSampler.SampleHeight(x,y) * nCellValue;
                totalValue += nCellValue;
                finalHeight += nheight;
            }
        }                                   //last change
        finalHeight /= (totalValue + 0.01f); // this can cause errors in some cases (division by zero)

        return finalHeight * heightMultiplier;
    }

    public Color SampleColor(float _x, float _y)
    {
        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return Color.black;
        }

        BiomeSampler fullBiomeMap = biomeManager.GetFullBiomeSampler();

                                                    //use encoder/decoder for these values
        int cellId = Mathf.RoundToInt(fullBiomeMap.SampleBiomeNearest(x,y).r * 24f);
        BiomeSampler biomeSampler = biomeManager.GetBiomeSampler(cellId);
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        Color finalColor = biomeSampler.displayColor * cellValue;
        float totalValue = cellValue;


        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeManager.GetNeighbours(cellId))
            {
                BiomeSampler neighbourSampler = biomeManager.GetBiomeSampler(neighbourId);
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                Color nColor = neighbourSampler.displayColor * nCellValue;
                totalValue += nCellValue;
                finalColor += nColor;
            }
        }
        finalColor /= (totalValue + 0.01f); // last change

        return finalColor;
    }



    public void DisplayTerrainMesh()// get world coordinates as input !
    {
        if (biomeManager != null)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(
                this,
                meshSize,
                meshSize,
                meshScale,
                Vector2.zero // offset in world coordinates

            );
            displayFilter.sharedMesh = meshData.CreateMesh();
            displayRenderer.sharedMaterial = displayMaterial;
            DisplayMap();

        }
    }

    public void DisplayMap()
    {
        if (displayRenderer == null)
        {
            return;
        }
        Texture2D texture = new Texture2D(241,241);

        Color[] colorMap = new Color[241 * 241];

        for (int x = 0; x < 241; x++)
        {
            for (int y = 0; y < 241; y++)
            {
                
                colorMap[x + y * 241] = SampleColor(x,y);
            }
        }
        texture.SetPixels(colorMap);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();


        displayRenderer.sharedMaterial.mainTexture = texture;
    }




    void OnValidate()
    {

    }

    void Start()
    {
        biomeManager = GetComponent<BiomeManager>();
    }

    void Update()
    {
        
    }
}
