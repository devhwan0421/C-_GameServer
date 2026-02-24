# Silvervine Server Engine 2 설계 리뷰를 바탕으로 재구성한 C# IOCP & Stackless Fiber 게임서버
 [실버바인 서버 엔진 2 설계 리뷰 (NDC2018)](http://ndcreplay.nexon.com/NDC2018/sessions/NDC2018_0075.html)

## 1. 프로젝트 개요
- 프로젝트 명: C# GameServer (Silvervine Engine 2 Reference)
- 설명: 마비노기 모바일에서 사용된 실버바인 서버 엔진의 구조를 학습하고 IOCP 기반의 비동기 소켓 통신과 싱글 스레드 게임 로직 처리를 C#으로 구현한 프로젝트입니다.

## 2. 핵심 기술 스택
- 언어: C#
- 네트워크: IOCP
- 데이터베이스: MySQL, Dapper(ORM)
- 로깅: Serilog(Seq 연동)

## 3. 주요 기능 구현 목록
### 🚀실버바인 서버엔진2 아키텍처 따라하기
- **Stackless Fiber 기반 로직 처리**
	- 실버바인의 Stackful Fiber 구조를 분석하여 C#의 `async/await`와 Task를 활용하여 `Stackless` 방식으로 구현하였습니다.
	- 비동기 IO 대기 구간이 포함된 로직을 동기식 순차 코드처럼 작성할 수 있게 하여 생산성을 높이는 구조로 설계하였습니다.
- **Single-Thread Game Loop**
	- 모든 게임 로직을 `싱글 스레드` 루프에서 순차적으로 처리하여 `데이터 일관성`을 보장하였습니다.
	- 멀티스레드 환경의 `경쟁상태`와 `데드락` 문제를 근본적으로 제거하여 로직 설계 난이도를 낮췄습니다.
- **Job Queue 시스템**
	- `네트워크 IO` 와 `DB` 작업을 `게임 로직 스레드`와 분리하여 병목 현상 없는 구조로 설계하였습니다.
	- **성능 증명:** 500명 동시 접속자가 초당 200개의 이동패킷을 전송하는 환경(초당 10만 패킷)에서 평균 8ms의 안정적인 루프(몬스터, 플레이어 브로드 캐스트 로직도 처리 중인 상태) 처리 성능을 확인하였습니다.

스택풀과 스택리스의 차이와 문제점
스택리스가 생성비용은 낮다고 한다. 하지만 GC가 발생하면 비용이 증가. 해결법 상태머신풀링 또는 ValueTask


// 이거 마저 다듬기
🌐 네트워크 및 세션 관리
IO 스레드 개수 8개로 설정. IO스레드는 CRL의 스레드풀을 활용하였습니다. 시간 단축의 이유로
IOCP 기반 비동기 네트워크: 고성능 서버 구현을 위해 SocketAsyncEventArgs를 활용한 IOCP 통신 모델을 구축했습니다.

패킷 직렬화 및 프로토콜: 가변 길이 패킷 구조를 설계하고, 효율적인 메모리 사용을 위해 ArrayPool 및 Span<T>을 적용한 직렬화 로직을 구현했습니다.

🎮 MMORPG 게임 콘텐츠 시스템
컴포넌트 기반 데이터 관리: 실버바인 엔진의 유연한 객체 구조를 모작하여, 플레이어/몬스터 기능을 컴포넌트 단위로 분리하고 동적으로 확장 가능하게 설계했습니다.

AI 및 월드 관리: FSM(상태 머신) 기반의 몬스터 AI와 인스턴스 던전/필드 맵 관리 시스템을 구현했습니다.

콘텐츠 루프: 로그인부터 캐릭터 생성, 이동, 퀘스트 수락 및 전투, 보상 획득까지의 전체 게임 흐름을 완성했습니다.

💾 데이터 지속성 (Persistence)
비동기 DB 워커: Dapper ORM과 별도의 DB 전용 스레드를 운영하여, 게임 로직의 중단(Blocking) 없이 안전하게 데이터를 저장하고 불러옵니다.

Data-Driven 리소스 관리: 모든 게임 밸런스 데이터(아이템, 스탯, 퀘스트)를 외부 JSON으로 분리하여 유지보수성을 극대화했습니다.










🌐 네트워크 및 세션 관리
- Network: IOCP 기반 비동기 소켓 통신, Packet Header/Body 구조, 세션 관리
- Logic: 싱글 스레드 게임 루프, Monster AI (FSM), 맵 인스턴스 관리
- Contents: 인벤토리 시스템, 퀘스트 시스템 (수락/진행/완료), NPC 상호작용
- Data/DB: MySQL 연동 (Dapper), JSON 기반 리소스 로더, 비동기 DB 작업 큐

🎮 MMORPG 게임 콘텐츠 시스템

💾 데이터 지속성 (Persistence)



## 3. 주요 기능 구현 목록
* IOCP
* 싱글 스레드 게임 로직 처리
* 로그인
* 세션 관리
* 플레이어 상태 관리
* 몬스터 AI
* 맵
* 인벤토리
* 퀘스트 시스템
* JSON 기반 리소스 로드 시스템


### 로그인 처리 및 세션 시스템
* 로그인

### 싱글 스레드 게임 로직 처리
* 비동기, 파이버 구조

### 네트워크 처리
* IOCP 기반 네트워크 통신 구현

### MMORPG 게임 시스템 구현
* 플레이어 상태 관리, 몬스터 AI, 맵 인스턴스 관리 등
* 컴포넌트 기반 데이터 관리
* 비동기 DB 작업 큐 시스템을 통한 데이터 지속성(Persistence) 구현
* 퀘스트 시스템 구현: NPC 대화, 퀘스트 수락/진행/완료 처리 및 보상 지급

### 데이터 관리 및 최적화
* JSON 기반 템플릿 시스템: 몬스터, 아이템, 맵, 퀘스트 데이터 로드 및 캐싱

### DB 연동 및 트랜잭션 처리
* MySQL과 Dapper를 활용한 데이터베이스 연동 및 CRUD 작업 구현
* 멀티 스레드 DB 워커: BlockingCollection을 활용한 DB 작업 큐 처리



### 🔐 인증 및 세션 시스템
* **비동기 계정 인증**: `DbTransactionWorker`를 통해 DB 조회 시 발생하는 프리징 현상 없이 로그인 및 세션 생성을 처리합니다.
* **IOCP 기반 고성능 네트워크**: `UserSession`과 `RecvBuffer`를 활용하여 대규모 동시 접속 환경에서도 안정적인 패킷 수신이 가능합니다.

### 🎮 게임 월드 및 전투 로직
* **싱글 스레드 로직 루프**: `GameLogicThread`에서 모든 게임 로직을 싱글 스레드에서 순차적으로 처리함으로써 데이터 경합(Race Condition) 문제를 원천 차단했습니다.
* **몬스터 AI 및 스폰 시스템**: `MonsterData` 템플릿을 기반으로 몬스터의 스탯과 스폰 위치를 관리하며, 플레이어 인식 및 추적 로직을 수행합니다.

### 🎒 컴포넌트 기반 데이터 관리
* **플러그인 형태의 기능 확장**: 플레이어 객체에 `Inventory`, `QuestComponent`를 컴포넌트 방식으로 부착하여 객체 간 결합도를 낮추고 유지보수성을 높였습니다.
* **정적 데이터 캐싱**: 게임 실행 시 몬스터, 아이템, 맵 정보를 `DataManager`에 미리 로드하여 런타임 시 I/O 부하를 최소화했습니다.

### 💾 비동기 DB 데이터 지속성(Persistence)
* **비동기 작업 큐 시스템**: `DbJob` 클래스와 큐를 이용해 로직 스레드와 DB 스레드를 완전히 분리, DB 병목이 게임 프레임에 영향을 주지 않도록 설계했습니다.
* **데이터 무결성 보장**: 퀘스트 완료나 보상 지급 등 중요한 작업 시 DB 트랜잭션을 적용하여 예외 상황에서도 데이터 오류가 발생하지 않게 구현했습니다.

---

## 3. 주요 기능 구현 목록
🔐 인증 및 세션 관리
비동기 로그인 시스템: DB 조회를 통한 사용자 인증 및 로그인 처리.

멀티 세션 관리: 중복 로그인 방지 및 끊긴 연결(Disconnected)에 대한 안전한 세션 해제.

IOCP 기반 네트워크 통신: 대규모 접속 처리를 위한 비동기 입출력 모델 구현.

⚔️ 플레이어 및 전투 시스템
캐릭터 상태 동기화: 플레이어의 위치(XYZ), 스탯(HP, Damage), 레벨 및 경험치 실시간 관리.

몬스터 AI 및 전투: 몬스터 스폰, 플레이어 인식(Aggro) 및 전투 로직 처리.

맵 인스턴스 관리: 각 맵별 독립적인 이벤트 처리 및 이동 시스템.

📦 아이템 및 데이터 시스템
컴포넌트 기반 인벤토리: 플레이어 귀속형 인벤토리 시스템 및 아이템 수량/강화 정보 관리.

정적 데이터 로드: JSON 기반 템플릿(Monster, Item, Map, Quest) 로드 및 캐싱 시스템.

데이터 지속성(Persistence): 게임 내 모든 변화를 비동기 큐를 통해 DB에 안전하게 기록.

📜 퀘스트 시스템
퀘스트 생명 주기 관리: NPC를 통한 퀘스트 수락, 진행 상태 추적 및 완료 보상 지급.

조건 감지 시스템: 몬스터 처치 등 퀘스트 달성 조건을 실시간으로 체크하는 컴포넌트 로직.

트랜잭션 처리: 퀘스트 완료 시 진행도 삭제와 보상 지급을 하나의 트랜잭션으로 묶어 데이터 무결성 보장.



## 4. 시스템 아키텍처 및 클래스 구성
- 프로그램 메인
	- Program.cs: 서버 초기화 및 실행 진입점

- 게임 로직 스레드
	- GameLogicThread.cs: 게임 로직 처리 및 일정한 틱 레이트 유지

- 게임 데이터 관리
	- DataManager.cs: 게임 데이터 관리 [싱글톤]
	- MonsterData.cs: 몬스터 데이터 로드 및 관리
	  - MonsterTemplate.cs: 몬스터 템플릿 정의
	- ItemData.cs: 아이템 데이터 로드 및 관리
	  - ItemTemplate.cs: 아이템 템플릿 정의
	- MapData.cs: 맵 데이터 로드 및 관리
	  - MapTemplate.cs: 맵 템플릿 정의
	- QuestData.cs: 퀘스트 데이터 로드 및 관리
	  - QuestTemplate.cs: 퀘스트 템플릿 정의
		
- 패킷 처리
	- Packet.cs: 패킷 정의 및 직렬화/역직렬화 구현
	- PacketHandler.cs: 패킷 처리 로직 구현
	- PacketMaker.cs: 패킷 생성
	- PacketSerializer.cs: 패킷 직렬화
	- RecvBuffer.cs: 수신 버퍼 관리
	- ~~PacketParser.cs: 패킷 역직렬화~~ (미리 역직렬화하여 게임로직스레드의 부하를 줄이려고 테스트 해봤으나 효과가 크지 않았음)
	- ~~Protocol.cs: 패킷 정의~~ Google Protocol Buffers를 활용하여 JSON Serializition 비용을 줄이려고 테스트 해봤으나 효과가 크지 않았음)

