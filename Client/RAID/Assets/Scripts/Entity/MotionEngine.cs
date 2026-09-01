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
                    _buffer.Clear();
                    ApplyPose(pending.Position, pending.Direction);
                    _buffer.Add(new Snapshot
                    {
                        Position = pending.Position,
                        Direction = pending.Direction,
                        Time = renderTime
                    });
                    TrimBuffer();
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
            ApplyPose(
                Vector2.LerpUnclamped(from.Position, to.Position, t),
                Vector2.LerpUnclamped(from.Direction, to.Direction, t));
        }

        private void ApplyPose(Vector2 position, Vector2 direction)
        {
            transform.position = ToWorld(position);
            transform.rotation = ToRotation(direction);
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
