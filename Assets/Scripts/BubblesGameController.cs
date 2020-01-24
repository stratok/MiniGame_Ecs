using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Minigames.Bubbles
{
	public class BubblesGameController : MonoBehaviour
    {

#pragma warning disable 649
        [Header("Fish settings")]
        [SerializeField]
        private Material _fishMaterial;
        [SerializeField]
        private Mesh _fishMesh;
        [SerializeField]
        private Transform _fishStartPoint;
        [Space(10)]
        [SerializeField]
        [Range(0.1f, 3)]
        private float _fishScale;
        [SerializeField]
        [Range(0, 10)]
        private float _maxXDeviation;
        [SerializeField]
        [Range(0, 10)]
        private float _fishSpeed;

        [Header("Bubble settings")]
        [SerializeField]
        private Material _bubbleMaterial;
        [SerializeField]
        private Mesh _bubbleMesh;
        [Space(10)]
        [SerializeField]
        [Range(0.1f, 1f)]
        private float _bubbleMinScale = 1f;
        [SerializeField]
        [Range(1f, 3f)]
        private float _bubbleMaxScale = 1.5f;
        [Space(10)]
        [SerializeField]
        [Range(0f, 1f)]
        private float _bubbleSpawnTime = 0.5f;
        [SerializeField]
        [Range(0.001f, 0.1f)]
        private float _spawnTimeDecreaseUnit = 0.002f;
        [Space(10)]
        [SerializeField]
        [Range(0, 10f)]
        private float _bubbleYRange = 1;
        [SerializeField]
        [Range(10, 90)]
        private int _bubbleMaxYRange = 80;
        [SerializeField]
        [Range(0.01f, 1f)]
        private float _bubbleRangeIncreaseUnit = 0.1f;
        [Space(10)]
        [SerializeField]
        [Range(0.5f, 10f)]
        private float _bubbleSpeed = 3f;
        [SerializeField]
        [Range(0.01f, 1f)]
        private float _bubbleSpeedIncreaseUnit = 0.05f;
        [Space(10)]
        [SerializeField]
        [Range(0.01f, 1f)]
        private float _bubbleTimeToDestroy = 0.1f;
        [SerializeField]
        [Range(0, 10)]
        private float _bubbleSpawnShift = 1;

        [Header("Game settings")]
        [SerializeField]
        [Range(1, 10)]
        private int _livesCount = 5;
        [SerializeField]
        [Range(1, 100)]
        private int _scoreForSuccess = 1;

        [Header("Canvas settings")]
        [SerializeField]
        private Text _streakText;
        [SerializeField]
        private Text _scoreText;
        [SerializeField]
        private Text _livesText;
        [SerializeField]
        private Text _finalText;
        [SerializeField]
        private GameObject _buttonStart;
        [SerializeField]
        private GameObject _buttonExit;

        private float3 _bubbleDestinationPoint;
        private float3 _bubbleStartPosition;
        private float3 _fishStartPosition;

        private bool _isGameRunning;
        private bool _isShouldRestart;

        private int _streakCount = 0;
        private int _scoreCount = 0;
        private int _startLivesCount;

        private float _startBubbleSpawnTime;
        private float _startBubbleYDeviation;
        private float _startBubbleSpeed;
        private float _spawnTargetTimer;
        private float _maxYBubbleDeviation;

        private static EntityManager _entityManager;
        private Camera _camera;
#pragma warning restore 649

        protected void Awake()
        {
            _camera = Camera.main;

            var screenBottomEdgePos = _camera.ViewportToWorldPoint(new Vector3(0, 0, 10));
            var screenCenterEdgePos = _camera.ViewportToWorldPoint(new Vector3(0, 0.5f, 10));
            _maxYBubbleDeviation = (screenCenterEdgePos - screenBottomEdgePos).magnitude * _bubbleMaxYRange * 0.01f;

            _bubbleDestinationPoint = screenCenterEdgePos;
            _bubbleStartPosition = _camera.ViewportToWorldPoint(new Vector3(1, 0.5f, 10));

            _bubbleStartPosition.x += _bubbleSpawnShift;
            _fishStartPosition = _fishStartPoint.position;
            _startBubbleSpawnTime = _bubbleSpawnTime;
            _startBubbleYDeviation = _bubbleYRange;
            _startBubbleSpeed = _bubbleSpeed;
            _startLivesCount = _livesCount;
            _finalText.gameObject.SetActive(false);
            _entityManager = World.Active.EntityManager;
            _buttonExit.SetActive(false);

            BubbleDestroySystem.FishDestroyBubble += FishIncreaseStreack;
            BubbleDestroySystem.ScreenDestroyBubble += ResetStreak;
            BubbleDestroySystem.TapDestroyBubble += TapIncreaseStreack;
        }



        public void StartGame()
        {
            _finalText.gameObject.SetActive(false);
            _buttonStart.SetActive(false);

            _bubbleSpawnTime = _startBubbleSpawnTime;
            _bubbleYRange = _startBubbleYDeviation;
            _bubbleSpeed = _startBubbleSpeed;
            _livesCount = _startLivesCount;
            _streakCount = 0;
            _scoreCount = 0;
            UpdateDashboard();
            SpawnFishEntity();

            _isGameRunning = true;
        }

        public void RestartGame()
        {
            _streakCount = 0;
            _livesCount -= 1;
            UpdateDashboard();

            _bubbleSpawnTime = _startBubbleSpawnTime;
            _bubbleYRange = _startBubbleYDeviation;
            _bubbleSpeed = _startBubbleSpeed;
            _isShouldRestart = true;
        }

        public void StopGame()
        {
            _livesCount = 0;
            _isGameRunning = false;

            UpdateDashboard();

            _buttonExit.SetActive(true);
            _finalText.text = $"Игра окончена!\n вы заработали {_scoreCount} очков!";
            _finalText.gameObject.SetActive(true);
        }

        protected void OnDestroy()
        {
            BubbleDestroySystem.FishDestroyBubble -= FishIncreaseStreack;
            BubbleDestroySystem.ScreenDestroyBubble -= ResetStreak;
            BubbleDestroySystem.TapDestroyBubble -= TapIncreaseStreack;
        }

        private void FishIncreaseStreack()
        {
            _streakCount ++;
            _scoreCount += _streakCount * _scoreForSuccess;

            UpdateDashboard();
            UpdateBubbleSettings();
        }

        private void ResetStreak()
        {
            if (_livesCount > 1)
                RestartGame();
            else
                StopGame();
        }

        private void TapIncreaseStreack()
        {
            _streakCount ++;
            _scoreCount += _streakCount * _scoreForSuccess;

            UpdateDashboard();
            UpdateBubbleSettings();
        }

        private void UpdateDashboard()
        {
            _streakText.text = _streakCount.ToString();
            _scoreText.text = _scoreCount.ToString();
            _livesText.text = _livesCount.ToString();
        }

        private void UpdateBubbleSettings()
        {
            _bubbleSpawnTime -= _spawnTimeDecreaseUnit;
            _bubbleSpeed += _bubbleSpeedIncreaseUnit;
            _bubbleYRange = Mathf.Clamp(_bubbleYRange + _bubbleRangeIncreaseUnit, 0, _maxYBubbleDeviation);
        }

        private void Update()
        {
            if (_isGameRunning)
            {
                if (_isShouldRestart)
                {
                    _entityManager.CreateEntity(typeof(StopGame));
                    _isShouldRestart = false;
                }
                else
                {
                    // Bubble Spawner
                    _spawnTargetTimer -= Time.deltaTime;
                    if (_spawnTargetTimer < 0)
                    {
                        _spawnTargetTimer = _bubbleSpawnTime;
                        SpawnBubbleEntity();
                    }

                    // Mouse Input Handler
                    if (Input.GetMouseButtonDown(0))
                    {
                        float3 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        tapPos.z = 0;
                        SpawnTouchEntity(tapPos);
                    }
                }
            }
            else
            {
                _entityManager.DestroyEntity(_entityManager.GetAllEntities());
            }
        }

        private void SpawnTouchEntity(float3 position)
        {
            Entity entity = _entityManager.CreateEntity(
                typeof(Translation),
                typeof(LocalToWorld),
                typeof(ScreenTap)
            );

            _entityManager.SetComponentData(entity, new Translation { Value = position });
        }

        private void SpawnFishEntity()
        {
            Entity entity = _entityManager.CreateEntity(
                typeof(Translation),
                typeof(LocalToWorld),
                typeof(RenderMesh),
                typeof(Rotation),
                typeof(Scale),
                typeof(FishComponent)
            );

            SetEntityComponentData(entity, _fishStartPosition, _fishMesh, _fishMaterial, _fishScale);

            _entityManager.SetComponentData(entity, new FishComponent
            {
                MaxXDeviation = _maxXDeviation,
                Speed = _fishSpeed
            });
        }

        private void SpawnBubbleEntity()
        {
            Entity entity = _entityManager.CreateEntity(
                typeof(Translation),
                typeof(LocalToWorld),
                typeof(RenderMesh),
                typeof(Rotation),
                typeof(Scale),
                typeof(BubbleComponent)
            );

            SetEntityComponentData(entity, _bubbleStartPosition, _bubbleMesh, _bubbleMaterial, Random.Range(_bubbleMinScale, _bubbleMaxScale));

            _entityManager.SetComponentData(entity, new BubbleComponent
            {
                DestinationPoint = new float3(_bubbleDestinationPoint.x,
                                                Random.Range(-_bubbleYRange, _bubbleYRange),
                                                _bubbleDestinationPoint.z),
                Entity = entity,
                TimeToDestroy = _bubbleTimeToDestroy,
                Speed = _bubbleSpeed
            });
        }

        private void SetEntityComponentData(Entity entity, float3 spawnPosition, Mesh mesh, Material material, float scale)
        {
            _entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                material = material,
                mesh = mesh,
            });
            _entityManager.SetComponentData(entity, new Translation
            {
                Value = spawnPosition
            });
            _entityManager.SetComponentData(entity, new Scale
            {
                Value = scale
            });
        }
    }
}