using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise{

    public static float[,] GenerateNoiseMap(int mapWidth,int mapHight,int seed,float noiseScale,int octaves, float persistance, float lacunarity,Vector2 offset){
        float[,] noiseMap = new float[mapWidth,mapHight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++){
            float offsetX = prng.Next(-100000,100000) + offset.x;
            float offsetY = prng.Next(-100000,100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX,offsetY);
        }

        if(noiseScale <= 0){
            noiseScale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHight = mapHight / 2f;

        for(int x = 0; x < mapWidth; x++){
            for(int y = 0; y < mapHight; y++){
                
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int i = 0; i < octaves; i++){
                    float sampleX = (x-halfWidth + octaveOffsets[i].x) / noiseScale * frequency;//octaveOffsets[i].x放在分子实现区块平滑连接
                    float sampleY = (y-halfHight + octaveOffsets[i].y) / noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX,sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                    
                }
                if(noiseHeight > maxNoiseHeight){
                    maxNoiseHeight = noiseHeight;
                }else if(noiseHeight < minNoiseHeight){
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }

        }

        for(int x = 0; x < mapWidth; x++){
            for(int y = 0; y < mapHight; y++){
                noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight,maxNoiseHeight,noiseMap[x,y]);
            }
        }
        return noiseMap;
    }
}
