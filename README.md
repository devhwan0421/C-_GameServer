# Silvervine Server Engine 2 설계 리뷰를 바탕으로 재구성한 C# IOCP & Stackless Fiber 게임서버
 [실버바인 서버 엔진 2 설계 리뷰 (NDC2018)](http://ndcreplay.nexon.com/NDC2018/sessions/NDC2018_0075.html)

## 1. 프로젝트 개요
- 프로젝트 명: C# GameServer (Silvervine Engine 2 Reference)
- 설명: 마비노기 모바일에서 사용된 실버바인 서버 엔진의 구조를 학습하고 IOCP 기반의 비동기 소켓 통신과 싱글 스레드 게임 로직 처리를 C#으로 구현한 프로젝트입니다.

## 2. 핵심 기술 스택
- 언어: C#
- 네트워크: IOCP
- 동시성: SynchroinizationContext 기반의 GameLogicThread, BlockingCollection을 활용한 멀티 스레드 DB 워커
- 데이터베이스: MySQL, Dapper(ORM)
- Data Serialization: JSON(System.Text.Json) 및 Google Protobuf (이동 패킷에서의 부하 감소 테스트를 위해 사용해봄)
- 로깅: Serilog(Seq 연동)

## 3. 기능 구현 목록
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