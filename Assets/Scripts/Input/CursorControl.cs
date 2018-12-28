using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CursorControl : MonoBehaviour
{
    public GameObject explosive;
    public GameObject pathObject;

    public Transform parentObject;
    public Sprite[] sprites;
    public bool throwingItem = false;
    public bool blinking = false;
    public bool shooting = false;
    public int range = 45;

    EntitySkills skills;
    Entity playerEntity;
    PlayerInput input;
    bool canSee = false;
    bool canHoldKeys = false;
    bool waitForRefresh = false;
    float moveTimer = 0;

    [HideInInspector] Skill activeSkill;
    List<GameObject> lineObjects = new List<GameObject>();
    int _myPosX, _myPosY;
    ObjectManager objectManager;
    SpriteRenderer spriteRenderer;
    CameraControl camControl;
    MouseController mouseController;

    List<Entity> allTargets;
    public int targetIndex;

    void OnAwake()
    {
        camControl = Camera.main.GetComponent<CameraControl>();
    }

    public int myPosX
    {
        get
        {
            return _myPosX;
        }
        protected set
        {
            _myPosX = value;
            _myPosX = Mathf.Clamp(_myPosX, 0, Manager.localMapSize.x - 1);
            OnPositionChange();
        }
    }
    public int myPosY
    {
        get
        {
            return _myPosY;
        }
        protected set
        {
            _myPosY = value;
            _myPosY = Mathf.Clamp(_myPosY, 0, Manager.localMapSize.y - 1);
            OnPositionChange();
        }
    }

    public Coord myPos
    {
        get { return new Coord(myPosX, myPosY); }
    }

    void Start()
    {
        Reset();

        objectManager = World.objectManager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerEntity = ObjectManager.playerEntity;
        skills = playerEntity.gameObject.GetComponent<EntitySkills>();
        input = playerEntity.gameObject.GetComponent<PlayerInput>();
        mouseController = Camera.main.GetComponent<MouseController>();
        spriteRenderer.enabled = false;
        this.enabled = false;
    }

    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = true;

        if (input != null && input.activeSkill != null)
            activeSkill = input.activeSkill;
    }

    void OnDisable()
    {
        Reset();
        ClearUIObjects();
        World.userInterface.LookTooltipOff();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.enabled = false;
        blinking = false;
        throwingItem = false;
        shooting = false;
    }

    void Update()
    {
        if (GameSettings.UseMouse)
        {
            mouseController.CursorIsActive = false;

            if (input.cursorMode == PlayerInput.CursorMode.Tile)
            {
                myPosX = (int)mouseController.cursorPosition.x;
                myPosY = (int)mouseController.cursorPosition.y + Manager.localMapSize.y;
            }
        }

        KeyInput();

        if (playerEntity.inSight(myPos) && Vector2.Distance(playerEntity.transform.position, transform.position) <= range && World.tileMap.WalkableTile(myPosX, myPosY))
        {
            int s = (activeSkill != null && activeSkill.HasTag(AbilityTags.Small_Square) && activeSkill.castType == CastType.Target) ? 2 : 0;
            spriteRenderer.sprite = sprites[s];
            canSee = true;
        }
        else
        {
            spriteRenderer.sprite = sprites[1];
            canSee = false;
        }

        SetRange();
        transform.position = new Vector3(myPosX + 0.5f, myPosY + 0.5f - Manager.localMapSize.y, 0);
    }

    void SetRange()
    {
        if (!throwingItem && !blinking && !shooting && activeSkill == null)
        {
            range = 55;
        }
        else
        {
            if (throwingItem)
                range = playerEntity.stats.Strength * 2;
            else if (blinking)
                range = playerEntity.sightRange;
            else if (shooting)
                range = (playerEntity.stats.FirearmRange * 2);
            else if (activeSkill != null)
                range = activeSkill.range;
            else
                range = 55;
        }
    }

    public void Reset()
    {
        if (playerEntity != null)
        {
            myPosX = playerEntity.posX;
            myPosY = playerEntity.posY;
        }

        transform.localPosition = Vector3.zero;
    }

    public void FindClosestEnemy(int offset)
    {
        if (playerEntity.fighter.lastTarget != null)
        {
            myPosX = playerEntity.fighter.lastTarget.posX;
            myPosY = playerEntity.fighter.lastTarget.posY;
            return;
        }

        FindInSightTargets();

        if (allTargets.Count == 0)
        {
            Reset();
            targetIndex = 0;
            return;
        }

        targetIndex = offset;

        if (targetIndex >= allTargets.Count)
            targetIndex = 0;

        allTargets = allTargets.OrderBy(x => playerEntity.myPos.DistanceTo(x.myPos)).ToList();

        Entity closest = allTargets[targetIndex];

        if (closest == null)
        {
            Reset();
        }
        else
        {
            myPosX = closest.posX;
            myPosY = closest.posY;
        }
    }

    void FindInSightTargets()
    {
        allTargets = objectManager.onScreenNPCObjects.FindAll(x => x.AI.isHostile && x.AI.InSightOfPlayer());
    }

    void KeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            FindClosestEnemy(targetIndex + 1);
        }

        if (!canHoldKeys)
        {
            if (input.keybindings.GetKey("North"))
            {
                myPosY++;
            }
            else if (input.keybindings.GetKey("East"))
            {
                myPosX++;
            }
            else if (input.keybindings.GetKey("South"))
            {
                myPosY--;
            }
            else if (input.keybindings.GetKey("West"))
            {
                myPosX--;
            }
            else if (input.keybindings.GetKey("NorthEast"))
            {
                myPosX++;
                myPosY++;
            }
            else if (input.keybindings.GetKey("SouthEast"))
            {
                myPosX++;
                myPosY--;
            }
            else if (input.keybindings.GetKey("SouthWest"))
            {
                myPosX--;
                myPosY--;
            }
            else if (input.keybindings.GetKey("NorthWest"))
            {
                myPosX--;
                myPosY++;
            }
        }
        else if (!waitForRefresh)
        {
            float waitTime = 0.1f;

            if (input.keybindings.GetKey("North", KeyPress.Held))
            { //N
                myPosY++;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("East", KeyPress.Held))
            { //E
                myPosX++;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("South", KeyPress.Held))
            { //S
                myPosY--;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("West", KeyPress.Held))
            { //W
                myPosX--;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }

            else if (input.keybindings.GetKey("NorthEast", KeyPress.Held))
            { //NE
                myPosX++;
                myPosY++;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("SouthEast", KeyPress.Held))
            { //SE
                myPosX++;
                myPosY--;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("SouthWest", KeyPress.Held))
            { //SW
                myPosX--;
                myPosY--;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
            else if (input.keybindings.GetKey("NorthWest", KeyPress.Held))
            { //NW
                myPosX--;
                myPosY++;
                waitForRefresh = true;
                Invoke("Refresh", waitTime);
            }
        }

        CheckTile();

        //Select a tile to affect in various ways 
        //Firing, throwing, using skill. 
        if (canSee)
        {
            if (input.keybindings.GetKey("Fire") || input.keybindings.GetKey("Enter") ||
                input.keybindings.GetKey("Interact") || (Input.GetMouseButtonUp(0) && GameSettings.UseMouse))
            {
                SelectTilePressed();
            }
        }

        if (input.AnyInput())
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= 0.3f)
            {
                canHoldKeys = true;
            }
        }
        else if (input.AnyInputUp())
        {
            canHoldKeys = false;
            moveTimer = 0;
        }
    }

    void SelectTilePressed()
    {
        if (!World.tileMap.WalkableTile(myPosX, myPosY))
        {
            return;
        }

        if (throwingItem)
        {
            GameObject ex = Instantiate(explosive, playerEntity.transform.position, Quaternion.identity);
            Explosive exScript = ex.GetComponent<Explosive>();
            exScript.localPosition = new Coord(myPosX, myPosY);

            playerEntity.fighter.ThrowItem(new Coord(myPosX, myPosY), exScript);
            input.CheckFacingDirection(myPosX);

            throwingItem = false;
            input.CancelLook();
        }
        else if (activeSkill != null)
        {
            if (World.tileMap.GetCellAt(new Coord(_myPosX, _myPosY)).mapObjects.Find(x => x.isDoor_Closed) != null)
                return;

            activeSkill.ActivateCoordinateSkill(skills, myPos);
            input.activeSkill = null;
            activeSkill = null;

            input.CheckFacingDirection(myPosX);
            input.CancelLook();
        }
        else if (!throwingItem)
        {
            if (playerEntity.inventory.firearm.HasProp(ItemProperty.Ranged))
            {
                if (myPos == playerEntity.myPos)
                    return;

                Item fa = playerEntity.inventory.firearm;
                
                playerEntity.ShootAtTile(myPosX, myPosY);
                input.CheckFacingDirection(myPosX);
                input.CancelLook();

                //Give commands
            }
            else if (World.tileMap.GetCellAt(myPosX, myPosY).entity != null)
            {
                if (World.objectManager.NumFollowers() > 0)
                {
                    for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
                    {
                        BaseAI bai = World.objectManager.onScreenNPCObjects[i].AI;

                        if (bai.isFollower())
                            bai.ForceTarget(World.tileMap.GetCellAt(myPosX, myPosY).entity);
                    }

                    CombatLog.SimpleMessage("Message_SetTarget");
                }
            }
        }
    }

    void OnPositionChange()
    {
        ClearUIObjects();

        if (camControl == null && Camera.main != null)
            camControl = Camera.main.GetComponent<CameraControl>();

        Coord newPos = myPos;
        Line line = new Line(playerEntity.myPos, newPos);
        List<Coord> points = line.GetPoints();

        for (int i = 0; i < points.Count; i++)
        {
            GameObject g = SimplePool.Spawn(pathObject, new Vector2(points[i].x, points[i].y - Manager.localMapSize.y));
            lineObjects.Add(g);
        }

        if (!GameSettings.UseMouse)
            camControl.SetTargetTransform(transform);
    }

    void ClearUIObjects()
    {
        for (int i = 0; i < lineObjects.Count; i++)
        {
            SimplePool.Despawn(lineObjects[i].gameObject);
        }

        lineObjects.Clear();
    }

    void Refresh()
    {
        waitForRefresh = false;
    }

    void CheckTile()
    {
        World.userInterface.LookTooltipOff();
        List<Entity> npcs = objectManager.onScreenNPCObjects;

        for (int i = 0; i < npcs.Count; i++)
        {
            Entity npcEntity = npcs[i];

            if (npcEntity.myPos == myPos && canSee)
            {
                World.userInterface.LookToolipOn(transform, npcEntity.AI);
                return;
            }
        }

        List<GameObject> mapobs = objectManager.onScreenMapObjects;

        for (int i = 0; i < mapobs.Count; i++)
        {
            MapObjectSprite mos = mapobs[i].GetComponent<MapObjectSprite>();

            if (mos.localPos == myPos && canSee && mos.objectType != "Bloodstain" && mos.objectType != "Bloodstain_Wall" && mos.Description != "")
            {
                World.userInterface.LookToolipOn(transform, mos);
                return;
            }
        }
    }
}
