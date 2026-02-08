using System;
using System.Collections.Generic;

public class QuestComponent
{
    private Player _owner;

    //진행중인 퀘스트 리스트
    private Dictionary<int, QuestProgress> _inProgressQuests = new Dictionary<int, QuestProgress>();

    private HashSet<int> _completedQuests = new HashSet<int>();

    //key: conditionType, value: 퀘스트 리스트
    private Dictionary<int, List<QuestProgress>> _eventHandlers = new Dictionary<int, List<QuestProgress>>();

    public QuestComponent(Player owner)
    {
        _owner = owner;
        _completedQuests.Add(0);
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

        var questTemplate = DataManager.Instance.Quest.GetQuestTemplate(questId);
        if (questTemplate == null) return;

        QuestProgress questProgress = new QuestProgress(questTemplate);
        _inProgressQuests.Add(questId, questProgress);

        foreach (var condition in questProgress.Conditions)
        {
            int type = condition.Data.Type;
            if (type == 2)
            {
                //아이템 획득 퀘스트 조건의 경우 이미 보유중일 수 있으니 초기 업데이트 필요
                condition.AddCount(_owner.Inventory.getItemCount(condition.Data.TargetId));
            }
            else if (type == 3)
            {
                //레벨업의 조건도 동일하게 초기 업데이트
                condition.AddCount(_owner.Level);
            }

            if (!_eventHandlers.ContainsKey(type))
            {
                _eventHandlers[type] = new List<QuestProgress>();
            }
            _eventHandlers[type].Add(questProgress);
        }

        //아이템 획득 퀘스트 조건의 경우 이미 보유중일 수 있으니 초기 업데이트 필요
        //1:몬스터사냥, 2:아이템보유, 3:레벨

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

    public void CompleteQuest(int questId)
    {
        if (!_inProgressQuests.TryGetValue(questId, out var questProgress)) return;
        if (questProgress.State != 2) return;

        //보상지급
        _owner.AddExp(questProgress.Template.RewardExp);
        _owner.AddItem(questProgress.Template.RewardItemId);

        _inProgressQuests.Remove(questId);
        _completedQuests.Add(questId);

        foreach (var condition in questProgress.Conditions)
        {
            if (_eventHandlers.TryGetValue(condition.Data.Type, out var questProgressesList))
                questProgressesList.Remove(questProgress);
        }
    }
}