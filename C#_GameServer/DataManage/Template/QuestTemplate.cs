/*public class QuestTemplate
{
    public int QuestId { get; set; }
    public int State { get; set; } //0:진행가능, 1:진행중, 2:완료
    public string Content { get; set; }
    public int Type { get; set; } //몬스터사냥, 아이템 가져오기, 특정 퀘스트 완료 후 오기, 돈 가져오기
    public int ItemId { get; set; } //임시로 아이템 가져오기 체크만 구현
    public int ItemCount { get; set; }
}*/

using System.Collections.Generic;

public class QuestTemplate
{
    public int QuestId { get; set; }
    public string QuestName { get; set; }
    public string Description { get; set; }

    public int RequiredLevel { get; set; } //퀘스트 레벨제한
    public int RequiredQuestId { get; set; } //선행 퀘스트 (0이면 없음)

    public List<ConditionData> Conditions { get; set; } = new List<ConditionData>();

    public int RewardExp { get; set; }
    public int RewardGold { get; set; }
    public List<int> RewardItemId { get; set; }
}

public class ConditionData
{
    public int Type { get; set; } //1:몬스터사냥, 2:아이템보유, 3:레벨
    public int TargetId { get; set; }
    public int TargetCount { get; set; }
}