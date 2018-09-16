using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using LitJson;


/// ---------
/// TODO: 
///     - Add NPC placement. (Static and non-static)
///     - Button to toggle random spawns. Will need a bool in TileMap_Data to check. Can be set to map's friendly variable initially.
///     - Spawn groups.
///     - Dropdown set to underground area. Maybe replace with string, since it's much easier.
///     - Move objects and NPCs.
/// ----------

public class MapCreatorTool : MonoBehaviour
{
    public int CurrentTile = 32;
    public GameObject tilePrefab;
    public GameObject buttonPrefab;
    public GameObject loadButtonPrefab;
    public Image currentSelectedImage;
    public Sprite[] Sprites { get; protected set; }
    public MC_Selection_Type selectType = MC_Selection_Type.Paint;

    public GameObject Menu;
    public GameObject SaveMenu;
    public GameObject LoadMenu;
    public InputField mapNameInput;
    public InputField mapTypeSelector;
    public Dropdown elevationSelector;

    public Transform tileAnchor;
    public Transform objectAnchor;
    public Transform npcAnchor;
    public Transform loadButtonAnchor;
    public GameObject yesNoButton;
    public GameObject helpMenu;
    public Text locationText;
    public Text mapInfoText;
    public Text mapNameTitle;
    public RectTransform Tooltip;
    public Sprite empty;
    [HideInInspector] public Sprite[] objectSprites;

    List<ChangeHolder> changes;
    List<MapCreator_Screen> savedMaps;
    MapCreator_Cell[,] cells;
    TilesetManager tsm;
    float tileSize = 26;
    int mapToDeleteIndex = -1;
    Coord lastSelected = new Coord(0, 0);
    Coord currentMouseOver = new Coord(0, 0);
    string MapName = "";
    string CurrentLocationID = "";
    int Elevation = 0;
    Coord mapSize;

    void Start()
    {
        if (!SpriteManager.initialized)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            return;
        }

