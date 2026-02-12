using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace _Source
{
    public class CircleMovementSystem : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int objectCount = 1000;
        [SerializeField] private float radius = 20f;
        [SerializeField] private float speed = 1f;
        
        [SerializeField] private float calculationInterval = 2f;
        
        private float _timer;
        private TransformAccessArray _transformArray;
        private NativeArray<float> _logResults; 
        private JobHandle _movementHandle;
        private JobHandle _calcHandle;

        struct MovementJob : IJobParallelForTransform
        {
            public float Time;
            public float Speed;
            public float Radius;

            public void Execute(int index, TransformAccess transform)
            {
                float offset = index * 0.1f;
                float angle = Time * Speed + offset;
                transform.position = new Vector3(math.cos(angle) * Radius, 0, math.sin(angle) * Radius);
            }
        }
        
        struct LogCalculationJob : IJob
        {
            public NativeArray<float> Results;
            public uint Seed; 

            public void Execute()
            {
                var random = new Unity.Mathematics.Random(Seed);

                for (int i = 0; i < Results.Length; i++)
                {
                    float randomVal = random.NextFloat(1f, 100f);
                    Results[i] = math.log10(randomVal);
                }
            }
        }

        void Start()
        {
            Transform[] transforms = new Transform[objectCount];
            for (int i = 0; i < objectCount; i++)
            {
                transforms[i] = Instantiate(prefab).transform;
            }

            _transformArray = new TransformAccessArray(transforms);
            _logResults = new NativeArray<float>(objectCount, Allocator.Persistent);
            _timer = calculationInterval;
        }

        void Update()
        {
            var moveJob = new MovementJob { Time = Time.time, Speed = speed, Radius = radius };
            _movementHandle = moveJob.Schedule(_transformArray);
            
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                var calcJob = new LogCalculationJob
                {
                    Results = _logResults,
                    Seed = (uint)(Time.time * 1000) + 1 
                };
                _calcHandle = calcJob.Schedule();
                _timer = calculationInterval;
            }
        }

        void LateUpdate()
        {
            _movementHandle.Complete();
            _calcHandle.Complete();
            if (_timer >= calculationInterval - Time.deltaTime)
            {
                Debug.Log($" Расчет логарифмов завершен. {_logResults[0]}");
            }
        }

        void OnDestroy()
        {
            if (_transformArray.isCreated)
            {
                _transformArray.Dispose();
            }

            if (_logResults.IsCreated)
            {
                _logResults.Dispose();
            }
        }
    }
}