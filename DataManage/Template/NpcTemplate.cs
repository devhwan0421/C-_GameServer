using System.Collections.Generic;

public class NpcTemplate
{
    public int NpcId { get; set; }
    public string Name { get; set; }
    public int DefaultDialogueId { get; set; }

    public Dictionary<int, DialogueBase> Dialogues { get; set; } = new Dictionary<int, DialogueBase>();

    public Dictionary<int, QuestDialogueMap> QuestMap { get; set; } = new Dictionary<int, QuestDialogueMap>();
}
/*public class NpcTemplate
{
    public int NpcId { get; set; }
    public string Name { get; set; }
    public int DefaultDialogueId { get; set; }

    public Dictionary<int, Dialogue> Dialogues { get; set; } = new Dictionary<int, Dialogue>();

    public Dictionary<int, QuestDialogueMap> QuestMap { get; set; } = new Dictionary<int, QuestDialogueMap>();
}*/

/*public class Dialogue
{
    public int NpcId { get; set; }
    public int DialogueId { get; set; }
    public int Type { get; set; } //0:Simple, 1:Ok, 2:Next, 3:AcceptDecline, 4:Selection
    public int NextDialogueId { get; set; }
    public int QuestId { get; set; }
    public bool IsQuestList { get; set; } //퀘스트 목록을 보여주는 건지 체크용
    public string Contents { get; set; }
    
    public bool checkQuestState { get; set; }

    public int RequiredQuestId { get; set; }
    public int RequiredStateId { get; set; }

    //public List<DialogueSelection> Selections { get; set; } = new List<DialogueSelection>();
}*/

public class QuestDialogueMap
{
    public string QuestTitle { get; set; }
    public int StartId { get; set; }
    public int ProgressId { get; set; }
    public int CompletableId {  get; set; }
    //public int CompleteId { get; set; }
}

/*public class DialogueSelection
{
    public string Text { get; set; } //버튼에 표시될 문구
    public int TargetId { get; set; } // 선택 시 연결될 아이디(퀘스트목록이면 퀘스트아이디, 퀘스트목록이 아니면 다이얼로그아이디)
}*/

/*public class NpcTemplate
{
    public int NpcId { get; set; }
    public string Name { get; set; }

    public List<Dialogue> DialogueList { get; set; } = new List<Dialogue>();
}

public class Dialogue
{
    public int NpcId { get; set; }
    public int DialogueId { get; set; }
    public int Type { get; set; } //0:simple, 1:ok, 2:next, 3:accept/decline
    public int NextDialogueId { get; set; } //type이 2라면 next누르면 nextDialogueId 서버로 전송
    public int QuestId { get; set; } //type이 3이라면 accept누르면 questId 서버로 전송
    public string Contents { get; set; }
    public int CompletedDialogueId { get; set; } // 퀘스트 완료시
    public int InProgressDialogueId { get; set; } // 퀘스트 조건 미완료시
}*/