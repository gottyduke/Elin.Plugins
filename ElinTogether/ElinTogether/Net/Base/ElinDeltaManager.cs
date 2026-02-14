using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ElinTogether.Models.ElinDelta;

namespace ElinTogether.Net;

public class ElinDeltaManager
{
    private const float Smoothing = 0.5f;

    /// <summary>
    ///     Coming in
    /// </summary>
    private readonly ConcurrentQueue<List<ElinDeltaBase>> _inBuffer = [];

    /// <summary>
    ///     Local deferred
    /// </summary>
    private readonly ConcurrentQueue<ElinDeltaBase> _inBufferDeferred = [];

    /// <summary>
    ///     Sending out
    /// </summary>
    private readonly ConcurrentQueue<ElinDeltaBase> _outBuffer = [];

    /// <summary>
    ///     Remote deferred
    /// </summary>
    private readonly ConcurrentQueue<ElinDeltaBase> _outBufferDeferred = [];

    private float _averageIn;

    // smoothed stat
    private float _averageOut;

    public bool HasPendingOut => !_outBuffer.IsEmpty || !_outBufferDeferred.IsEmpty;
    public bool HasPendingIn => !_inBuffer.IsEmpty || !_inBufferDeferred.IsEmpty;
    public bool IsIdle => !HasPendingOut && !HasPendingIn;

    /// <summary>
    ///     Sending out
    /// </summary>
    public void AddRemote(ElinDeltaBase delta)
    {
        _outBuffer.Enqueue(delta);
    }

    /// <summary>
    ///     Sending out, next flush
    /// </summary>
    public void DeferRemote(ElinDeltaBase delta)
    {
        _outBufferDeferred.Enqueue(delta);
    }

    public void AddLocalFront(ElinDeltaBase delta)
    {
        if (_inBuffer.FirstOrDefault() is List<ElinDeltaBase> batch) {
            batch.Add(delta);
        } else {
            AddLocalBatch([delta]);
        }
    }

    public void AddLocalBack(ElinDeltaBase delta)
    {
        if (_inBuffer.LastOrDefault() is List<ElinDeltaBase> batch) {
            batch.Add(delta);
        } else {
            AddLocalBatch([delta]);
        }
    }

    public void AddLocalBatch(List<ElinDeltaBase> batch)
    {
        _inBuffer.Enqueue(batch);
    }

    /// <summary>
    ///     Local defer, next flush
    /// </summary>
    public void DeferLocal(ElinDeltaBase delta)
    {
        _inBufferDeferred.Enqueue(delta);
    }

    public void ProcessLocalBatch(ElinNetBase net, int batchSize = -1)
    {
        // hurry up if we are too far behind
        var n = Math.Max((_inBuffer.Count + 3) / 4, 1);
        while (n-- > 0) {
            var batch = FlushInBuffer(batchSize);
            foreach (var delta in batch) {
                try {
                    if (delta is null) {
                        continue;
                    }

                    delta.Apply(net);
                } catch (Exception ex) {
                    EmpLog.Debug(ex, "Exception at processing delta {DeltaType}\n{@Delta}",
                        delta.GetType().Name, delta);
                    // noexcept
                }
            }
        }
    }

    public List<ElinDeltaBase> FlushOutBuffer(int batchSize = -1)
    {
        var batch = new List<ElinDeltaBase>();
        if (_outBuffer.IsEmpty) {
            return batch;
        }

        var count = 0;
        var max = batchSize > 0 ? batchSize : _outBuffer.Count;

        while (count < max && _outBuffer.TryDequeue(out var delta)) {
            batch.Add(delta);
            count++;
        }

        while (_outBufferDeferred.TryDequeue(out var deferred)) {
            _outBuffer.Enqueue(deferred);
        }

        return batch;
    }

    public List<ElinDeltaBase> FlushInBuffer(int batchSize = -1)
    {
        var batch = new List<ElinDeltaBase>();
        if (_inBuffer.IsEmpty) {
            return batch;
        }

        if (_inBuffer.TryDequeue(out var delta)) {
            batch = delta;
        }

        while (_inBufferDeferred.TryDequeue(out var deferred)) {
            AddLocalFront(deferred);
        }

        return batch;
    }

    public void ClearOut()
    {
        _outBuffer.Clear();
        _outBufferDeferred.Clear();
    }

    public void ClearIn()
    {
        _inBuffer.Clear();
        _inBufferDeferred.Clear();
    }

    public void UpdateAverages()
    {
        _averageOut = _averageOut * (1f - Smoothing) + _outBuffer.Count * Smoothing;
        _averageIn = _averageIn * (1f - Smoothing) + _inBuffer.Count * Smoothing;
    }

    public (int outBuffer, int outDeferred, int inBuffer, int inDeferred) GetCounts()
    {
        return (_outBuffer.Count, _outBufferDeferred.Count, _inBuffer.Count, _inBufferDeferred.Count);
    }

    public override string ToString()
    {
        UpdateAverages();
        return $"Delta Out={_averageOut:F1}\tDelta In={_averageIn:F1}";
    }
}