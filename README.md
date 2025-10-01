# Unity ECS vs Unity OOS in VR

### There are three tests:
1. Zombie walking test: how many animated skinned meshes can each stack run?
2. Boid simulation: how well can each stack reference its neighbors and simulate simple objects?
3. Simple object simulation: how well can each stack render simple objects?

### Important Notes:
1. In each of these tests, I increased the number of objects/entities until the fps fell below 72 fps.
2. Note that we're using a free ECS animation asset from the asset store. ECS may be even more performant if we used a better ECS animator.
3. There are 2 models used in these tests. The first is a zombie model I took from the asset store. It has 3.3 k triangles. The other is a simple pyramid I made in blender. It has 6 triangles. The Quest 2 and Quest 3 both performed *WAY* better with the lower poly model. I think it might be worth it to make a *much* lower poly model for the zombie and run these tests again. The headsets' hardware seem to max out at a certain triangle count.
4. In each of these tests, the GPU would max out before the CPU would. The ECS tests has less of a CPU load than the OOS.

### Results on Quest 2:
- Test 1: ECS & OOS were equal at about 250 skinned meshes. I believe that this is because of the GPU limitations of the hardware. They simply can't render many triangles. *These skinned meshes were higher poly than any of the other objects we rendered. ECS outperformed OOS in this test by 2x on the Quest 3S and 14x in the editor.*
  - Q2 ECS: 250 entities
  - Q2 OOS: 250 entities
  - Q3S ECS: 750 entities
  - Q3S OOS: 300 entities
- Test 2: ECS outperformed OOS by 15-30x.
  - Q2 ECS: 15k entities
  - Q2 OOS: 950 entities
  - Q3S ECS: 30k entities
  - Q3S OOS: 1k entities
- Test 3: ECS outperformed OOS by 5-6x.
  - Q2 ECS: 20k entities
  - Q2 OOS: 3k entities
  - Q3S ECS: 30k entities
  - Q3S OOS: 6.5k entities

## Key Takeaways:
- Triangle count is **absolutely** something that will max out the GPU. I think this is why the ECS and OOS performed the same on Q2 in test 1.
- **ECS did 5-30x better than OOS in both GPU & CPU utilization.**
- GPU utilization is the bottleneck on the quests. We already knew this.
