using UnityEngine;

public class RandomPlacementOnTerrain : MonoBehaviour
{
    public Transform playerTransform;
    public Transform targetTransform;
    public Terrain terrain;

    void Start()
    {
        PlaceObjectOnTerrain(playerTransform);
        PlaceObjectOnTerrain(targetTransform);
    }

    void PlaceObjectOnTerrain(Transform obj)
    {
        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;
        float terrainPosX = terrain.transform.position.x;
        float terrainPosZ = terrain.transform.position.z;

        float randomX = UnityEngine.Random.Range(terrainPosX, terrainPosX + terrainWidth);
        float randomZ = UnityEngine.Random.Range(terrainPosZ, terrainPosZ + terrainLength);

        float y = terrain.SampleHeight(new Vector3(randomX, 0, randomZ));
        y += 10;

        obj.position = new Vector3(randomX, y, randomZ);
        
    }
}
