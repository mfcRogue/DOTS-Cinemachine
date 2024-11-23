using Networking.Common;
using UnityEngine;
using Unity.Entities;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Move Settings")]
    [SerializeField] private bool drawBounds;
    [SerializeField] private Bounds cameraBounds;
    [SerializeField] private float camSpeed;
    [SerializeField] private Vector2 screenPercentageDetection;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoomDistance;
    [SerializeField] private float maxZoomDistance;
    [SerializeField] private float zoomSpeed;

    [Header("Camera Start Positions")] 
    [SerializeField] private Vector3 redTeamPosition = new Vector3(50f, 0f, 50f);
    [SerializeField] private Vector3 blueTeamPosition = new Vector3(-50f, 0f, -50f);
    [SerializeField] private Vector3 spectatorPosition = new Vector3(0f, 0f, 0f);

    private Vector2 _normalScreenPercentage;
    private Vector2 NormalMousePos => new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
    private bool InScreenLeft => NormalMousePos.x < _normalScreenPercentage.x  && Application.isFocused;
    private bool InScreenRight => NormalMousePos.x > 1 - _normalScreenPercentage.x  && Application.isFocused;
    private bool InScreenTop => NormalMousePos.y < _normalScreenPercentage.y  && Application.isFocused;
    private bool InScreenBottom => NormalMousePos.y > 1 - _normalScreenPercentage.y  && Application.isFocused;

    private CinemachineFollow _follow;
    private EntityManager _entityManager;
    private EntityQuery _teamControllerQuery;
    private EntityQuery _localChampQuery;
    private bool _cameraSet;

    private void Awake()
    {
        _normalScreenPercentage = screenPercentageDetection * 0.01f;

        // Get the Transposer component
        _follow = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineFollow;
    }

    private void Start()
    {
        if (World.DefaultGameObjectInjectionWorld == null) return;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _teamControllerQuery = _entityManager.CreateEntityQuery(typeof(ClientTeamRequest));
        _localChampQuery = _entityManager.CreateEntityQuery(typeof(OwnerChampionTag));

        // Move the camera to the base corresponding to the team the player is on.
        // Spectators' cameras will start in the center of the map
        if (_teamControllerQuery.TryGetSingleton<ClientTeamRequest>(out var requestedTeam))
        {
            var team = requestedTeam.Value;
            var cameraPosition = team switch
            {
                TeamType.Blue => blueTeamPosition,
                TeamType.Red => redTeamPosition,
                _ => spectatorPosition
            };
            transform.position = cameraPosition;

            if (team != TeamType.AutoSelect)
            {
                _cameraSet = true;
            }
        }
    }

    private void OnValidate()
    {
        _normalScreenPercentage = screenPercentageDetection * 0.01f;
    }

    private void Update()
    {
        SetCameraForAutoAssignTeam();
        MoveCamera();
        ZoomCamera();
    }

    private void MoveCamera()
    {
        Vector3 direction = Vector3.zero;

        if (InScreenLeft)
            direction += Vector3.left;
        if (InScreenRight)
            direction += Vector3.right;
        if (InScreenTop)
            direction += Vector3.back;
        if (InScreenBottom)
            direction += Vector3.forward;

        transform.position += direction.normalized * (camSpeed * Time.deltaTime);

        if (!cameraBounds.Contains(transform.position))
        {
            transform.position = cameraBounds.ClosestPoint(transform.position);
        }
    }

    private void ZoomCamera()
    {
        if (Mathf.Abs(Input.mouseScrollDelta.y) > float.Epsilon)
        {
            var followOffset = _follow.FollowOffset;
            followOffset.y -= Input.mouseScrollDelta.y * zoomSpeed * Time.deltaTime;
            followOffset.y = Mathf.Clamp(followOffset.y, minZoomDistance, maxZoomDistance);
            _follow.FollowOffset = followOffset;
        }
    }

    private void SetCameraForAutoAssignTeam()
    {
        if (!_cameraSet)
        {
            if (_localChampQuery.TryGetSingletonEntity<OwnerChampionTag>(out var localChamp))
            {
                var team = _entityManager.GetComponentData<GameTeam>(localChamp).Value;
                var cameraPosition = team switch
                {
                    TeamType.Blue => blueTeamPosition,
                    TeamType.Red => redTeamPosition,
                    _ => spectatorPosition
                };
                transform.position = cameraPosition;
                _cameraSet = true;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawBounds) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
    }
}
