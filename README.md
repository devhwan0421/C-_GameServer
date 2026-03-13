# 🚀 C# Game Server
> **NDC2018 [실버바인 서버 엔진 2] 설계 리뷰를 바탕으로 재구성한 C# IOCP & Stackless Fiber 서버**

---

## 1. 프로젝트 소개
- 프로젝트 명: C# GameServer (Silvervine Engine 2 Reference)
- 설명: 마비노기 모바일에서 사용된 실버바인 서버 엔진의 구조를 학습하고 IOCP 기반의 비동기 소켓 통신과 싱글 스레드 게임 로직 처리를 C#으로 구현한 프로젝트

## 2. 핵심 기술 스택
- 언어: C#
- 네트워크: IOCP
- 데이터 형식: JSON[리소스 로드, 패킷 직렬화(일부는 ProtoBuf 테스트 사용)]
- 데이터베이스: MySQL, Dapper(ORM)
- 로깅: Serilog(Seq 연동)

## 3. 시작하기
이 프로젝트는 윈도우 환경에서의 빌드와 GitHub Actions를 통한 자동 배포(CI/CD)를 지원

### **1) 필수 소프트웨어 설치 (Windows Server)**
서버의 PowerShell에서 아래 명령어를 실행하여 빌드 환경을 구축
```powershell
# .NET Framework 4.7.2 Developer Pack 설치
winget install --id Microsoft.NetFramework.4.7.2.DevPack -e --source winget
```

```powershell
# 비주얼 스튜디오 빌드 도구 설치
winget install --id Microsoft.VisualStudio.2022.BuildTools -e --source winget
```

### **2) GitHub Actions 자동 배포 설정**
`master` 브랜치 푸시 시 **Self-hosted Runner**를 통해 서버에 자동 배포되도록 구성됨

### **A. Self-hosted Runner 등록**
1. GitHub 저장소의 Settings > Actions > Runners 메뉴로 이동
2. New self-hosted runner를 클릭하고 Windows를 선택
3. 가이드에 따라 서버의 PowerShell에서 스크립트를 실행하여 Runner를 등록

### **B. 환경 변수(Secrets) 등록**
보안을 위해 DB 연결 문자열은 GitHub Secrets에 등록해야 함
1. Settings > Secrets and variables > Actions 메뉴로 이동
2. New repository secret을 클릭하여 아래 내용을 추가
	- Name: **DB_CONFIG**
	- Value: (아래 양식을 복사하여 수정 후 입력)
	```code
	Server=YOUR_HOST;Port=3306;Database=silverbine;User=YOUR_USER;Password=YOUR_PASSWORD;
	```

### **3) 배포 확인**
모든 설정 후 코드를 push하면 GitHub Actions가 자동으로 빌드를 수행함

Success PID: 1234 처럼 출력되면 성공적으로 배포된 것

## 4. 테스트 가이드
본 프로젝트는 MS Test를 활용한 성능 벤치마킹 및 로직 검증을 포함하고 있음

### **방법 1: Visual Studio**
1. C#_GameServer.sln 솔루션 파일 오픈
2. 상단 메뉴에서 테스트 -> 테스트 탐색기 실행
3. UnitTestProject1 프로젝트 내의 테스트 항목을 우클릭하여 실행 클릭
	- 주요 테스트: IntegrationTest (유저 접속->맵 입장->접속 종료)
	- 주요 테스트: TestMovePacketBroadCast (1만 명 규모 브로드캐스트 성능 측정)

### **방법 2: CLI**
본 프로젝트는 .NET Framework 4.7.2 기반으로 작성되었음

터미널에서 실행하려면 Developer PowerShell for VS 환경에서 아래 명령어를 사용

```
# 1. 빌드 (먼저 수행)
msbuild

# 2. 테스트 실행 (vstest.console 사용)
vstest.console.exe .\UnitTestProject1\bin\Debug\UnitTestProject1.dll
```

## 5. 아키텍처

### 🏗️데이터 흐름도
> 클라이언트의 요청이 IOCP를 통해 수신되어 싱글 스레드 게임 로직에서 처리되고, 비동기 DB 작업으로 이어지는 전체 파이프라인

![sequenceDiagram](./Image/sequenceDiagram.png)

### 🧱서버 컴포넌트 구조
> 서버의 주요 도메인 간의 관계와 구조

![classDiagram](./Image/classDiagram.png)

### ⚡실버바인 서버엔진2 아키텍처 따라하기
- **Stackless Fiber 기반 로직 처리**
	- 실버바인의 Stackful Fiber 구조를 분석하여 C#의 `async/await`와 Task를 활용하여 `Stackless` 방식으로 구현
	- 비동기 IO 대기 구간이 포함된 로직을 동기식 순차 코드처럼 작성할 수 있게 하여 생산성을 높이는 구조로 설계
