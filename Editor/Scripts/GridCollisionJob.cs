using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Flexus.ParticleMapEditor.Editor
{
    [BurstCompile]
    public struct GridCollisionJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<ParticleData> Particles;
        [ReadOnly] public NativeParallelMultiHashMap<int2, int> Grid;
        [ReadOnly] public NativeArray<LevelObjectData> LevelObjects;
        [ReadOnly] public float CellSize;

        [BurstCompile]
        public void Execute(int index)
        {
            var particle1 = Particles[index];
            var gridCell = GetGridCell(particle1.CurrentPosition, CellSize);
            // Iterate over the current cell and neighboring cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var neighborCell = new int2(gridCell.x + x, gridCell.y + y);
                    if (!Grid.TryGetFirstValue(neighborCell, out int neighborIndex, out var iterator)) continue;
                    do
                    {
                        if (neighborIndex == index) continue; // Skip self-collision

                        var particle2 = Particles[neighborIndex];

                        // Early exit if both particles are locked
                        if (particle1.IsLocked && particle2.IsLocked) continue;

                        // Collision detection and resolution
                        var collisionAxis = particle1.CurrentPosition - particle2.CurrentPosition;
                        float sqrDist = math.lengthsq(collisionAxis);
                        float minDist = particle1.Radius + particle2.Radius;
                        float sqrMinDist = minDist * minDist;

                        if (!(sqrDist <= sqrMinDist)) continue;

                        float dist = math.sqrt(sqrDist);
                        var n = collisionAxis / dist;
                        float delta = minDist - dist;

                        if (particle1.IsLocked == particle2.IsLocked)
                        {
                            particle1.CurrentPosition += 0.5f * delta * n;
                            particle2.CurrentPosition -= 0.5f * delta * n;
                        }
                        else if (particle1.IsLocked)
                        {
                            particle2.CurrentPosition -= delta * n;
                        }
                        else
                        {
                            particle1.CurrentPosition += delta * n;
                        }

                        // Update particle states
                        Particles[index] = particle1;
                        Particles[neighborIndex] = particle2;
                    } 
                    while (Grid.TryGetNextValue(out neighborIndex, ref iterator));
                }
            }
            
            // Handle collisions with level objects
            for (int i = 0; i < LevelObjects.Length; i++)
            {
                var levelObject = LevelObjects[i];

                var collisionAxis = particle1.CurrentPosition - levelObject.Position;
                float sqrDist = math.lengthsq(collisionAxis);
                float minDist = particle1.Radius + levelObject.Radius;
                float sqrMinDist = minDist * minDist;

                if (!(sqrDist <= sqrMinDist)) continue;
                
                float dist = math.sqrt(sqrDist);
                var n = collisionAxis / dist;
                float delta = minDist - dist;

                if (!particle1.IsLocked)
                {
                    particle1.CurrentPosition += delta * n;
                }
            }

            // Update particle
            Particles[index] = particle1;
        }

        //[BurstCompile]
        private static int2 GetGridCell(float2 position, float cellSize)
        {
            return new int2((int)math.floor(position.x / cellSize), (int)math.floor(position.y / cellSize));
        }
    }
}