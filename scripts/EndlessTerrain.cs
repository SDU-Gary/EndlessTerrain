using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndlessTerrain : MonoBehaviour
{
    
    const float viewerMoveThresholdForChunkUpdate = 25f; //更新地形块的阈值。如果观察者移动的距离超过这个阈值，就会更新地形块
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDst;//最大可见距离

    public Transform viewer;//观察者主体
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;//与上一帧的观察者位置比较，判断是否需要更新地形块

    int chunkSize;//区块大小
    int chunksVisibleInViewDst;//可见范围内的地形块数量

    static MapGenerator mapGenerator;//地图生成器


    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();//存储地形块对象,以坐标为键值
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start(){
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
        
    }

    void Update(){//每帧更新一次
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if((viewerPositionOld-viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate){//只更新在视野范围内的chunk
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    

    void UpdateVisibleChunks(){
        for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++){
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++){
            for(int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++){
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord)){
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if(terrainChunkDictionary[viewedChunkCoord].IsVisible()){
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }else{
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    


    class TerrainChunk{
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;

        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels,Transform parent,Material material){
            this.detailLevels = detailLevels;

            Vector2 position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);


            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < detailLevels.Length; i++){
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position ,OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData){
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        /*void OnMeshDataReceived(MeshData meshData){
            meshFilter.mesh = meshData.CreateMesh();
        }*/       
        
        public void UpdateTerrainChunk(){
            if(mapDataReceived){
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(EndlessTerrain.viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if(visible){
                    int lodIndex = 0;
                    for(int i = 0; i < detailLevels.Length - 1; i++){
                        if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold){
                            lodIndex = i + 1;
                        }else{
                            break;
                        }
                    }
                    if(lodIndex != previousLODIndex){
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if(lodMesh.hasMesh){
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }else if(!lodMesh.hasRequestedMesh){
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible){
            meshObject.SetActive(visible);
            
        }
        public bool IsVisible(){
            return meshObject.activeSelf;
        }
    }

    class LODMesh{
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod,System.Action updateCallback){
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData){
            mesh = meshData.CreateMesh();
            hasMesh = true;
        }
        public void RequestMesh(MapData mapData){
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo{
        public int lod;
        public float visibleDstThreshold;

        public LODInfo(int lod, float visibleDstThreshold){
            this.lod = lod;
            this.visibleDstThreshold = visibleDstThreshold;
        }
    }
}

       

