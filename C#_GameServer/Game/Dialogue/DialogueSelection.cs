using System.Collections.Generic;

public class DialogueSelection : DialogueBase
{
    //public bool IsQuestList { get; set; }
    public List<DialogueSelectionOption> Selections { get; set; } = new List<DialogueSelectionOption>();
}

public class DialogueSelectionOption
{
    public int OptionType { get; set; }
    public int DialogueId { get; set; }
    public string Contents { get; set; }
    public int QuestId { get; set; }
}