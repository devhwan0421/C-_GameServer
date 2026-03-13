using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public class QuestData
{
    private Dictionary<int, QuestTemplate> _questDatas = new Dictionary<int, QuestTemplate>();

    public int Count() => _questDatas.Count();

    public void LoadJson()
    {
        string folderPath = "./GameData/Quest";

        string[] files = Directory.GetFiles(folderPath, "quest*.json");

        foreach (string filePath in files)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);

                QuestTemplate quest = JsonSerializer.Deserialize<QuestTemplate>(jsonString);

                if (_questDatas != null) _questDatas.Add(quest.QuestId, quest);

                Console.WriteLine($"[데이터 로드] Id: {quest.QuestId}, name: {quest.QuestName}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public QuestTemplate GetQuestTemplate(int questId)
    {
        if (_questDatas.TryGetValue(questId, out var data))
            return data;
        return null;
    }
}