using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public class QuestComponent
{
    private Player _owner;

    //진행중인 퀘스트 리스트
    private Dictionary<int, QuestProgress> _inProgressQuests = new Dictionary<int, QuestProgress>();

    private HashSet<int> _completedQuests = new HashSet<int>();

    //key: conditionType, value: 퀘스트 리스트
    private Dictionary<int, List<QuestProgress>> _eventHandlers = new Dictionary<int, List<QuestProgress>>();

    //DB업데이트 변수
    public bool IsDirty { get; private set; }
    private HashSet<int> _dirtyQuestIds = new HashSet<int>();

    public QuestComponent(Player owner)
    {
        _owner = owner;
        _completedQuests.Add(0);
    }

    public void SetDirty(int questId)
    {
        IsDirty = true;
        _dirtyQuestIds.Add(questId);
    }

    public void ClearDirty()
    {
        IsDirty = false;
        _dirtyQuestIds.Clear();
    }

    public void RestoreDirty(QuestDtoSet questDto)
    {
        if (questDto == null) return;

        foreach (var quest in questDto.Quests)
        {
            _dirtyQuestIds.Add(quest.quest_id);
        }
        IsDirty = true;
    }

    public QuestDtoSet GetDirtyQuests()
    {
        var questDtoSet = new QuestDtoSet();

        foreach (int questId in _dirtyQuestIds)
        {
            if (_inProgressQuests.TryGetValue(questId, out var quest))
            {
                questDtoSet.Quests.Add(new QuestDto
                {
                    character_id = _owner.CharacterId,
                    quest_id = questId,
                    state = quest.State
                });

                foreach (var condition in quest.Conditions)
                {
                    if (condition.Data.Type == 1)
                    {
                        questDtoSet.QuestProgresses.Add(new QuestProgressDto
                        {
                            character_id = _owner.CharacterId,
                            quest_id = questId,
                            monster_id = condition.Data.TargetId,
                            current_count = condition.CurrentCount
                        });
                    }
                }
            }
        }
        return questDtoSet;
    }

    public QuestDtoSet GetQuestDtoByQuestId(int questId)
    {
        var questDtoSet = new QuestDtoSet();

        if (_inProgressQuests.TryGetValue(questId, out var quest))
        {
            questDtoSet.Quests.Add(new QuestDto
            {
                character_id = _owner.CharacterId,
                quest_id = questId,
                state = quest.State
            });

            foreach (var condition in quest.Conditions)
            {
                if (condition.Data.Type == 1)
                {
                    questDtoSet.QuestProgresses.Add(new QuestProgressDto
                    {
                        character_id = _owner.CharacterId,
                        quest_id = questId,
                        monster_id = condition.Data.TargetId,
                        current_count = condition.CurrentCount
                    });
                }
            }
        }
        return questDtoSet;
    }

    //0:진행가능, , 
    public int GetQuestState(int questId)
    {
        if (_completedQuests.Contains(questId)) return 3; //3:완료

        if (_inProgressQuests.TryGetValue(questId, out var questProgress))
        {
            return questProgress.State; //1:진행중, 2:완료가능
        }

        var questTemplate = DataManager.Instance.Quest.GetQuestTemplate(questId);
        if(questTemplate != null && _owner.Level >= questTemplate.RequiredLevel && _completedQuests.Contains(questTemplate.RequiredQuestId))
        {
            return 0;
        }

        return -1; //오류
    }

    public void AcceptQuest(int questId)
    {
        if (GetQuestState(questId) != 0) return;

        //퀘스트 정보 불러오기
        var questTemplate = DataManager.Instance.Quest.GetQuestTemplate(questId);
        if (questTemplate == null) return;

        //퀘스트 진행도 객체 생성 및 _inProgressQuests 추가
        QuestProgress questProgress = new QuestProgress(questTemplate);
        _inProgressQuests.Add(questId, questProgress);

        //퀘스트 조건 초기값 세팅 및 이벤트 핸들러 추가
        foreach (var condition in questProgress.Conditions)
        {
            //타입에 따른 조건 초기 세팅
            int type = condition.Data.Type;
            if (type == 2) //보유 아이템 수량 적용
            {
                condition.SetCount(_owner.Inventory.getItemCount(condition.Data.TargetId));
            }
            else if (type == 3) //레벨 적용
            {
                condition.SetCount(_owner.Level);
            }

            //이벤트 핸들러 등록
            if (!_eventHandlers.ContainsKey(type))
            {
                _eventHandlers[type] = new List<QuestProgress>();
            }
            if (!_eventHandlers[type].Contains(questProgress))
            {
                _eventHandlers[type].Add(questProgress);
            }
        }

        //DB업데이트 예약
        SetDirty(questId);

        //퀘스트를 받자마자 완료가능일 수 있으니 CheckQuestCompletable() 호출할 것
        CheckQuestCompletable(questProgress);
    }

    public void OnNotifyEvent(int type, int targetId, int value)
    {
        if (!_eventHandlers.TryGetValue(type, out var targetQuests)) return;

        foreach (var quest in targetQuests)
        {
            if (quest.State != 1) continue;

            bool isUpdate = false;
            foreach (var condition in quest.Conditions)
            {
                if (condition.Data.Type == type && condition.Data.TargetId == targetId)
                {
                    condition.AddCount(value);
                    isUpdate = true;
                }
            }

            if (isUpdate)
            {
                SetDirty(quest.Template.QuestId);
                CheckQuestCompletable(quest);
            }
        }
    }

    private void CheckQuestCompletable(QuestProgress quest)
    {
        if (quest.IsAllSatisfied())
        {
            quest.State = 2;

            //완료 가능 패킷 전송
            var QuestCompleteBuff = PacketMaker.Instance.QuestComplete(quest.Template.QuestId, quest.Template.QuestName);
            _owner._mySession.Send(QuestCompleteBuff);
        }
    }

    public async Task<bool> CompleteQuest(int questId)
    {
        //완료 검증
        if (!_inProgressQuests.TryGetValue(questId, out var questProgress)) return false;
        if (questProgress.State != 2) return false;

        //아직 업데이트되지 않았다면 업데이트 목록에서 제거
        if(_dirtyQuestIds.Contains(questId)) _dirtyQuestIds.Remove(questId);

        //업데이트용 Dto 생성
        QuestDtoSet questDto = GetQuestDtoByQuestId(questId);
        questDto.Quests[0].state = 3;

        //진행중 -> 완료
        _inProgressQuests.Remove(questId);
        _completedQuests.Add(questId);

        //DB 업데이트 즉시 실행
        bool result = await DbManager.CompleteQuest(questDto);
        if (!result) //실패시 상태 복구
        {
            _inProgressQuests.Add(questId, questProgress);
            _completedQuests.Remove(questId);
            SetDirty(questId);

            Log.Error($"[Quest] DB 업데이트 실패. 퀘스트 완료 불가: {questId}");
            return false;
        }

        

        //보상지급
        _owner.AddExp(questProgress.Template.RewardExp);

        List<Item> Items = await ItemManager.Instance.CreateItemAndInsertDB(questProgress.Template.RewardItemId, _owner.CharacterId);
        _owner.AddItem(Items);
        //_owner.AddItem(questProgress.Template.RewardItemId);

        //이벤트 핸들러 정리
        foreach (var condition in questProgress.Conditions)
        {
            if (_eventHandlers.TryGetValue(condition.Data.Type, out var questProgressesList))
                questProgressesList.Remove(questProgress);
        }

        //퀘스트 완료 db 업데이트, 완료된 questProgress 테이블의 데이터는 어떻게 하지? 삭제 비용이 클까?
        //1. insert, on duplicate key update
        //2. table-quest.state = 3 //생략
        //3. table-questProgress 정리
        return true;
    }

    public void LoadDbQuestTable(List<QuestDto> questList, List<QuestProgressDto> questProgressList)
    {
        if (questList == null) return;

        if (questProgressList == null)
            questProgressList = new List<QuestProgressDto>();

        Dictionary<int, List<QuestProgressDto>> questProgressMap = new Dictionary<int, List<QuestProgressDto>>();

        foreach (var questProgress in questProgressList)
        {
            if (!questProgressMap.ContainsKey(questProgress.quest_id))
            {
                questProgressMap[questProgress.quest_id] = new List<QuestProgressDto>();
            }
            questProgressMap[questProgress.quest_id].Add(questProgress);
        }

        foreach (var quest in questList)
        {
            if (quest.state == 3)
            {
                _completedQuests.Add(quest.quest_id);
            }
            else
            {
                List<QuestProgressDto> questProgressDtos = null;
                if (!questProgressMap.TryGetValue(quest.quest_id, out questProgressDtos))
                {
                    questProgressDtos= new List<QuestProgressDto>();
                }

                RestoreQuestProgress(quest, questProgressDtos);
            }
        }

        foreach (var quest in _inProgressQuests.Values)
        {
            CheckQuestCompletable(quest);
        }
    }

    public void RestoreQuestProgress(QuestDto questDto, List<QuestProgressDto> questProgressDtos)
    {
        var template = DataManager.Instance.Quest.GetQuestTemplate(questDto.quest_id);
        if (template == null) return;

        QuestProgress questProgress = new QuestProgress(template);
        questProgress.State = questDto.state;

        if (questProgress.Conditions != null)
        {
            foreach (var condition in questProgress.Conditions)
            {
                int type = condition.Data.Type;
                switch (condition.Data.Type)
                {
                    case 1: //사냥
                        var questProgressDto = questProgressDtos.Find(data => data.monster_id == condition.Data.TargetId);
                        if (questProgressDto != null)
                        {
                            condition.SetCount(questProgressDto.current_count);
                        }
                        else
                        {
                            condition.SetCount(0);
                            Log.Warning($"[Quest] 진행 데이터 누락: Quest {questDto.quest_id}, Monster {condition.Data.TargetId}");
                        }
                        break;
                    case 2: //아이템
                        condition.SetCount(_owner.Inventory.getItemCount(condition.Data.TargetId));
                        break;
                    case 3: //레벨
                        condition.SetCount(_owner.Level);
                        break;
                }

                if (!_eventHandlers.ContainsKey(type))
                {
                    _eventHandlers[type] = new List<QuestProgress>();
                }

                if (!_eventHandlers[type].Contains(questProgress))
                {
                    _eventHandlers[type].Add(questProgress);
                }
            }
        }

        _inProgressQuests.Add(questDto.quest_id, questProgress);
    }
}