- **Single-Thread Game Loop**
	- 모든 게임 로직을 `싱글 스레드` 루프에서 순차적으로 처리하여 `데이터 일관성`을 보장
	- 멀티스레드 환경의 `경쟁상태`와 `데드락` 문제를 근본적으로 제거하여 로직 설계 난이도를 낮춤
- **Job Queue 시스템**
	- `네트워크 IO` 와 `DB` 작업을 `게임 로직 스레드`와 분리하여 병목 현상 없는 구조로 설계

### 📈 성능 지표
- **처리량:** 동시 접속자 500명, 초당 10만 패킷(이동 패킷 200/s) 무난하게 처리 가능
- **안정성:** 대규모 브로드캐스팅 상황에서도 **평균 루프 타임 8ms** 미만 유지(루프는 몬스터, 플레이어 브로드 캐스트 로직도 처리 중인 상태)
	
### 🌐 네트워크
- **IOCP 기반 비동기 네트워크**
	- 적은 수의 스레드로 여러 접속자를 효율적으로 처리
	- 세션마다 버퍼풀과 SocketAsyncEventArgs를 활용하여 GC 부담을 최소화
	- ArraySegment를 활용하는 RecvBuffer 클래스를 통해 메모리 절약

### 💾 데이터베이스
- **비동기 DB 워커**
	- 별도의 DB 전용 스레드로 분리하여 게임 로직의 중단(Blocking) 없이 안전하게 데이터를 저장하고 불러올 수 있도록 설계

### 🎮 MMORPG 게임 콘텐츠 시스템 구성
| 시스템 명칭 | 주요 역할 및 상세 기능 |
| :--- | :--- |
| **인증 시스템** | 클라이언트 접속 제어, DB 연동을 통한 사용자 인증 및 캐릭터 정보 로딩 |
| **엔티티 관리** | **플레이어** 및 **몬스터** 객체의 생명주기 및 상태 관리 |
| **월드 시스템** | 게임 내 **맵** 데이터 관리 및 구역별 엔티티 동기화 |
| **상호작용 시스템** | **NPC** 대화, **퀘스트** 수락/완료 로직 및 보상 처리 |
| **아이템 시스템** | **인벤토리** 관리, 아이템 획득 및 사용 로직 처리 |
| **리소스 로드 시스템** | **JSON 기반 리소스 관리 클래스**를 통해 코드 수정없이 콘텐츠 확장 가능 |

### 📂 프로젝트 구조
```text
Root/
├── GameLogicThread.cs
├── Program.cs
├── Protocol.cs
├── protocol.proto
├── Common/
│   └── Packet.cs
├── DataManage/
│   ├── DataManager.cs
│   ├── ItemData.cs
│   ├── MapData.cs
│   ├── MonsterData.cs
│   ├── NpcData.cs
│   ├── QuestData.cs
│   └── DataManage/Template/
│       ├── ItemTemplate.cs
│       ├── MapTemplate.cs
│       ├── MonsterTemplate.cs
│       ├── NpcTemplate.cs
│       └── QuestTemplate.cs
├── DB/
│   ├── DbManager.cs
│   ├── DbJob.cs
│   ├── DbTransactionWorker.cs
│   └── Dto/
│       └── RequestDto.cs
├── Game/
│   ├── Game/Dialogue/
│   │   ├── DialogueBase.cs
│   │   ├── DialogueSimple.cs
│   │   ├── DialogueOk.cs
│   │   ├── DialogueNext.cs
│   │   ├── DialogueAcceptDecline.cs
│   │   └── DialogueSelection.cs
│   └── Game/Quest/
│       ├── QuestComponent.cs
│       ├── QuestCondition.cs
│       └── QuestProgress.cs
├── GameData/
│   ├── Item/ (Item01.json ~ Item04.json)
│   ├── Map/ (Map01.json)
│   └── Monster/ (monster01.json ~ monster03.json)
├── GameObject/
│   ├── Player.cs
│   ├── Monster.cs
│   ├── Map.cs
│   └── Inventory.cs
├── Manager/
│   ├── PlayerManager.cs
│   ├── MapManager.cs
│   ├── SessionManager.cs
│   ├── LoginManager.cs
│   ├── NpcManager.cs
│   └── ItemManager.cs
└── Network/
    ├── UserSession.cs
    ├── PacketHandler.cs
    ├── PacketMaker.cs
    ├── PacketSerializer.cs
    ├── RecvBuffer.cs
    └── PacketParser.cs
```

## 6. 설계 상세 및 문서
- **[C# GameServer 문서](./CsharpGameServer-Doc/README.md)**

## 7. 구동 이미지
![ServerScreen](./Image/image01.PNG)

## 🔗 관련 링크
- [실버바인 서버 엔진 2 설계 리뷰 (NDC2018)](http://ndcreplay.nexon.com/NDC2018/sessions/NDC2018_0075.html)
- [C# GameServer 클라이언트](https://github.com/devhwan0421/MiniRPG)