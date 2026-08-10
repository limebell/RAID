using System.Threading.Tasks;
using Raid.Contracts.Battle.Events;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientController : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _floor;

    private HubClient _client;

    private Vector2 _targetPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async Task Start()
    {
        _client = new HubClient("http://localhost:5182");
        _client.Initialize();
        await _client.ConnectAsync();
        _client.OnBattleEvent += HandleBattleEvent;
    }

    // Update is called once per frame
    void Update()
    {
        _player.transform.position = new Vector3(_targetPosition.x, 1, _targetPosition.y);
        _camera.transform.position = _player.transform.position + new Vector3(0, 10, -6);
        _camera.transform.LookAt(_player.transform);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryMoveToMousePosition();
        }
    }

    private void TryMoveToMousePosition()
    {
        if (_client == null || !_client.IsConnected)
        {
            return;
        }

        var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit))
        {
            return;
        }

        if (hit.collider.gameObject != _floor &&
            !hit.collider.transform.IsChildOf(_floor.transform))
        {
            return;
        }

        // 바닥(XZ) 기준 2D 좌표
        _client.Move(new Vector2(hit.point.x, hit.point.z));
    }

    async Task OnDestroy()
    {
        if (_client != null)
        {
            await _client.DisconnectAsync();
            await _client.DisposeAsync();
        }
    }

    private void HandleBattleEvent(object sender, BattleEventDto e)
    {
        switch (e.Type)
        {
            case BattleEventType.EntityMoved:
                _targetPosition = new Vector2(e.Position.X, e.Position.Y);
                break;
            case BattleEventType.EntityMoveCompleted:
                _targetPosition = new Vector2(e.Position.X, e.Position.Y);
                break;
            case BattleEventType.PositionSet:
                _targetPosition = new Vector2(e.Position.X, e.Position.Y);
                break;
            case BattleEventType.ActionStarted:
                break;
            case BattleEventType.ActionPhaseChanged:
                break;
            case BattleEventType.ActionEnded:
                break;
            case BattleEventType.DamageApplied:
                break;
            case BattleEventType.Unknown:
                break;
        }
    }
}
