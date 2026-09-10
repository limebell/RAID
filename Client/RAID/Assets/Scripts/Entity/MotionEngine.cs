using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Raid.Entity
{
    public class MotionEngine : MonoBehaviour
    {
        [Tooltip("렌더 시점 지연(초). 서버 틱(기본 50ms)의 약 2배 권장.")]
        [SerializeField] private float _interpolationDelay = 0.1f;

        [Tooltip("마지막 스냅샷과의 간격이 이보다 크면 현재 화면 포즈에서 버퍼를 다시 심습니다.")]
        [SerializeField] private float _rebaseGap = 0.2f;

        [Tooltip("이 거리 이하면 이동이 끊긴 것으로 보고, 현재 화면 위치에서 감속 정지합니다.")]
        [SerializeField] private float _stopDistance = 0.02f;

        [Tooltip("감속/스냅 보간의 최소 시간(초).")]
        [SerializeField] private float _minBlendDuration = 0.08f;

        [Tooltip("감속/스냅 보간의 최대 시간(초).")]
        [SerializeField] private float _maxBlendDuration = 0.22f;

        [SerializeField] private int _maxBufferedSnapshots = 32;

        private readonly ConcurrentQueue<PendingSnapshot> _pending = new();
        private readonly List<Snapshot> _buffer = new();

        private struct PendingSnapshot
        {
            public Vector2 Position;
            public Vector2 Direction;
            public bool Snap;
        }

        private struct Snapshot
        {
            public Vector2 Position;
            public Vector2 Direction;
            public float Time;
            public bool EaseOut;
        }

        public void PushSnapshot(Vector2 position, Vector2 direction)
        {
            _pending.Enqueue(new PendingSnapshot { Position = position, Direction = direction, Snap = false });
        }

        public void SnapTo(Vector2 position, Vector2 direction)
        {
            _pending.Enqueue(new PendingSnapshot { Position = position, Direction = direction, Snap = true });
        }

        private void Update()
        {
            DrainPending();
            ApplyInterpolation();
        }

        private void DrainPending()
        {
            while (_pending.TryDequeue(out var pending))
            {
                var now = Time.time;
                var renderTime = now - _interpolationDelay;

                if (pending.Snap)
                {
                    if (_buffer.Count == 0)
                    {
                        HoldPose(pending.Position, pending.Direction, renderTime);
                    }
                    else
                    {
                        BlendTo(pending.Position, pending.Direction, renderTime);
                    }

                    continue;
                }

                if (IsStoppedSnapshot(pending.Position, pending.Direction))
                {
                    BlendTo(pending.Position, pending.Direction, renderTime);
                    continue;
                }

                if (ShouldRebase(now))
                {
                    _buffer.Clear();
                    _buffer.Add(new Snapshot
                    {
                        Position = FromWorld(transform.position),
                        Direction = FromRotation(transform.rotation),
                        Time = renderTime
                    });
                }

                _buffer.Add(new Snapshot
                {
                    Position = pending.Position,
                    Direction = pending.Direction,
                    Time = now
                });
                TrimBuffer();
            }
        }

        private void TrimBuffer()
        {
            while (_buffer.Count > _maxBufferedSnapshots)
            {
                _buffer.RemoveAt(0);
            }
        }

        private bool ShouldRebase(float now)
        {
            if (_buffer.Count == 0)
            {
                return true;
            }

            return now - _buffer[_buffer.Count - 1].Time > _rebaseGap;
        }

        private void ApplyInterpolation()
        {
            if (_buffer.Count == 0)
            {
                return;
            }

            var renderTime = Time.time - _interpolationDelay;

            if (renderTime <= _buffer[0].Time)
            {
                ApplyPose(_buffer[0].Position, _buffer[0].Direction);
                return;
            }

            while (_buffer.Count >= 2 && _buffer[1].Time <= renderTime)
            {
                _buffer.RemoveAt(0);
            }

            if (_buffer.Count == 1)
            {
                ApplyPose(_buffer[0].Position, _buffer[0].Direction);
                return;
            }

            var from = _buffer[0];
            var to = _buffer[1];
            var t = Mathf.InverseLerp(from.Time, to.Time, renderTime);
            if (to.EaseOut)
            {
                t = 1f - (1f - t) * (1f - t);
            }

            ApplyPose(
                Vector2.LerpUnclamped(from.Position, to.Position, t),
                SlerpDirection(from.Direction, to.Direction, t));
        }

        private bool IsStoppedSnapshot(Vector2 position, Vector2 direction)
        {
            if (_buffer.Count == 0)
            {
                return false;
            }

            var last = _buffer[_buffer.Count - 1];
            return Vector2.Distance(last.Position, position) <= _stopDistance
                && Vector2.Distance(last.Direction, direction) <= _stopDistance;
        }

        private void BlendTo(Vector2 position, Vector2 direction, float renderTime)
        {
            var visualPosition = FromWorld(transform.position);
            var visualDirection = FromRotation(transform.rotation);
            var remaining = Vector2.Distance(visualPosition, position);
            var facingDelta = Vector2.Distance(
                visualDirection.sqrMagnitude > 0.0001f ? visualDirection.normalized : visualDirection,
                direction.sqrMagnitude > 0.0001f ? direction.normalized : direction);

            if (remaining <= 0.0001f && facingDelta <= 0.001f)
            {
                HoldPose(position, direction, renderTime);
                return;
            }

            var speed = EstimateSpeed();
            var duration = remaining > 0.0001f && speed > 0.01f
                ? remaining / speed * 2f
                : _minBlendDuration;
            duration = Mathf.Clamp(duration, _minBlendDuration, _maxBlendDuration);

            _buffer.Clear();
            _buffer.Add(new Snapshot
            {
                Position = visualPosition,
                Direction = visualDirection,
                Time = renderTime
            });
            _buffer.Add(new Snapshot
            {
                Position = position,
                Direction = direction,
                Time = renderTime + duration,
                EaseOut = true
            });
        }

        private void HoldPose(Vector2 position, Vector2 direction, float renderTime)
        {
            _buffer.Clear();
            ApplyPose(position, direction);
            _buffer.Add(new Snapshot
            {
                Position = position,
                Direction = direction,
                Time = renderTime
            });
        }

        private float EstimateSpeed()
        {
            if (_buffer.Count < 2)
            {
                if (_buffer.Count == 0 || _interpolationDelay <= 0.0001f)
                {
                    return 0f;
                }

                return Vector2.Distance(FromWorld(transform.position), _buffer[_buffer.Count - 1].Position)
                    / _interpolationDelay;
            }

            var from = _buffer[_buffer.Count - 2];
            var to = _buffer[_buffer.Count - 1];
            var deltaTime = to.Time - from.Time;
            if (deltaTime <= 0.0001f)
            {
                return 0f;
            }

            return Vector2.Distance(from.Position, to.Position) / deltaTime;
        }

        private void ApplyPose(Vector2 position, Vector2 direction)
        {
            transform.position = ToWorld(position);
            transform.rotation = ToRotation(direction);
        }

        private static Vector2 SlerpDirection(Vector2 from, Vector2 to, float t)
        {
            if (from.sqrMagnitude < 0.0001f)
            {
                return to;
            }

            if (to.sqrMagnitude < 0.0001f)
            {
                return from;
            }

            var rotation = Quaternion.Slerp(
                Quaternion.LookRotation(new Vector3(from.x, 0f, from.y)),
                Quaternion.LookRotation(new Vector3(to.x, 0f, to.y)),
                t);
            var forward = rotation * Vector3.forward;
            return new Vector2(forward.x, forward.z);
        }

        private Vector3 ToWorld(Vector2 position) =>
            new(position.x, 0, position.y);

        private Vector2 FromWorld(Vector3 position) =>
            new(position.x, position.z);

        private Quaternion ToRotation(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
        }

        private Vector2 FromRotation(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            return new Vector2(forward.x, forward.z);
        }
    }
}