        mapNameTitle.text = "";
        mapSize = new Coord(Manager.localMapSize.x, Manager.localMapSize.y);
        tsm = GetComponent<TilesetManager>();
        Init();
        SetPlace(0);
        SetupTiles();
        DisableTooltip();
    }

    void Init()
    {
        changes = new List<ChangeHolder>();
        savedMaps = new List<MapCreator_Screen>();

        tileSize = (tileSize) * (Screen.width / 1600f);

        yesNoButton.SetActive(false);
        helpMenu.SetActive(false);
        Menu.SetActive(false);

        //Setup Object Sprites.
        objectSprites = new Sprite[ItemList.mapObjectBlueprints.Count];

        for (int i = 0; i < objectSprites.Length; i++)
        {
            objectSprites[i] = SpriteManager.GetObjectSprite(ItemList.mapObjectBlueprints[i].spriteID);

            //Clip image to bottom left
            if (objectSprites[i].texture.width > 16)
                objectSprites[i] = Sprite.Create(objectSprites[i].texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }

        for (int i = 0; i < objectSprites.Length; i++)
        {
            GameObject g = Instantiate(buttonPrefab, objectAnchor);
            g.GetComponent<MapCreator_SideButton>().Init(this, i, MapCreator_SideButton.MC_ButtonType.Object);
        }
    }

    void SetupTiles()
    {
        cells = new MapCreator_Cell[mapSize.x, mapSize.y];

        if (Tile.tiles == null || Tile.tiles.Count <= 0)
            Tile.InitializeTileDictionary();

        CurrentTile = 32;

        Sprites = TileMap.LoadImageFromStreamingAssets(10, 12);

        List<string> tileIDs = new List<string>();

        foreach (KeyValuePair<string, Tile_Data> entry in Tile.tiles)
        {
            if (entry.Key.Contains("_Empty"))
                continue;

            tileIDs.Add(entry.Key);
        }

        tileIDs.Sort();
        tileIDs.Reverse();

        foreach (string s in tileIDs)
        {
            GameObject g = Instantiate(buttonPrefab, tileAnchor);
            g.GetComponent<MapCreator_SideButton>().Init(this, Tile.tiles[s].ID, MapCreator_SideButton.MC_ButtonType.Tile);
        }

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = mapSize.y - 1; y >= 0; y--)
            {
                float xPos = 10 + x * tileSize, yPos = 10 + y * tileSize;
                GameObject g = Instantiate(tilePrefab, new Vector3(xPos, yPos, 0), Quaternion.identity);
                g.transform.SetParent(GameObject.Find("All Tiles").transform);

                g.GetComponent<RectTransform>().sizeDelta = new Vector2(tileSize + 0.01f, tileSize + 0.01f);

                g.name = string.Format("({0}, {1})", x, y);
                cells[x, y] = g.GetComponent<MapCreator_Cell>();
                cells[x, y].pos = new Coord(x, y);
            }
        }

        currentSelectedImage.sprite = Sprites[32];
        NewMap();
    }

    public void EnableTooltip(string text)
    {
        Tooltip.gameObject.SetActive(true);
        Tooltip.position = Input.mousePosition;
        Tooltip.GetComponentInChildren<Text>().text = text;
    }

    public void DisableTooltip()
    {
        Tooltip.gameObject.SetActive(false);
    }

    public void Undo()
    {
        if (changes.Count <= 0)
            return;

        ChangeHolder holder = changes[changes.Count - 1];

        foreach (Change c in holder.changes)
        {
            cells[c.pos.x, c.pos.y].SetTile(Sprites[c.previousID], c.previousID);
        }

        if (holder.previousPos != null)
            lastSelected = holder.previousPos;

        changes.RemoveAt(changes.Count - 1);

        Autotile(0, 0, mapSize.x - 1, mapSize.y - 1);
    }

    public void FloodFill(MapCreator_Cell start, int tileToReplace)
    {
        if (start.tileID == CurrentTile)
            return;

        Stack<MapCreator_Cell> stack = new Stack<MapCreator_Cell>();
        ChangeHolder holder = new ChangeHolder();
        stack.Push(start);

        while (stack.Count > 0)
        {
            MapCreator_Cell cell = stack.Pop();

            if (cell.tileID == tileToReplace)
            {
                holder.changes.Add(new Change(cell.pos, cell.tileID));
                cell.SetTile(Sprites[CurrentTile], CurrentTile);

                if (cell.pos.x < mapSize.x - 1)
                    stack.Push(cells[cell.pos.x + 1, cell.pos.y]);
                if (cell.pos.x > 0)
                    stack.Push(cells[cell.pos.x - 1, cell.pos.y]);
                if (cell.pos.y < mapSize.y - 1)
                    stack.Push(cells[cell.pos.x, cell.pos.y + 1]);
                if (cell.pos.y > 0)
                    stack.Push(cells[cell.pos.x, cell.pos.y - 1]);

                Autotile(cell.pos.x, cell.pos.y, 1, 1);
            }
        }

        changes.Add(holder);
    }

    public void MouseOver(Coord c)
    {
        currentMouseOver = c;
        locationText.text = (c == null) ? "" : string.Format("<color=yellow>(</color> {0}<color=yellow>,</color>{1} <color=yellow>)</color>", c.x, c.y);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            int x0 = (lastSelected.x >= currentMouseOver.x) ? currentMouseOver.x : lastSelected.x;
            int y0 = (lastSelected.y >= currentMouseOver.y) ? currentMouseOver.y : lastSelected.y;

            int x1 = (lastSelected.x >= currentMouseOver.x) ? lastSelected.x : currentMouseOver.x;
            int y1 = (lastSelected.y >= currentMouseOver.y) ? lastSelected.y : currentMouseOver.y;

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    cells[x, y].GetComponent<Image>().color = Color.white;
                }
            }

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    cells[x, y].GetComponent<Image>().color = Color.yellow;
                }
            }
        }
    }

    public void FillAllWithCurrent()
    {
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                cells[x, y].OnPress(true, true);
                cells[x, y].EmptySprite();
            }
        }
    }

    public void Changed(int x, int y, bool skipAutoTile = false)
    {
        lastSelected = new Coord(x, y);

        ChangeHolder holder = new ChangeHolder(new List<Change>(), lastSelected);
        holder.changes.Add(new Change(new Coord(x, y), cells[x, y].tileID));
        changes.Add(holder);

        cells[x, y].SetTile(GetSprite(), GetID());

        if (!skipAutoTile)
            Autotile(x, y, 1, 1);
    }

    public void Box(Coord endPos)
    {
        ChangeHolder holder = new ChangeHolder(new List<Change>(), lastSelected);
        int x0 = (lastSelected.x >= endPos.x) ? endPos.x : lastSelected.x;
        int y0 = (lastSelected.y >= endPos.y) ? endPos.y : lastSelected.y;

        int x1 = (lastSelected.x >= endPos.x) ? lastSelected.x : endPos.x;
        int y1 = (lastSelected.y >= endPos.y) ? lastSelected.y : endPos.y;

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                holder.changes.Add(new Change(new Coord(x, y), cells[x, y].tileID));
                cells[x, y].SetTile(Sprites[CurrentTile], CurrentTile);
            }
        }

        changes.Add(holder);
        Autotile(endPos.x, endPos.y, mapSize.x, mapSize.y);
        lastSelected = endPos;
    }

    Tile_Data MapData(int x, int y)
    {
        return Tile.GetByID(cells[x, y].tileID);
    }

    void Autotile(int cx, int cy, int width, int height)
    {
        for (int x = cx - width; x <= cx + width; x++)
        {
            for (int y = cy - width; y <= cy + width; y++)
            {
                if (x == cx && y == cy || x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y || MapData(x, y) == null)
                    continue;

                Tile_Data tile = MapData(x, y);

                if (Tile.isWaterTile(tile.ID, false))
                {
                    int tIndex = (Elevation == 0) ? 0 : 8;
                    BitwiseAutotile(x, y, tIndex, (z => Tile.isWaterTile(z, true)), true);
                }
                else if (tile.HasTag("Liquid") && tile.HasTag("Swamp"))
                {
                    BitwiseAutotile(x, y, 4, (z => Tile.isWaterTile(z, true)), true);
                }
                else if (Tile.isMountain(tile.ID))
                {
                    int tIndex = 9;

                    if (tile == Tile.tiles["Volcano_Wall"])
                        tIndex = 13;

                    BitwiseAutotile(x, y, tIndex, (z => Tile.isMountain(z)), true);

                }
                else if (tile == Tile.tiles["Lava"])
                {
                    int tIndex = 10;
                    BitwiseAutotile(x, y, tIndex, (z => z == Tile.tiles["Lava"].ID), true);

                }
                else if (tile.HasTag("Wall") && MapData(x, y) != Tile.tiles["Door"])
                {
                    int tIndex = 11;
                    bool eightWay = false;

                    if (Tile.IsInTag(tile.ID, "Construct_Ensis"))
                    {
                        tIndex = 12;
                        eightWay = true;
                    }
                    else if (Tile.IsInTag(tile.ID, "Construct_Prison"))
                    {
                        tIndex = 18;
                    }
                    else if (Tile.IsInTag(tile.ID, "Construct_Magna"))
                    {
                        tIndex = 19;
                        eightWay = true;
                    }
                    else if (Tile.IsInTag(tile.ID, "Construct_Facility"))
                    {
                        tIndex = 20;
                    }
                    else if (Tile.IsInTag(tile.ID, "Construct_Kin"))
                    {
                        tIndex = 22;
                        eightWay = true;
                    }

                    BitwiseAutotile(x, y, tIndex, (z => Tile.GetByID(z) != null && Tile.GetByID(z).HasTag("Wall") && !Tile.isMountain(z)), eightWay);
                }
                else if (tile == Tile.tiles["Dream_Floor"])
                {
                    int tIndex = 23;
                    bool eightWay = true;

                    BitwiseAutotile(x, y, tIndex, (z => z == Tile.tiles["Dream_Floor"].ID), eightWay);
                }
            }
        }
    }

    int BitwiseAutotile(int x, int y, int tIndex, System.Predicate<int> p, bool eightWay)
    {
        int sum = 0;

        if (y < mapSize.y - 1 && p(cells[x, y + 1].tileID) && y < mapSize.y - 1)
            sum++;
        if (x > 0 && p(cells[x - 1, y].tileID) && x > 0)
            sum += 2;
        if (x < mapSize.x - 1 && p(cells[x + 1, y].tileID) && x < mapSize.x - 1)
            sum += 4;
        if (y > 0 && p(cells[x, y - 1].tileID) && y > 0)
            sum += 8;

        if (eightWay && sum == 15)
        {
            bool NE = (y < mapSize.y - 1 && x < mapSize.x - 1 && !p(cells[x + 1, y + 1].tileID));
            bool SE = (y > 0 && x < mapSize.x - 1 && !p(cells[x + 1, y - 1].tileID));
            bool SW = (y > 0 && x > 0 && !p(cells[x - 1, y - 1].tileID));
            bool NW = (y < mapSize.y - 1 && x > 0 && !p(cells[x - 1, y + 1].tileID));

            if (NE)
                sum = 16;
            else if (SE)
                sum = 17;
            else if (SW)
                sum = 18;
            else if (NW)
                sum = 19;

            if (NE && SW && !NW && !SE)
                sum = 20;
            else if (NW && SE && !NE && !SW)
                sum = 21;
            else if (NE && NW && !SE && !SW)
                sum = 22;
            else if (SE && SW && !NE && !NW)
                sum = 23;
            else if (NE && SE && !NW && !SW)
                sum = 24;
            else if (NW && SW && !NE && !SE)
                sum = 25;
        }

        cells[x, y].SetTile(tsm.tilesets[tIndex].Autotile[sum], cells[x, y].tileID);
        return sum;
    }

    public Sprite GetSprite()
    {
        return Sprites[CurrentTile];
    }

    public int GetID()
    {
        return CurrentTile;
    }

    public void FillTool()
    {
        selectType = MC_Selection_Type.Fill;
    }

    public void PaintTool()
    {
        selectType = MC_Selection_Type.Paint;
    }

    public void SetCurrentTileID(int i)
    {
        CurrentTile = i;

        if (selectType == MC_Selection_Type.Paint || selectType == MC_Selection_Type.Fill)
            currentSelectedImage.sprite = Sprites[CurrentTile];
        else if (selectType == MC_Selection_Type.Place_Object)
            currentSelectedImage.sprite = objectSprites[CurrentTile];
    }

    public void SaveMap()
    {
        SetName(mapNameInput.text);
        CurrentLocationID = mapTypeSelector.text;
        Elevation = -elevationSelector.value;

        if (MapName == "")
            return;

        MapCreator_Screen sc = new MapCreator_Screen(MapName, Elevation, mapSize, CurrentLocationID);
        int[] ids = new int[mapSize.x * mapSize.y];

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                ids[x * mapSize.y + y] = cells[x, y].tileID;

                if (cells[x, y].objectID >= 0)
                {
                    string t = ItemList.mapObjectBlueprints[cells[x, y].objectID].objectType;
                    sc.objects.Add(new MapCreator_Object(t, new Coord(x, y)));
                }
            }
        }

        sc.IDs = ids;

        JsonData mapJson = JsonMapper.ToJson(sc);
        string path = (Application.isEditor) ? (Application.streamingAssetsPath + TileMap_Data.defaultMapPath + "/" + MapName + ".map") : (Application.persistentDataPath + "/Maps/" + MapName + ".map");

        File.WriteAllText(path, mapJson.ToString());
        mapNameTitle.text = mapNameInput.text + ".map";
        CancelSaveMenu();
    }

    public void MainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void SaveMenuActive()
    {
        Menu.SetActive(true);
        SaveMenu.SetActive(true);
        LoadMenu.SetActive(false);
        selectType = MC_Selection_Type.Save;
    }

    public void LoadMenuActive()
    {
        Menu.SetActive(true);
        SaveMenu.SetActive(false);
        LoadMenu.SetActive(true);

        savedMaps = new List<MapCreator_Screen>();

        for (int i = 0; i < loadButtonAnchor.childCount; i++)
        {
            Destroy(loadButtonAnchor.GetChild(i).gameObject);
        }

        string modPath = (Application.streamingAssetsPath + TileMap_Data.defaultMapPath);
        string[] ss = Directory.GetFiles(modPath, "*.map", SearchOption.AllDirectories);

        GetDataFromDirectory(ss);

        if (Directory.Exists(Application.persistentDataPath + "/Maps"))
        {
            modPath = (Application.persistentDataPath + "/Maps");
            ss = Directory.GetFiles(modPath, "*.map", SearchOption.AllDirectories);
        }
        else
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/Maps");
        }

        GetDataFromDirectory(ss);

        foreach (MapCreator_Screen m in savedMaps)
        {
            GameObject button = Instantiate(loadButtonPrefab, loadButtonAnchor);
            button.GetComponentInChildren<Text>().text = "> " + m.Name + ".map";
            string mapInfo = string.Format(" File: {0}\n\n Location ID: {1}\n\n Elevation: {2}", m.Name, m.locationID, m.elev.ToString());
            button.GetComponent<MapCreator_LoadButton>().Init(this, mapInfo);
            button.GetComponent<Button>().onClick.AddListener(() => { LoadMap(button.transform.GetSiblingIndex()); });
            button.GetComponentsInChildren<Button>()[1].onClick.AddListener(() => { mapToDeleteIndex = button.transform.GetSiblingIndex(); OpenYesNoDialogue(); });
        }

        selectType = MC_Selection_Type.Save;
    }

    void GetDataFromDirectory(string[] ss)
    {
        foreach (string s in ss)
        {
            string jstring = File.ReadAllText(s);
            JsonData d = JsonMapper.ToObject(jstring);

            string name = d["Name"].ToString();
            int elevation = (d.ContainsKey("elev")) ? (int)d["elev"] : 0;
            string landmark = (d.ContainsKey("locationID")) ? d["locationID"].ToString() : "";

            MapCreator_Screen sc = new MapCreator_Screen(name, elevation, mapSize, landmark);

            for (int j = 0; j < d["IDs"].Count; j++)
            {
                sc.IDs[j] = (int)d["IDs"][j];
            }

            if (d.ContainsKey("objects"))
            {
                for (int j = 0; j < d["objects"].Count; j++)
                {
                    string n = d["objects"][j]["Name"].ToString();
                    Coord p = new Coord((int)d["objects"][j]["Pos"][0], (int)d["objects"][j]["Pos"][1]);
                    MapCreator_Object mco = new MapCreator_Object(n, p);
                    sc.objects.Add(mco);
                }
            }

            savedMaps.Add(sc);
        }
    }

    public void NewMap()
    {
        if (changes != null)
            changes.Clear();

        CurrentTile = 32;
        SetPlace(0);
        FillAllWithCurrent();
        CancelSaveMenu();

        if (changes != null)
            changes.Clear();
    }

    void OpenYesNoDialogue()
    {
        yesNoButton.SetActive(true);
    }

    public void CloseYesNoDialogue()
    {
        yesNoButton.SetActive(false);
        mapToDeleteIndex = -1;
    }

    public void DeleteMap()
    {
        int m = mapToDeleteIndex;
        string modPath = (Application.streamingAssetsPath + TileMap_Data.defaultMapPath + "/" + savedMaps[m].Name + ".map");

        if (!File.Exists(modPath))
            modPath = (Application.persistentDataPath + "/Maps/" + savedMaps[m].Name + ".map");

        if (File.Exists(modPath))
        {
            File.Delete(modPath);
            CloseYesNoDialogue();
            LoadMenuActive();
        }
    }

    public void LoadMap(int m)
    {
        MapCreator_Screen screen = savedMaps[m];
        int max = screen.IDs.Length;

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (x * mapSize.y + y >= max)
                    continue;

                int index = screen.IDs[x * mapSize.y + y];
                cells[x, y].SetTile(Sprites[index], index);
                cells[x, y].EmptySprite();
            }
        }

        for (int i = 0; i < savedMaps[m].objects.Count; i++)
        {
            MapCreator_Object mco = savedMaps[m].objects[i];
            cells[mco.Pos.x, mco.Pos.y].SetObject(mco.Name);
        }

        SaveMenuActive();
        mapNameInput.text = screen.Name;
        mapNameTitle.text = screen.Name + ".map";

        mapTypeSelector.transform.parent.gameObject.SetActive(true);

        CurrentLocationID = screen.locationID;
        mapTypeSelector.text = CurrentLocationID;
        elevationSelector.value = (-screen.elev);

        CancelSaveMenu();
        SetPlace(0);

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Autotile(x, y, 1, 1);
            }
        }

        changes.Clear();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (!LoadMenu.activeSelf && !SaveMenu.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SaveMenu.activeSelf)
                    CancelSaveMenu();
                else
                    SaveMenuActive();
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    for (int y = 0; y < mapSize.y; y++)
                    {
                        cells[x, y].GetComponent<Image>().color = Color.white;
                    }
                }

                cells[currentMouseOver.x, currentMouseOver.y].GetComponent<Image>().color = Color.magenta;
            }
        }
    }

    public void SetPlace(int id)
    {
        tileAnchor.gameObject.SetActive(id == 0);
        objectAnchor.gameObject.SetActive(id == 1);
        npcAnchor.gameObject.SetActive(id == 2);

        if (id == 0)
            selectType = MC_Selection_Type.Paint;
        else if (id == 1)
            selectType = MC_Selection_Type.Place_Object;
        else if (id == 2)
            selectType = MC_Selection_Type.Place_NPC;

    }

    public void OpenHelpMenu()
    {
        helpMenu.SetActive(true);
    }

    public void CloseHelpMenu()
    {
        helpMenu.SetActive(false);
    }

    public void CancelSaveMenu()
    {
        CloseHelpMenu();
        SaveMenu.SetActive(false);
        LoadMenu.SetActive(false);
        Menu.SetActive(false);
        selectType = MC_Selection_Type.Paint;
    }

    public void SetName(string name)
    {
        MapName = name;
    }

    public enum MC_Selection_Type
    {
        Paint, Fill, Place_Object, Place_NPC, Save
    }

    public class MapCreator_Screen
    {
        public string Name;
        public int[] IDs;
        public List<MapCreator_Object> objects;
        public int elev;
        public string locationID;

        public MapCreator_Screen(string name, int e, Coord size, string land)
        {
            Name = name;
            IDs = new int[size.x * size.y];
            objects = new List<MapCreator_Object>();
            elev = e;
            locationID = land;
        }
    }

    public class MapCreator_Object
    {
        public string Name;
        public Coord Pos;

        public MapCreator_Object(string n, Coord p)
        {
            Name = n;
            Pos = p;
        }
    }

    class ChangeHolder
    {
        public List<Change> changes;
        public Coord previousPos;

        public ChangeHolder()
        {
            changes = new List<Change>();
        }

        public ChangeHolder(List<Change> c, Coord pp)
        {
            changes = c;
            previousPos = pp;
        }
    }

    class Change
    {
        public Coord pos;
        public int previousID;

        public Change(Coord p, int pID)
        {
            pos = p;
            previousID = pID;
        }
    }
}
