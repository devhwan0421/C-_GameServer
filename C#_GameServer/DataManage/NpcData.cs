using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class NpcData
{
    private Dictionary<int, NpcTemplate> _npcDatas = new Dictionary<int, NpcTemplate>();

    public int Count() => _npcDatas.Count();

    public void LoadJson()
    {
        string folderPath = "./GameData/Npc";

        string[] files = Directory.GetFiles(folderPath, "npc*.json");

        foreach (string filePath in files)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                NpcTemplate npcTemplate = new NpcTemplate
                {
                    NpcId = root.GetProperty("NpcId").GetInt32(),
                    Name = root.GetProperty("Name").GetString(),
                    DefaultDialogueId = root.GetProperty("DefaultDialogueId").GetInt32()
                };

                if (root.TryGetProperty("Dialogues", out JsonElement dialoguesElement))
                {
                    foreach (var property in dialoguesElement.EnumerateObject())
                    {
                        int dialogueId = int.Parse(property.Name);
                        JsonElement jsonElement = property.Value;
                        int type = jsonElement.GetProperty("Type").GetInt32();

                        DialogueBase dialogue = null;

                        switch (type)
                        {
                            case 0:
                                dialogue = JsonSerializer.Deserialize<DialogueSimple>(jsonElement.GetRawText());
                                break;
                            case 1:
                                dialogue = JsonSerializer.Deserialize<DialogueOk>(jsonElement.GetRawText());
                                break;
                            case 2:
                                dialogue = JsonSerializer.Deserialize<DialogueNext>(jsonElement.GetRawText());
                                break;
                            case 3:
                                dialogue = JsonSerializer.Deserialize<DialogueAcceptDecline>(jsonElement.GetRawText());
                                break;
                            case 4:
                                dialogue = JsonSerializer.Deserialize<DialogueSelection>(jsonElement.GetRawText());
                                break;
                        }

                        npcTemplate.Dialogues.Add(dialogueId, dialogue);
                    }
                }

                if (root.TryGetProperty("QuestMap", out JsonElement questMapElement))
                {
                    npcTemplate.QuestMap = JsonSerializer.Deserialize<Dictionary<int, QuestDialogueMap>>(questMapElement.GetRawText());
                }

                _npcDatas.Add(npcTemplate.NpcId, npcTemplate);

                Console.WriteLine($"[데이터 로드] Id: {npcTemplate.NpcId}, name: {npcTemplate.Name}, DefaultDialogueId: {npcTemplate.DefaultDialogueId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public NpcTemplate GetNpcData(int npcId)
    {
        if (_npcDatas.TryGetValue(npcId, out var data))
            return data;
        return null;
    }

    public DialogueBase GetDialogueData(int npcId, int dialogueId)
    {
        if (_npcDatas.TryGetValue(npcId, out var npcData))
        {
            if (npcData.Dialogues.TryGetValue(dialogueId, out var dialogue))
            {
                return dialogue;
            }
        }
        return null;
    }
    /*public Dialogue GetDialogueData(int npcId, int dialogueId)
    {
        if (_npcDatas.TryGetValue(npcId, out var data))
        {
            foreach (var dialogue in data.Dialogues)
            {
                if (dialogue.Key == dialogueId)
                    return dialogue.Value;
            }
        }
        return null;
    }*/
}