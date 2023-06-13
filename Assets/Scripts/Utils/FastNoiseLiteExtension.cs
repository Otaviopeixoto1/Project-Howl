using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

// Switch between using floats or doubles for input position
using FNLfloat = System.Single;

public struct CellularData // this is for baking all data and transmitting to a different place
{

}



public class FastNoiseLiteExtension : FastNoiseLite
{
    protected ModifiedCellularReturnType mModifiedCellularReturnType = ModifiedCellularReturnType.ModifiedCellValue;
    protected int mModifiedCellularGridDim = 4;
    protected int mModifiedCellularTilingX = 4;
    protected int mModifiedCellularTilingY = 4;
    protected int[] cellularVectors;


    public void SetModifiedCellularReturnType(ModifiedCellularReturnType modCellularReturnType) { mModifiedCellularReturnType = modCellularReturnType; }

    /// <summary>
    /// Sets return type from modified cellular noise calculations
    /// </summary>
    /// <remarks>
    /// Default: EuclideanSq
    /// </remarks>
    public void SetModifiedCellularGridDim(int gridDim) { mModifiedCellularGridDim = gridDim; }


    /// <summary>
    /// Sets amount of tiles before the pattern can repeat in modified cellular
    /// </summary>
    /// <remarks>
    /// Default: EuclideanSq
    /// </remarks>
    public void SetModifiedCellularTilingX(int cellularTilingX) { mModifiedCellularTilingX = cellularTilingX; }

    /// <summary>
    /// Sets amount of tiles before the pattern can repeat in modified cellular
    /// </summary>
    /// <remarks>
    /// Default: EuclideanSq
    /// </remarks>
    public void SetModifiedCellularTilingY(int cellularTilingY) { mModifiedCellularTilingY = cellularTilingY; }
    







    protected override float GenNoiseSingle(int seed, FNLfloat x, FNLfloat y)
    {
        switch (mNoiseType)
        {
            case NoiseType.OpenSimplex2:
                return SingleSimplex(seed, x, y);
            case NoiseType.OpenSimplex2S:
                return SingleOpenSimplex2S(seed, x, y);
            case NoiseType.Cellular:
                return SingleCellular(seed, x, y);
            case NoiseType.ModifiedCellular:
                return SingleModifiedCellular(seed,x,y);
            case NoiseType.Perlin:
                return SinglePerlin(seed, x, y);
            case NoiseType.ValueCubic:
                return SingleValueCubic(seed, x, y);
            case NoiseType.Value:
                return SingleValue(seed, x, y);
            default:
                return 0;
        }
    }

    protected override float GenNoiseSingle(int seed, FNLfloat x, FNLfloat y, FNLfloat z)
    {
        switch (mNoiseType)
        {
            case NoiseType.OpenSimplex2:
                return SingleOpenSimplex2(seed, x, y, z);
            case NoiseType.OpenSimplex2S:
                return SingleOpenSimplex2S(seed, x, y, z);
            case NoiseType.Cellular:
                return SingleCellular(seed, x, y, z);
            case NoiseType.ModifiedCellular:
                return SingleModifiedCellular(seed,x,y,z);
            case NoiseType.Perlin:
                return SinglePerlin(seed, x, y, z);
            case NoiseType.ValueCubic:
                return SingleValueCubic(seed, x, y, z);
            case NoiseType.Value:
                return SingleValue(seed, x, y, z);
            default:
                return 0;
        }
    }


    public Vector2[] GetCellularVectors()
    {
        int arraysize = (mModifiedCellularGridDim  + 1) * (mModifiedCellularGridDim  + 1);
        Vector2[] cellVectors = new Vector2[arraysize];

        float cellularJitter = 0.43701595f * mCellularJitterModifier;

        int xPrimed = (-mModifiedCellularGridDim/2) * PrimeX;
        int yPrimedBase = (-mModifiedCellularGridDim/2) * PrimeY;

        int i = 0; 
        for (int yi = -mModifiedCellularGridDim/2; yi <= mModifiedCellularGridDim/2; yi++)
        {
            int yPrimed = yPrimedBase;
            
            for (int xi = -mModifiedCellularGridDim/2; xi <= mModifiedCellularGridDim/2; xi++)
            {
                int hash = Hash(mSeed, xPrimed, yPrimed);
                int idx = hash & (255 << 1);

                float vecX = (float)(xi) + RandVecs2D[idx] * cellularJitter;
                float vecY = (float)(yi) + RandVecs2D[idx | 1] * cellularJitter;
                
                cellVectors[i] = new Vector2(vecX,vecY);
                i++;

                yPrimed += PrimeY;
            }
            xPrimed += PrimeX;
        }

        return cellVectors;
    }


