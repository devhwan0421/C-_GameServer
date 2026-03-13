/*using System.Collections.Generic;

public class QuestProgress
{
    public int QuestId { get; set; }
    public int State { get; set; } //1:Available, 2:InProgress, 3:Completable

    public List<QuestCondition> Conditions { get; set; } = new List<QuestCondition>();

    public bool IsAllSatisfied()
    {
        foreach (var condition in Conditions)
        {
            if (!condition.IsStisfied()) return false;
        }
        return true;
    }
}*/

using System.Collections.Generic;

public class QuestProgress
{
    public QuestTemplate Template { get; private set; }

    public List<QuestCondition> Conditions { get; private set; } = new List<QuestCondition>();

    public int State { get; set; } //0:진행가능, 1:진행중, 2:완료가능, 3:완료

    public QuestProgress(QuestTemplate template)
    {
        Template = template;
        State = 1;

        foreach (var data in Template.Conditions)
        {
            Conditions.Add(new QuestCondition(data));
        }
    }

    public bool IsAllSatisfied()
    {
        foreach (var data in Conditions)
        {
            if (!data.IsSatisfied) return false;
        }
        return true;
    }
}