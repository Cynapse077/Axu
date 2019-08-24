using LitJson;
using System.Collections.Generic;

public class DialogueSingle : IAsset
{
    public string ID { get; set; } //Faction ID or reg ID
    public string ModID { get; set; }
    public string[] dialogues;

    public DialogueSingle() { dialogues = new string[0]; }
    public DialogueSingle(JsonData dat)
    {
        ID = dat["ID"].ToString();
        dialogues = new string[dat["Dialogues"].Count];

        for (int i = 0; i < dat["Dialogues"].Count; i++)
        {
            dialogues[i] = dat["Dialogues"][i].ToString();
        }
    }
}

public class DialogueNode : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string display;
    public List<DialogueResponse> options;
    public LuaCall onSelect;

    public DialogueNode()
    {
        ID = "null";
        display = "ERROR";
        options = new List<DialogueResponse>()
            {
                new DialogueResponse("End", "End")
            };
    }

    public DialogueNode(JsonData dat)
    {
        FromJson(dat);
    }

    void FromJson(JsonData dat)
    {
        ID = dat["ID"].ToString();
        display = dat["Display"].ToString();
        options = new List<DialogueResponse>();

        for (int i = 0; i < dat["Responses"].Count; i++)
        {
            string disp = dat["Responses"][i]["Display"].ToString();
            string nextID = dat["Responses"][i]["GoTo"].ToString();

            options.Add(new DialogueResponse(disp, nextID));
        }

        onSelect = null;

        if (dat.ContainsKey("OnSelect"))
        {
            onSelect = new LuaCall(dat["OnSelect"].ToString());
        }
    }

    public struct DialogueResponse
    {
        public string display;
        public string nextID;

        public DialogueResponse(string dis, string next)
        {
            display = dis;
            nextID = next;
        }
    }
}
