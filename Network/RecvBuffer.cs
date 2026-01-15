using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RecvBuffer
{
    private ArraySegment<byte> _buffer;
    private int _readPos; //읽기 커서 위치, 읽기 시작 위치
    private int _writePos; //쓰기 커서 위치, 쓰기 시작 위치

    public RecvBuffer(ArraySegment<byte> buffer)
    {
        _buffer = buffer;
    }

    //현재 버퍼에 쌓여있는 데이터 크기
    public int DataSize => _writePos - _readPos; // writepos:5, readpos:2 -> 5-2=3
    public int FreeSize => _buffer.Count - _writePos;

    //데이터의 시작 위치(읽기용)
    //매개변수 설명: (배열, 오프셋, 길이). _array는 버퍼 배열, _buffer.Offset + _readPos는 읽기 시작 위치, DataSize는 읽을 데이터 크기
    public ArraySegment<byte> ReadSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); 
    public ArraySegment<byte> WriteSegment => new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize);

    //성공적으로 처리했을 때 커서 이동
    public bool OnRead(int numOfBytes)
    {
        if (numOfBytes > DataSize) return false;
        _readPos += numOfBytes;
        return true;
    }

    //데이터 수신 후 커서 이동
    public bool OnWrite(int numOfBytes)
    {
        if(numOfBytes > FreeSize) return false;
        _writePos += numOfBytes;
        return true;
    }

    //커서를 앞으로 당겨서 공간 확보 (정리 작업)
    public void Clean()
    {
        int dataSize = DataSize;
        if(dataSize == 0)
        {
            _readPos = _writePos = 0;
        }
        else
        {
            //남은 데이터를 맨 앞으로 복사
            Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
            _readPos = 0;
            _writePos = dataSize;
        }
    }

    //ArrayPool 반납을 위해 원본 배열에 접근할 수 있게 함
    public byte[] UnderlyingArray => _buffer.Array;
}