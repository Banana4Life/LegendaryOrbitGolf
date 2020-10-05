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

            return _buffer[_head + index % Capacity];
        }
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
        var index = (_tail + 1) % Capacity;
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
        var index = (_head - 1 + Capacity) % Capacity;
        if (index == _tail)
        {
            throw new IndexOutOfRangeException("Head overtook Head!");
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
        _head = (_head + 1) % Capacity;
        _length--;
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
        _tail = (_tail - 1 + Capacity) % Capacity;
        _length--;
        return item;
    }
}