    // Modified Cellular Noise

    protected float SingleModifiedCellular(int seed, FNLfloat x, FNLfloat y)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);

        float distance0 = float.MaxValue;
        float distance1 = float.MaxValue;
        int closestHash = 0;
        int closestX = 0;
        int closestY = 0;

        float cellularJitter = 0.43701595f * mCellularJitterModifier;

        int xPrimed = (xr - 1) * PrimeX;
        int yPrimedBase = (yr - 1) * PrimeY;

        switch (mCellularDistanceFunction)
        {
            default:
            case CellularDistanceFunction.Euclidean:
            case CellularDistanceFunction.EuclideanSq:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int hash = Hash(seed, xPrimed, yPrimed);
                        int idx = hash & (255 << 1);

                        float vecX = (float)(xi - x) + RandVecs2D[idx] * cellularJitter;
                        float vecY = (float)(yi - y) + RandVecs2D[idx | 1] * cellularJitter;

                        float newDistance = vecX * vecX + vecY * vecY;

                        distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                            closestX = xi;
                            closestY = yi;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int hash = Hash(seed, xPrimed, yPrimed);
                        int idx = hash & (255 << 1);

                        float vecX = (float)(xi - x) + RandVecs2D[idx] * cellularJitter;
                        float vecY = (float)(yi - y) + RandVecs2D[idx | 1] * cellularJitter;

                        float newDistance = FastAbs(vecX) + FastAbs(vecY);

                        distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                            closestX = xi;
                            closestY = yi;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
            case CellularDistanceFunction.Hybrid:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int hash = Hash(seed, xPrimed, yPrimed);
                        int idx = hash & (255 << 1);

                        float vecX = (float)(xi - x) + RandVecs2D[idx] * cellularJitter;
                        float vecY = (float)(yi - y) + RandVecs2D[idx | 1] * cellularJitter;

                        float newDistance = (FastAbs(vecX) + FastAbs(vecY)) + (vecX * vecX + vecY * vecY);

                        distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                        if (newDistance < distance0)
                        {
                            distance0 = newDistance;
                            closestHash = hash;
                            closestX = xi;
                            closestY = yi;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
        }

        if (mCellularDistanceFunction == CellularDistanceFunction.Euclidean && mCellularReturnType >= CellularReturnType.Distance)
        {
            distance0 = FastSqrt(distance0);

            if (mModifiedCellularReturnType >= ModifiedCellularReturnType.Distance2)
            {
                distance1 = FastSqrt(distance1);
            }
        }
        float _x = closestX + mModifiedCellularGridDim/2f; //save x and y
        float _y = closestY + mModifiedCellularGridDim/2f;
        float uVal = ((_x + _y * (mModifiedCellularGridDim + 1)) / mFrequency);

        switch (mModifiedCellularReturnType)
        {
            case ModifiedCellularReturnType.CellValue:
                return closestHash * (1 / 2147483648.0f);
            case ModifiedCellularReturnType.ModifiedCellValue:
                return uVal;
            case ModifiedCellularReturnType.Distance:
                return distance0;
            case ModifiedCellularReturnType.Distance2:
                return distance1;
            case ModifiedCellularReturnType.Distance2Add:
                return (distance1 + distance0) * 0.5f - 1;
            case ModifiedCellularReturnType.Distance2Sub:
                return distance1 - distance0;
            case ModifiedCellularReturnType.Distance2Mul:
                return distance1 * distance0 * 0.5f - 1;
            case ModifiedCellularReturnType.Distance2Div:
                return (distance0 / distance1);
            default:
                return 0;
        }
    }

    protected float SingleModifiedCellular(int seed, FNLfloat x, FNLfloat y, FNLfloat z)
    {
        int xr = FastRound(x);
        int yr = FastRound(y);
        int zr = FastRound(z);

        float distance0 = float.MaxValue;
        float distance1 = float.MaxValue;
        int closestHash = 0;
        int closestX = 0;
        int closestY = 0;
        int closestZ = 0;

        float cellularJitter = 0.39614353f * mCellularJitterModifier;

        int xPrimed = (xr - 1) * PrimeX;
        int yPrimedBase = (yr - 1) * PrimeY;
        int zPrimedBase = (zr - 1) * PrimeZ;

        switch (mCellularDistanceFunction)
        {
            case CellularDistanceFunction.Euclidean:
            case CellularDistanceFunction.EuclideanSq:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int zPrimed = zPrimedBase;

                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            int hash = Hash(seed, xPrimed, yPrimed, zPrimed);
                            int idx = hash & (255 << 2);

                            float vecX = (float)(xi - x) + RandVecs3D[idx] * cellularJitter;
                            float vecY = (float)(yi - y) + RandVecs3D[idx | 1] * cellularJitter;
                            float vecZ = (float)(zi - z) + RandVecs3D[idx | 2] * cellularJitter;

                            float newDistance = vecX * vecX + vecY * vecY + vecZ * vecZ;

                            distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                            if (newDistance < distance0)
                            {
                                distance0 = newDistance;
                                closestHash = hash;
                                closestX = xi;
                                closestY = yi;
                                closestZ = zi;
                            }
                            zPrimed += PrimeZ;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
            case CellularDistanceFunction.Manhattan:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int zPrimed = zPrimedBase;

                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            int hash = Hash(seed, xPrimed, yPrimed, zPrimed);
                            int idx = hash & (255 << 2);

                            float vecX = (float)(xi - x) + RandVecs3D[idx] * cellularJitter;
                            float vecY = (float)(yi - y) + RandVecs3D[idx | 1] * cellularJitter;
                            float vecZ = (float)(zi - z) + RandVecs3D[idx | 2] * cellularJitter;

                            float newDistance = FastAbs(vecX) + FastAbs(vecY) + FastAbs(vecZ);

                            distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                            if (newDistance < distance0)
                            {
                                distance0 = newDistance;
                                closestHash = hash;
                                closestX = xi;
                                closestY = yi;
                                closestZ = zi;
                            }
                            zPrimed += PrimeZ;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
            case CellularDistanceFunction.Hybrid:
                for (int xi = xr - 1; xi <= xr + 1; xi++)
                {
                    int yPrimed = yPrimedBase;

                    for (int yi = yr - 1; yi <= yr + 1; yi++)
                    {
                        int zPrimed = zPrimedBase;

                        for (int zi = zr - 1; zi <= zr + 1; zi++)
                        {
                            int hash = Hash(seed, xPrimed, yPrimed, zPrimed);
                            int idx = hash & (255 << 2);

                            float vecX = (float)(xi - x) + RandVecs3D[idx] * cellularJitter;
                            float vecY = (float)(yi - y) + RandVecs3D[idx | 1] * cellularJitter;
                            float vecZ = (float)(zi - z) + RandVecs3D[idx | 2] * cellularJitter;

                            float newDistance = (FastAbs(vecX) + FastAbs(vecY) + FastAbs(vecZ)) + (vecX * vecX + vecY * vecY + vecZ * vecZ);

                            distance1 = FastMax(FastMin(distance1, newDistance), distance0);
                            if (newDistance < distance0)
                            {
                                distance0 = newDistance;
                                closestHash = hash;
                                closestX = xi;
                                closestY = yi;
                                closestZ = zi;
                            }
                            zPrimed += PrimeZ;
                        }
                        yPrimed += PrimeY;
                    }
                    xPrimed += PrimeX;
                }
                break;
            default:
                break;
        }

        if (mCellularDistanceFunction == CellularDistanceFunction.Euclidean && mCellularReturnType >= CellularReturnType.Distance)
        {
            distance0 = FastSqrt(distance0);

            if (mModifiedCellularReturnType >= ModifiedCellularReturnType.Distance2)
            {
                distance1 = FastSqrt(distance1);
            }
        }

        switch (mModifiedCellularReturnType)
        {
            case ModifiedCellularReturnType.CellValue:
                return closestHash * (1 / 2147483648.0f);
            case ModifiedCellularReturnType.ModifiedCellValue:
                return (closestX*mModifiedCellularTilingX + closestY* mModifiedCellularTilingY +closestZ) * (1 / 2147483648.0f);
            case ModifiedCellularReturnType.Distance:
                return distance0 - 1;
            case ModifiedCellularReturnType.Distance2:
                return distance1 - 1;
            case ModifiedCellularReturnType.Distance2Add:
                return (distance1 + distance0) * 0.5f - 1;
            case ModifiedCellularReturnType.Distance2Sub:
                return distance1 - distance0 - 1;
            case ModifiedCellularReturnType.Distance2Mul:
                return distance1 * distance0 * 0.5f - 1;
            case ModifiedCellularReturnType.Distance2Div:
                return distance0 / distance1 - 1;
            default:
                return 0;
        }
    }
}
