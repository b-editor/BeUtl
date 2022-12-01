﻿namespace Beutl.Media.Source;

internal sealed class Counter<T>
    where T : class, IDisposable
{
    private T? _value;
    private Action? _onRelease;
    private volatile int _refs;

    public Counter(T value, Action? onRelease)
    {
        _value = value;
        _onRelease = onRelease;
        _refs = 1;
    }

    public void AddRef()
    {
        var old = _refs;
        while (true)
        {
            if (old == 0)
            {
                throw new ObjectDisposedException("Cannot add a reference to a nonreferenced item");
            }
            var current = Interlocked.CompareExchange(ref _refs, old + 1, old);
            if (current == old)
            {
                break;
            }
            old = current;
        }
    }

    public void Release()
    {
        var old = _refs;
        while (true)
        {
            var current = Interlocked.CompareExchange(ref _refs, old - 1, old);

            if (current == old)
            {
                if (old == 1)
                {
                    _onRelease?.Invoke();
                    _onRelease = null;

                    _value?.Dispose();
                    _value = null;
                }
                break;
            }
            old = current;
        }
    }

    public int RefCount => _refs;
}
