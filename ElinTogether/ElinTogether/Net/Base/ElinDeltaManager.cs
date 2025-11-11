using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ElinTogether.Models.ElinDelta;

namespace ElinTogether.Net;

public class ElinDeltaManager
{
    /// <summary>
    ///     Coming in
    /// </summary>
    private readonly ConcurrentQueue<IElinDelta> _inBuffer = [];

    /// <summary>
    ///     Local deferred
    /// </summary>
    private readonly ConcurrentQueue<IElinDelta> _inBufferDeferred = [];

    /// <summary>
    ///     Sending out
    /// </summary>
    private readonly ConcurrentQueue<IElinDelta> _outBuffer = [];

    /// <summary>
    ///     Remote deferred
    /// </summary>
    private readonly ConcurrentQueue<IElinDelta> _outBufferDeferred = [];

    public bool HasPendingOut => !_outBuffer.IsEmpty || !_outBufferDeferred.IsEmpty;
    public bool HasPendingIn => !_inBuffer.IsEmpty || !_inBufferDeferred.IsEmpty;
    public bool IsIdle => !HasPendingOut && !HasPendingIn;

    /// <summary>
    ///     Sending out
    /// </summary>
    public void AddRemote(IElinDelta delta)
    {
        _outBuffer.Enqueue(delta);
    }

    /// <summary>
    ///     Sending out, next flush
    /// </summary>
    public void DeferRemote(IElinDelta delta)
    {
        _outBufferDeferred.Enqueue(delta);
    }

    /// <summary>
    ///     Coming in to process
    /// </summary>
    public void AddLocal(IElinDelta delta)
    {
        _inBuffer.Enqueue(delta);
    }

    /// <summary>
    ///     Local defer, next flush
    /// </summary>
    public void DeferLocal(IElinDelta delta)
    {
        _inBufferDeferred.Enqueue(delta);
    }

    public void ProcessLocalBatch(ElinNetBase net, int batchSize = -1)
    {
        var batch = FlushInBuffer(batchSize);
        foreach (var delta in batch) {
            try {
                delta.Apply(net);
            } catch (Exception ex) {
                EmpLog.Debug(ex, "Exception at processing delta {DeltaType}",
                    delta.GetType().Name);
                // noexcept
            }
        }
    }

    public List<IElinDelta> FlushOutBuffer(int batchSize = -1)
    {
        var batch = new List<IElinDelta>();
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

    public List<IElinDelta> FlushInBuffer(int batchSize = -1)
    {
        var batch = new List<IElinDelta>();
        if (_inBuffer.IsEmpty) {
            return batch;
        }

        var count = 0;
        var max = batchSize > 0 ? batchSize : _inBuffer.Count;

        while (count < max && _inBuffer.TryDequeue(out var delta)) {
            batch.Add(delta);
            count++;
        }

        while (_inBufferDeferred.TryDequeue(out var deferred)) {
            _inBuffer.Enqueue(deferred);
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

    public (int outBuffer, int outDeferred, int inBuffer, int inDeferred) GetCounts()
    {
        return (_outBuffer.Count, _outBufferDeferred.Count, _inBuffer.Count, _inBufferDeferred.Count);
    }
}