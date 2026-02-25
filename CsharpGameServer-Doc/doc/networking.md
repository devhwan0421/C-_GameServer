# 네트워킹
- IO 스레드 개수 8개로 설정
- IO스레드는 CLR의 스레드풀을 활용
- 물리가 아닌 논리프로세스 개수 만큼 설정하는 이유는 레지스터가 논리프로세스 수 만큼 있기 때문에 컨텍스트 스위칭이 발생하지 않기 때문
- 캐시는 물리 개수 만큼 있지만 두 스레드가 같은 데이터를 읽고 있다면 성능상 이점이 생긴다(캐시히트라고 함. 반대의 경우는 캐시오염)
- 특정 논리 프로세서에 붙이는 방법은 ProcessThread.ProcessorAffinity 설정
- IOCP는 자체적으로 캐시히트율을 높이기 위해 가장 최근에 일을 마친 스레드를 먼저 깨우고 있음

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