- 네트워크 및 세션
	- SessionManager.cs: 세션 관리 및 연결/해제 처리
	- UserSession.cs: 네트워크 처리(IOCP)를 담당하는 세션 객체
	
- 로그인
	- LoginManager.cs: 로그인 처리 및 세션 관리
	
- DB 제어
	- DbTransactionWorker.cs: 큐에 들어오는 DB 작업을 처리하는 워커 스레드 생성 및 처리 [싱글톤]
	- DbManager.cs: CRUD 함수가 구현된 곳
	- DbJob.cs: DB 작업 정보를 담는 클래스 (나중에 추가확장 가능. 예: 작업 우선순위, 재시도 횟수 등)
	- RequestDto.cs: DTO 정의 클래스

- 게임 시스템
	- 플레이어 
		- PlayerManager.cs: 플레이어 관리 [싱글톤]
		- Player.cs: 플레이어 정보 및 상태 관리
			- 인벤토리
				- Inventory.cs: 인벤토리 관리 (플레이어에 붙는 컴포넌트)
			- 퀘스트
				- QuestComponent.cs: 퀘스트 진행 상태 관리 (플레이어에 붙는 컴포넌트)
					- QuestProgress.cs: 퀘스트 진행 상태 정보
					- QuestCondition.cs: 퀘스트 조건 정보
	- 맵
		- MapManager.cs: 맵 관리 [싱글톤]
		- Map.cs: 맵 정보 및 해당 맵에서 발생하는 모든 이벤트 처리

	- 몬스터
		- Monster.cs: 몬스터 정보 및 상태 관리
	- NPC
		- NpcManager.cs: NPC 대화 처리 및 퀘스트 수락/진행/완료 처리 [싱글톤]
	 - 아이템
		- ItemManager.cs: 퀘스트 컴포넌트에서 보상지급 시 사용되는 아이템 생성 DB 요청 로직을 따로 분리함

