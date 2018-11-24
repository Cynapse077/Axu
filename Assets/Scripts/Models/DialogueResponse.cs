using System.IO;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public static class DialogueList
{
    public static List<DialogueNode> nodes;

    public static void Init()
    {
        nodes = new List<DialogueNode>();

        string listToJson = File.ReadAllText(Application.streamingAssetsPath + "/Data/Dialogue/DialogueOptions.json");
        JsonData dat = JsonMapper.ToObject(listToJson);

        for (int i = 0; i < dat.Count; i++)
        {
            nodes.Add(GetDialogue(dat[i]));
        }
    }

    public static DialogueNode GetNode(string node)
    {
        foreach (DialogueNode n in nodes) {
            if (n.id == node)
                return n;
        }

        return new DialogueNode("null", "ERROR", new List<DialogueResponse>() { new DialogueResponse("End", "End" ) });
    }

    static DialogueNode GetDialogue(JsonData dat)
    {
        string id = dat["ID"].ToString();
        string display = dat["Display"].ToString();
        List<DialogueResponse> resp = new List<DialogueResponse>();

        for (int i = 0; i < dat["Responses"].Count; i++)
        {
            string disp = dat["Responses"][i]["Display"].ToString();
            string nextID = dat["Responses"][i]["GoTo"].ToString();

            resp.Add(new DialogueResponse(disp, nextID));
        }

        return new DialogueNode(id, display, resp);
    }

    public struct DialogueNode
    {
        public string id;
        public string display;
        public List<DialogueResponse> options;

        public DialogueNode(string _id, string _display, List<DialogueResponse> dops)
        {
            id = _id;
            display = _display;
            options = dops;
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
