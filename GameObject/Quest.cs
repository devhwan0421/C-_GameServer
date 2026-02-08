using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Quest
{
    Player _myPlayer;

    private Dictionary<int, QuestTemplate> _inProgressQuest = new Dictionary<int, QuestTemplate>();
    private Dictionary<int, QuestTemplate> _completedQuest = new Dictionary<int, QuestTemplate>();
    private Dictionary<int, QuestTemplate> _availableQuest = new Dictionary<int, QuestTemplate>();

    //원본 템플릿이 참조되지 않게 new로 넣을 것. 퀘스트템플릿의 State{진행가능, 진행중, 완료} 체크
    private Dictionary<int, Dictionary<int, QuestTemplate>> _quest = new Dictionary<int, Dictionary<int, QuestTemplate>>();

    public Quest(Player myPlayer)
    {
        _myPlayer = myPlayer;
    }

    //내 퀘스트 중 해당 npc에 대한 모든 퀘스트 목록을 반환
    //목록에 없는 것은 진행가능으로 간주
    //목록에 있는 것은 State 체크하여 사용
    public Dictionary<int, QuestTemplate> GetNpcQuestInfo(int npcId)
    {
        if (_quest.TryGetValue(npcId, out var quest)) return quest;
        return null;
    }

    /*public List<QuestTemplate> CheckNpcQuestComplete(int npcId)
    {
        List<QuestTemplate> result = new List<QuestTemplate>();
        if (_quest.TryGetValue(npcId, out var questInfos))
        {
            if(questInfos)
        }
    }*/

    




    //지금은 한 npc에 하나의 퀘스트 밖에 못 넣음. 이후 개선 필요
    public void AddQuest(int npcId, int questId)
    {
        QuestTemplate questTemplate = DataManager.Instance.Quest.GetQuestTemplate(questId);
        if (questTemplate == null) return;

        _inProgressQuest.Add(npcId, questTemplate);
        Console.WriteLine("퀘스트 추가됨");
    }

    //이름이 명확하지 않음. 해당 npc에서 받은 퀘스트가 있는지 확인하는 함수
    //받은 퀘스트가 있다면 퀘스트 정보 반환.
    //없다면 null 반환
    public QuestTemplate IsQuest(int npcId)
    {
        if (_inProgressQuest.TryGetValue(npcId, out var questInfo))
        {
            return questInfo;
        }
        return null;
    }

    /*public bool CheckCompleteQuest(int npcId, int questId) //퀘스트에 필요한 정보 모두 받아와야 함
    {
        return false;
    }*/
}