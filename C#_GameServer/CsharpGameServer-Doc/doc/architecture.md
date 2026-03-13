# 시스템 아키텍처 및 클래스 구성
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
