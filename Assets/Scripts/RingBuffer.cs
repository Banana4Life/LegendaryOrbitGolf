using System;

public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _length;
    
    public T Head => _buffer[_head];
    public T Tail => _buffer[_tail];
    public int Capacity => _buffer.Length;
    public int Length => _length;

    public T this[int index]
    {
        get
        {
            if (index >= _length)
            {
                throw new IndexOutOfRangeException();
            }

            return _buffer[ToRingIndex(_head + index)];
        }
    }

    private int ToRingIndex(int index)
    {
        return (index + Capacity) % Capacity;
    }

    public RingBuffer(int capacity)
    {
        _buffer = new T[capacity];
        _head = -1;
        _tail = -1;
        _length = 0;
    }

    public void Append(T item)
    {
        var index = ToRingIndex(_tail + 1);
        if (index == _head)
        {
            throw new IndexOutOfRangeException("Tail overtook Head!");
        }

        _buffer[index] = item;
        _tail = index;
        if (_head == -1)
        {
            _head = 0;
        }
        _length++;
    }

    public void Prepend(T item)
    {
        var index = _head == -1 ? 0 : ToRingIndex(_head - 1);
        if (index == _tail)
        {
            throw new IndexOutOfRangeException("Head overtook Tail!");
        }

        _buffer[index] = item;
        _head = index;
        if (_tail == -1)
        {
            _tail = 0;
        }
        _length++;
    }

    public T PopHead()
    {
        if (_length == 0)
        {
            throw new IndexOutOfRangeException("Buffer is empty!");
        }

        T item = Head;
        _buffer[_head] = default;
        _head = ToRingIndex(_head + 1);
        _length--;
        if (_length == 0)
        {
            _head = -1;
            _tail = -1;
        }
        
        return item;
    }

    public T PopTail()
    {
        if (_length == 0)
        {
            throw new IndexOutOfRangeException("Buffer is empty!");
        }

        T item = Tail;
        _buffer[_tail] = default;
        _tail = ToRingIndex(_tail - 1);
        _length--;
        if (_length == 0)
        {
            _head = -1;
            _tail = -1;
        }
        
        return item;
    }
}