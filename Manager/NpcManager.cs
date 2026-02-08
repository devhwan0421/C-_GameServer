using System;
using System.Collections.Generic;

public class NpcManager
{
    public static NpcManager Instance { get; } = new NpcManager();

    //클라이언트에서 Npc를 클릭하여 대화 요청했을 때 핸들러에서 호출되는 함수
    public void OnNpcTalk(UserSession session, NpcTalkRequest res)
    {
        switch (res.Type)
        {
            case 0: //일반 대화
                processDialogue(session, res.NpcId, res.DialogueId);
                break;
            case 11: //퀘스트 수락
                processQuestAccept(session, res.QuestId);
                break;
            case 12: //퀘스트 완료 체크
                processQuestComplete(session, res.NpcId, res.DialogueId, res.QuestId);
                break;
        }
    }

    private void processDialogue(UserSession session, int npcId, int dialogueId)
    {
        Console.WriteLine($"processDialogue {npcId}, {dialogueId}");
        //1. npc데이터 불러오기
        NpcTemplate npcTemplate = DataManager.Instance.Npc.GetNpcData(npcId);
        if (npcTemplate == null) return;

        //dialogueId 0일 경우 첫 대화 -> 퀘스트 체크
        //dialogueId 0이 아닐 경우 해당 dialogue 전송
        if(dialogueId == 0)
        {
            //2. npc 데이터의 퀘스트 목록에서 완료상태가 아닌 퀘스트 찾기
            //찾아서 해당 데이터만 보내주는게 낫나 아니면 다 보내고 클라이언트에서 처리하는게 낫나

            Dictionary<int, int> showQuestId = new Dictionary<int, int>();
            foreach (var questId in npcTemplate.QuestMap.Keys)
            {
                int state = session.MyPlayer.QuestComponent.GetQuestState(questId);
                Console.WriteLine($"state: {questId}, {state}");
                if (state == 3) continue;

                showQuestId.Add(questId, state);
            }

            if (showQuestId.Count > 0)
            {
                //퀘스트 목록 전송
                SendQuestSelection(session, npcTemplate, showQuestId);
            }
            else
            {
                //기본 대화 전송
                if (npcTemplate.Dialogues.TryGetValue(npcTemplate.DefaultDialogueId, out var dialogue))
                {
                    SendDialogue(session, dialogue);
                }
            }
        }
        else
        {
            //요청들어온 DialogueId를 전송(클라이언트에서 nextDialogueId를 보낸 것)
            if (npcTemplate.Dialogues.TryGetValue(dialogueId, out var dialogue))
            {
                SendDialogue(session, dialogue);
            }
        }
    }

    private void SendQuestSelection(UserSession session, NpcTemplate npcTemplate, Dictionary<int, int> showQuestId)
    {
        DialogueSelection dialogueSelection = new DialogueSelection
        {
            NpcId = npcTemplate.NpcId,
            Type = 4,
            Contents = "무슨 일이지?"
        };

        foreach (var questId in showQuestId)
        {
            if (npcTemplate.QuestMap.TryGetValue(questId.Key, out var quest))
            {
                int targetId = 0;
                string suffix = "";
                int optionType = 0;
                switch (questId.Value)
                {
                    case 0:
                        targetId = quest.StartId;
                        optionType = 0;
                        suffix = "(진행가능)";
                        break;
                    case 1:
                        targetId = quest.ProgressId;
                        optionType = 0;
                        suffix = "(진행중)";
                        break;
                    case 2:
                        targetId = quest.CompletableId;
                        optionType = 12;
                        suffix = "(완료가능)";
                        break;
                }

                dialogueSelection.Selections.Add(new DialogueSelectionOption
                {
                    OptionType = optionType,
                    DialogueId = targetId,
                    Contents = quest.QuestTitle + suffix,
                    QuestId = questId.Key
                });
            }
        }

        dialogueSelection.Selections.Add(new DialogueSelectionOption
        {
            DialogueId = npcTemplate.DefaultDialogueId,
            Contents = "그냥 대화한다"
        });

        //var questSelectionBuff = PacketMaker.Instance.QuestSelection(dialogueSelection);
        //session.Send(questSelectionBuff);
        SendDialogue(session, dialogueSelection);
    }

    private void SendDialogue(UserSession session, DialogueBase dialogue)
    {
        Console.WriteLine($"processDialogue {dialogue.NpcId}, {dialogue.Contents}");
        ArraySegment<byte> dialogueBuff = default;
        switch (dialogue.Type)
        {
            case 0: //simple
                dialogueBuff = PacketMaker.Instance.DialogueSimple((DialogueSimple)dialogue);
                break;
            case 1: //ok
                dialogueBuff = PacketMaker.Instance.DialogueOk((DialogueOk)dialogue);
                break;
            case 2: //next
                dialogueBuff = PacketMaker.Instance.DialogueNext((DialogueNext)dialogue);
                break;
            case 3: //acceptDecline
                dialogueBuff = PacketMaker.Instance.DialogueAcceptDecline((DialogueAcceptDecline)dialogue);
                break;
            case 4: //selection
                dialogueBuff = PacketMaker.Instance.DialogueSelection((DialogueSelection)dialogue);
                break;
        };

        session.Send(dialogueBuff);
    }

    private void processQuestAccept(UserSession session, int questId)
    {
        //성공 실패 받아야 함. 임시
        session.MyPlayer.QuestComponent.AcceptQuest(questId);
        Console.WriteLine("퀘스트 수락처리");
    }

    private void processQuestComplete(UserSession session, int npcId, int dialogueId, int questId)
    {
        Console.WriteLine($"processQuestComplete {npcId}, {dialogueId}, {questId}");
        int state = session.MyPlayer.QuestComponent.GetQuestState(questId);
        Console.WriteLine($"state: {state}");
        if (state != 2) return;

        Console.WriteLine($"CompleteQuest 호출");
        //성공 실패 받아야 함. 임시
        session.MyPlayer.QuestComponent.CompleteQuest(questId);

        Console.WriteLine($"완료 다이얼로그 호출");
        DialogueBase dialogue = DataManager.Instance.Npc.GetDialogueData(npcId, dialogueId);
        SendDialogue(session, dialogue);
    }
}