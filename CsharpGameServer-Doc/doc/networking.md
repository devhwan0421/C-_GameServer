# 네트워킹

- IO 스레드 개수 8개로 설정. IO스레드는 CLR의 스레드풀을 활용하였습니다. 시간 단축의 이유로
- 패킷 직렬화 및 프로토콜: 가변 길이 패킷 구조를 설계하고, 효율적인 메모리 사용을 위해 ArrayPool 및 Span<T>을 적용한 직렬화 로직을 구현했습니다.

## 메모리 단편화
- 85KB 이하는 SOH(Small Object Heap)에 할당됨
- GC가 압축을 할 때 Pinning(고정)된 객체 때문에 단편화가 발생
- Pinning은 IO작업 중일 때 발생
- 그래서 버퍼를 85KB 이상으로 할당하여 LOH(Large Object Heap)에 할당되도록 함
- LOH는 GC가 압축을 기본적으로 하지 않음 (2세대 GC에서는 압축)
- LOH의 단편화도 문제가 되지만 ArrayPool을 사용하여 단편화 문제를 완화했음

## SocketAsyncEventArgs
- 기존방식: BeginReceive를 호출할 때마다 내부적으로 IAsyncResult 객체가 생성되고 소멸됨. 매우 비효율적
- SocketAsyncEventArgs는 미리 만들어두고 재사용이 가능함. 현재 풀링방식으로 구현해놓지 않았음. 세션 내에서만 재사용 중