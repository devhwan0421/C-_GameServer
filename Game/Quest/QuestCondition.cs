/*public enum ConditionType
{
    LevelUp,
    MonsterKill,
    ItemPossess
}

public abstract class QuestCondition
{
    public ConditionType Type { get; set; } //퀘스트 타입
    public int TargetId { get; set; } // 레벨은 -1, 몬스터 id, 아이템 id
    public int TargetCount { get; set; } //목표 수치 
    public int CurrentCount { get; set; } //현재 수치

    //조건이 만족되었는지 확인
    public bool IsStisfied() => CurrentCount >= TargetCount;

    //진행 상황 업데이트
    public abstract void OnEvent(ConditionType type, int id, int value);
}*/

public class QuestCondition
{
    public ConditionData Data { get; private set; }
    public int CurrentCount { get; set; }

    public QuestCondition(ConditionData data)
    {
        Data = data;
        CurrentCount = 0;
    }

    public bool IsSatisfied => CurrentCount >= Data.TargetCount;

    public void AddCount(int value)
    {
        CurrentCount += value;
        if (CurrentCount > Data.TargetCount) CurrentCount = Data.TargetCount;
    }
}