

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

public class Solver : MonoBehaviour
{
    public Transform starter;
    public Transform playerTransform;
    public Transform targetTransform;
    public float initialSearchRadius = 10f;
    public int numPoints = 10000;
    public float radiusIncrement = 10f;
    public float maxRadius = 3000f;

    public LayerMask obstacleLayer; 
    public LayerMask terrainLayer;

    private NativeArray<float3> randomPoints;
    private List<(Vector3 start, Vector3 direction, bool hitTarget)> rays = new List<(Vector3 start, Vector3 direction, bool hitTarget)>();
    private Vector3? validPosition = null; //Store the first valid position
    private Vector3 initialPlayerPosition; //Store the initial player position

    private int brojac;

    void Start()
    {
        initialPlayerPosition = playerTransform.position;

        // First check if the current position can see the target
        if (CanTargetSeePlayer(targetTransform.position, playerTransform.position))
        {
            Debug.Log("Target can see the player from the initial position.");
            brojac+=1;
        }
        else
        {
            Debug.Log("Target cannot see the player from the initial position, searching for a visible point.");
            SearchForVisiblePoint();
        }

        // Move the player to the valid position if found
        if (!validPosition.HasValue)
        {
            Debug.Log("No valid position found within the maximum radius.");
        }
        else
        {
            playerTransform.position = validPosition.Value;
            Debug.Log("Player moved to the new position: " + validPosition.Value);
            Debug.Log("Distance between start and end: " + Vector3.Distance(initialPlayerPosition, validPosition.Value));
        }

        initialPlayerPosition += new Vector3(0f, 10f, 0f);
        starter.position = initialPlayerPosition;
         Debug.Log("Ukupan broj raycasteva: " + brojac);
    }

    void SearchForVisiblePoint()
    {
        float currentRadius = initialSearchRadius;
        float previousRadius = 0f;

        while (currentRadius <= maxRadius)
        {
            randomPoints = new NativeArray<float3>(numPoints, Allocator.TempJob);
            var job = new GenerateRandomPointsJob
            {
                playerPosition = (float3)playerTransform.position,
                innerRadius = previousRadius,
                outerRadius = currentRadius,
                numPoints = numPoints,
                randomPoints = randomPoints,
                randomSeed = (uint)UnityEngine.Random.Range(1, 100000)
            };
            JobHandle handle = job.Schedule();
            handle.Complete();
            if (ProcessRandomPoints())
            {
                Debug.Log("Target found a clear line of sight to the player.");
                randomPoints.Dispose(); 
                break;
            }
            randomPoints.Dispose();
            previousRadius = currentRadius;
            currentRadius += radiusIncrement;
        }
        if (currentRadius > maxRadius)
        {
            Debug.Log("Target could not find a clear line of sight to the player within the maximum radius.");
        }
    }

    bool ProcessRandomPoints()
    {
        Vector3 targetPosition = targetTransform.position;
        float closestDistance = Mathf.Infinity;

        // Iterate through generated points
        for (int i = 0; i < randomPoints.Length; i++)
        {
            Vector3 randomPoint = (Vector3)randomPoints[i];

            // Project the point onto the terrain
            if (ProjectPointOntoTerrain(ref randomPoint))
            {
                // Perform a raycast from the target to the random point around the player
                if (CanTargetSeePlayer(targetPosition, randomPoint))
                {
                    float distance = Vector3.Distance(initialPlayerPosition, randomPoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        validPosition = randomPoint + new Vector3(0, playerTransform.localScale.y, 0); // Store the closest valid position
                    }
                }
            }
        }

        return validPosition.HasValue; // Returns true if at least one visible point was found
    }

    bool ProjectPointOntoTerrain(ref Vector3 point)
    {
        Ray ray = new Ray(new Vector3(point.x, 1000f, point.z), Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, terrainLayer))
        {
            point = hit.point;
            return true;
        }
        else { return false; }
    }

    bool CanTargetSeePlayer(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 direction = toPosition - fromPosition;
        RaycastHit hitInfo;
        bool hitObstacle = false;

        //Perform the raycast
        if (Physics.Raycast(fromPosition, direction, out hitInfo, Vector3.Distance(fromPosition, toPosition), obstacleLayer))
        {
            brojac++;
            // Check if the raycast hit something before reaching the player
            if (hitInfo.collider != null && hitInfo.collider != playerTransform.GetComponent<Collider>())
            {
                hitObstacle = true;
            }
        }

        //Store the ray for debugging and drawing
        rays.Add((fromPosition, direction, !hitObstacle));

        return !hitObstacle;
    }

    void OnDrawGizmos()
    {
        if (rays == null) return;

        foreach (var ray in rays)
        {
            Gizmos.color = ray.hitTarget ? Color.green : Color.red;
            Gizmos.DrawRay(ray.start, ray.direction);
        }
    }

    [BurstCompile]
    struct GenerateRandomPointsJob : IJob
    {
        public float3 playerPosition;
        public float innerRadius;
        public float outerRadius;
        public int numPoints;
        public NativeArray<float3> randomPoints;
        public uint randomSeed;

        public void Execute()
        {
            var random = new Unity.Mathematics.Random(randomSeed);

            for (int i = 0; i < numPoints; i++)
            {
                // Generate points only within the ring (donut) area between innerRadius and outerRadius
                float distance = random.NextFloat(innerRadius, outerRadius);
                float3 randomDirection = random.NextFloat3Direction();
                randomDirection.y = 0;
                randomDirection = math.normalize(randomDirection);

                float3 randomPoint = playerPosition + randomDirection * distance;
                randomPoints[i] = randomPoint;
            }
        }
    }
}
