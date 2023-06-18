using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;




/////////////////////////////////////////////////////////////////////////////////////////////////////////
//  These classes store data that gets serialized into json files for saving and loading baked map data
/////////////////////////////////////////////////////////////////////////////////////////////////////////
[Serializable]
public struct BiomeData
{
    public int id;
    public string name;
    public string heightMapPath;
    public string biomeMapPath;
    public Color displayColor;

    public BiomeData(int id, string name,string heightMapPath, string biomeMapPath, Color displayColor)
    {
        this.id = id;
        this.name = name;
        this.heightMapPath = heightMapPath;
        this.biomeMapPath = biomeMapPath;
        this.displayColor = displayColor;
    }

}

[Serializable]
public struct BiomeMapData
{
    public int biomeGridSize;
    public BiomeData fullbiomeMapData;
    public BiomeData[] biomeMaps;
    public BiomeMapData(int biomeGridSize,BiomeData fullbiomeMapData, BiomeData[] biomeMaps)
    {
        this.biomeGridSize = biomeGridSize;
        this.fullbiomeMapData = fullbiomeMapData;
        this.biomeMaps = biomeMaps;
    }
}

[Serializable]
public class BiomeLinks //add the links to the biome map data as an array off biome indexes
{
    public int gridSize;
    public Dictionary<int, int[]> neighbours = new Dictionary<int, int[]>();
    
    public BiomeLinks(int gridSize)
    {
        this.gridSize = gridSize;
    }

    public void GenerateLinksFromGrid()
    {
        neighbours[0] = new int[]{
            1, 
            gridSize + 1, 
            gridSize + 2
        };

        neighbours[gridSize * (gridSize + 1)] = new int[]{
            (gridSize * gridSize) - 1, 
            (gridSize * gridSize), 
            gridSize * (gridSize + 1) + 1
        };

        neighbours[gridSize] = new int[]{
            gridSize - 1, 
            (2 * gridSize) + 1, 
            2 * gridSize
        };

        neighbours[gridSize * (gridSize + 2)] = new int[]{
            gridSize * (gridSize + 2) - 1, 
            gridSize * (gridSize + 1) - 1, 
            gridSize * (gridSize + 1) - 2
        };

        for (int i = 1; i < gridSize; i++)
        {
            neighbours[i] =  new int[]{
                i - 1, 
                i + 1, 
                i + gridSize,
                i + gridSize + 1,
                i + gridSize + 2,
            };

            int t = i + gridSize * (gridSize + 1);
            neighbours[t] =  new int[]{
                t - 1, 
                t + 1, 
                t - gridSize,
                t - gridSize - 1,
                t - gridSize - 2,
            };

            int l = (i) * (gridSize + 1);
            neighbours[l] =  new int[]{
                l - (gridSize + 1), 
                l + (gridSize + 1), 
                l + 1,
                l + (gridSize + 2),
                l - (gridSize), 
            };

            int r = (i) * (gridSize + 1) + gridSize;
            neighbours[r] =  new int[]{
                r - (gridSize + 1), 
                r + (gridSize + 1), 
                r - 1,
                r - (gridSize + 2),
                r + (gridSize), 
            };


        }


        for (int i = gridSize + 2; i < (gridSize * (gridSize + 1) + 1); i += gridSize + 1)
        {

            for (int j = 0; j < gridSize - 1; j++)
            {
                neighbours[i + j] =  new int[]{
                    i + j - 1, 
                    i + j + 1, 
                    i + j - (gridSize + 1), 
                    i + j + (gridSize + 1), 
                    i + j - (gridSize + 2), 
                    i + j + (gridSize + 2), 
                    i + j - (gridSize), 
                    i + j + (gridSize), 
                };
            }

        }
    }
    public void Print()
    {
        foreach(KeyValuePair<int, int[]> entry in neighbours)
        {
            string s = "{" + entry.Key+ ", " ;
            foreach (int n in entry.Value)
            {
                s += n +", ";
            }

            Debug.Log(s + "}");
        }
    }
    
}
////////////////////////////////////////////////////////////////////////////////////////////////////////