## 보류
1. 기반 시스템(Framework)
	- 비동기 소켓 서버: IOCP 모델을 사용한 대규모 연결 처리 및 세션 관리 (UserSession, SessionManager)
	- 패킷 직렬화 시스템: 가변 길이 패킷 처리 및 자동 직렬화/역직렬화 (PacketSerializer, PacketParser)
	- 데이터 관리 시스템: JSON 기반의 게임 데이터(몬스터, 아이템, 맵, NPC) 로드 및 관리 (DataManager)
	- 스케줄러: GameLogicThread를 통한 일정한 틱 레이트(30 FPS) 유지 및 작업 큐 처리
2. 게임 시스템(Game Systems)
	- 캐릭터 시스템: 캐릭터 생성, 삭제, 정보 조회 기능 구현 (CharacterManager)
	- 전투 시스템: 몬스터와의 전투 로직 구현 (CombatSystem)
	- 아이템 시스템: 아이템 획득, 사용, 인벤토리 관리 기능 구현 (ItemManager)
	- 퀘스트 시스템: 퀘스트 수락, 진행, 완료 기능 구현 (QuestManager)
3. 데이터베이스 연동(Database Integration)
	- MySQL과 Dapper를 활용한 데이터베이스 연동 및 CRUD 작업 구현 (DatabaseManager)
	- 멀티 스레드 DB 워커: BlockingCollection을 활용한 DB 작업 큐 처리 (DBWorker)
4. 로깅 및 모니터링(Logging & Monitoring)
	- Serilog을 활용한 로깅 시스템 구축 및 Seq와 연동하여 실시간 로그 모니터링 구현 (Logger)
5. 테스트 및 최적화(Testing & Optimization)
	- 단위 테스트: NUnit을 활용한 주요 시스템의 단위 테스트 작성 (CharacterManagerTests, CombatSystemTests 등)
	- 성능 최적화: 패킷 직렬화/역직렬화 최적화 및 게임 로직 처리 개선

## 4. 실행 방법 (Usage)
- db.config 파일에 MySQL 연결 문자열 설정
- GameData/ 폴더 내 JSON 데이터 확인
- 솔루션 빌드 및 실행 (기본 포트: 7777)