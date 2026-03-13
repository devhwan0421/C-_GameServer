using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

public class Monster
{
    public int SpawnId { get; set; }
    public int MonsterId { get; set; }
    public string Nickname { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Damage { get; set; }
    public int State { get; set; } = 1; //9: 사망
    public int Dir { get; set; } = 1;

    public int Exp { get; set; }
    public List<int> DropItemIdList { get; set; }

    MonsterPatrolInfo _moveData;

    //private bool IsDeath { get; set; }
    //public int MonsterType { get; set; } //0: 일반 몬스터, 1: 보스 몬스터
    public float RespawnSec { get; set; } = 10.0f; //respawnTime을 0으로 하면 부활하지 않음으로 세팅
    private float _respawnTimer = 0;

    public Monster(int spawnId, MonsterTemplate monsterData, MonsterPatrolInfo moveData, Map map)
    {
        SpawnId = spawnId;
        MonsterId = monsterData.MonsterId;
        Nickname = monsterData.Nickname;
        Hp = monsterData.MaxHp;
        MaxHp = monsterData.MaxHp;
        Damage = monsterData.Damage;
        Exp = monsterData.Exp;
        DropItemIdList = monsterData.DropItemIdList.ToList();

        PosX = moveData.PosX;
        PosY = moveData.PosY;
        MinPosX = moveData.MinPosX;
        MaxPosX = moveData.MaxPosX;

        _map = map;
        _moveData = moveData;
    }

    private Map _map;

    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public float MinPosX { get; set; }
    public float MaxPosX { get; set; }

    public float Speed { get; set; } = 2.0f;

    public bool IsDirty { get; set; } = false;

    private Player _target;

    private static readonly Random _rand = new Random();

    private float _idleTimer = 0;
    private float _knockbackTimer = 0;
    private float _knockbackSec = 0.3f;
    private float _knockbackSpeed = 4.0f;

    public int GetDropItem()
    {
        if (DropItemIdList.Count == 0) return -1;
        return DropItemIdList[_rand.Next(DropItemIdList.Count)];
    }

    public void Update(float deltaTime)
    {
        //if(SpawnId == 3)
            //Console.WriteLine($"nickname: {Nickname}, x: {PosX}, y: {PosY}, state: {State}, minX: {MinPosX}, maxX: {MaxPosX}");
        switch (State)
        {
            case 0: //제자리
                UpdateIdle(deltaTime);
                break;
            case 1: //이동중
                UpdatePatrol(deltaTime);
                break;
            case 2: //넉백
                UpdateKnockback(deltaTime);
                break;
            case 3: //공격당하면 플레이어 쫓아가기
                UpdateChasing(deltaTime);
                break;
            case 9: //사망
                WaitRespawn(deltaTime);
                break;
        }
    }

    /* _packetSendTimer += deltaTime;
         if(_packetSendTimer >= PACKET_SEND_INTERVAL)
         {
             _packetSendTimer = 0;
             BroadcastMonsterUpdates();
         }*/


    private void UpdateIdle(float deltaTime)
    {
        //Console.WriteLine("idle");
        _idleTimer += deltaTime;
        if (_idleTimer > 5.0) //2초간 쉬면 이동 상태로 변경
        {
            _idleTimer = 0;
            State = 1;
            IsDirty = true;
        }
    }

    private void UpdatePatrol(float deltaTime)
    {
        //Console.WriteLine("patrol");
        PosX += Dir * Speed * deltaTime;

        if (PosX >= MaxPosX)
        {
            PosX = MaxPosX;
            Dir = -1;
            State = 0;
            _idleTimer = 0;
        }
        else if (PosX <= MinPosX)
        {
            PosX = MinPosX;
            Dir = 1;
            State = 0;
            _idleTimer = 0;
        }
        IsDirty = true;
    }

    public void OnKnockback(Player player, int knockbackDir)
    {
        _target = player;

        //넉백될 방향
        Dir = knockbackDir;

        //넉백 상태로 지정
        State = 2;
    }

    private void UpdateKnockback(float deltaTime)
    {
        float knockbackPos = Dir * _knockbackSpeed * deltaTime;
        if ((PosX + knockbackPos) > MinPosX && (PosX + knockbackPos) < MaxPosX)
            PosX += knockbackPos;

        _knockbackTimer += deltaTime;
        //Console.WriteLine($"넉백상태: {_knockbackTimer}");
        if (_knockbackTimer > _knockbackSec) //넉백 시간 후 State를 3로 하여 플레이어 쫓아가게
        {
            _knockbackTimer = 0;
            State = 3;
            //Console.WriteLine($"State: {State}");
        }
        IsDirty = true;
    }

    private void UpdateChasing(float deltaTime)
    {
        //Log.Debug($"MonsterId: {MonsterId}, State: {State}, Target: {_target.Nickname}, TargetCurrentMap: {_target.CurrentMap}, TargetPos: ({_target.PosX}, {_target.PosY})");
        if (_target == null || _target.CurrentMap != _map || _target.State == 9)
        {
            State = 0;
            return;
        }

        if (PosX >= MinPosX && PosX <= MaxPosX)
        {
            float movePos = Dir * Speed * deltaTime; ;
            Dir = _target.PosX > PosX ? 1 : -1;
            if ((PosX + movePos) > MinPosX && (PosX + movePos) < MaxPosX)
                PosX += movePos;
            IsDirty = true;
        }

        float distX = _target.PosX - PosX;
        float distY = _target.PosY - PosY;
        float dist = (distX * distX) + (distY * distY);

        if (dist > 64.0f)
        {
            _target = null;
            State = 0;
            IsDirty = true;
        }
    }

    public void WaitRespawn(float deltaTime)
    {
        //몬스터 타입체크
        if (RespawnSec == 0) return;

        _respawnTimer += deltaTime;

        if (_respawnTimer > RespawnSec)
        {
            _respawnTimer = 0;
            State = 1;
            PosX = _moveData.PosX;
            PosY = _moveData.PosY;
            Hp = MaxHp;
            _map.RespawnMonster(SpawnId, _moveData.PosX, _moveData.PosY);
            IsDirty = true;
        }
    }
}