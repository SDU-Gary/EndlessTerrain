using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


public class MapGenerator : MonoBehaviour
{

    public enum DrawMode
    {
        NoiseMap,
        ColorMap,

        MeshMap
    }
    public DrawMode drawMode;

    public const int mapChunkSize = 241;

    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;
    [Range(1, 14)]
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    
    public float meshHeightMultiplier;

    public AnimationCurve meshHeightCurve;
    
    public bool autoUpdate;

    public TerrainType[] regions;
    Queue< MapThreadInfo<MapData> > mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue< MapThreadInfo<MeshData> > meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor(){

        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }else if (drawMode == DrawMode.MeshMap)
        {
            display.DrawMesh(MeshGenerator.GeneratorTerrainMesh(mapData.heightMap,meshHeightMultiplier,meshHeightCurve,editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

     MapData GenerateMapData(Vector2 centre)//绘制地图
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize,mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre+offset);//生成高度图,centre+offset使每个区块都不一样
        
        Color[] colorMap = new Color [mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)//渲染高度
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap,colorMap);
        
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback){ //start of the thread of mapData
        ThreadStart threadStart = delegate{
            MapDataThread(centre,callback);
        };

        new Thread(threadStart).Start();
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback){ //start of the thread of meshData
        ThreadStart threadStart = delegate{
            MeshDataThread(mapData,lod,callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback){ //get mapData & add mapData+callback to queue
        MapData mapData = GenerateMapData(centre);
        
        lock(mapDataThreadInfoQueue){
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback,mapData));
        }
        
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback){ //get mapData & add mapData+callback to queue
        MeshData meshData = MeshGenerator.GeneratorTerrainMesh(mapData.heightMap,meshHeightMultiplier,meshHeightCurve,lod);
        
        lock(meshDataThreadInfoQueue){
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback,meshData));
        }
        
    }

    void Update(){
        if(mapDataThreadInfoQueue.Count > 0){
            for(int i = 0 ; i < mapDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0){
            for(int i = 0 ; i < meshDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    void OnValidate()
    {
    
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }

    struct MapThreadInfo<T>{
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter){
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}



    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }

    public struct MapData{
        public readonly float[,] heightMap;
        public readonly Color[] colorMap;

        public MapData(float[,] heightMap, Color[] colorMap){
            this.heightMap = heightMap;
            this.colorMap = colorMap;
        }
    }
