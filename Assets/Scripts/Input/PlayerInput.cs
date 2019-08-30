using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

[MoonSharp.Interpreter.MoonSharpUserData]
public class PlayerInput : MonoBehaviour
{
    public static bool fullMap = false;
    public static bool lockInput = false;

    public GameObject mapPointer;
    public GameObject directionSelectObject;
    public SpriteRenderer childSprite;
    public Path_AStar localPath;

    Coord storedTravelPos;
    bool waitForRefresh, canHoldKeys, fireWeapon, walking;
    Entity entity;
    GameObject mapPositionPointer;
    MiniMap miniMap;
    CursorControl cursorControlScript;
    float moveTimer = 0;
    GameObject sidePanelUI;
    Vector3 mapOffset = Vector3.zero;
    MapPointer questPointer;
    List<NPC> npcsToMove;
    Path_AStar worldPath;

    public Ability activeSkill;
    public CursorMode cursorMode = CursorMode.None;
    public GameObject restingIcon;
    public InputKeys keybindings;
    public bool showMinimap = true;

    void Start()
    {
        World.playerInput = this;
        Initialize();

        if (questPointer == null)
        {
            questPointer = GameObject.FindObjectOfType<MapPointer>();
        }

        questPointer.OnChangeWorldMapPosition();
        World.tileMap.UpdateMapFeatures();
    }

    void SetStoredTravelPosition()
    {
        if (storedTravelPos == null)
        {
            storedTravelPos = new Coord(World.tileMap.worldCoordX, World.tileMap.worldCoordY);
        }
        else
        {
            storedTravelPos.x = World.tileMap.worldCoordX;
            storedTravelPos.y = World.tileMap.worldCoordY;
        }
    }

    void FlipX(bool flip)
    {
        childSprite.flipX = flip;
    }

    public void ChangeCursorMode(CursorMode cm)
    {
        cursorMode = cm;
        directionSelectObject.SetActive(cursorMode == CursorMode.Direction || KeyHeld("AlternateAttack"));
        cursorControlScript.enabled = (cursorMode == CursorMode.Tile);

        if (!cursorControlScript.enabled)
        {
            Camera.main.GetComponent<CameraControl>().SetTargetTransform(entity.transform);
        }
    }

    Vector3 PointerPos
    {
        get
        {
            mapOffset.x = World.tileMap.worldCoordX + WorldMap.offset.x;
            mapOffset.y = World.tileMap.worldCoordY + WorldMap.offset.y;
            return mapOffset;
        }
    }

    public IEnumerator FollowPath()
    {
        if (worldPath.result == PathResult.Fail)
        {
            yield break;
        }

        int moveCount = worldPath.StepCount;

        for (int i = 0; i < moveCount; i++)
        {
            if (worldPath == null)
            {
                break;
            }

            Coord next = worldPath.GetNextStep();

            if (next == null)
            {
                yield break;
            }

            int wx = next.x - World.tileMap.worldCoordX;
            int wy = next.y - World.tileMap.worldCoordY;

            Action(wx, wy);
            yield return new WaitForSeconds(0.025f);
        }

        worldPath = null;
    }

    public void SetWorldPath(Coord targetPos)
    {
        if (World.tileMap.WalkableWorldTile(targetPos.x, targetPos.y))
        {
            Path_AStar path = new Path_AStar(World.tileMap.WorldPosition, targetPos, World.tileMap.worldMap);

            if (path.Traversable)
            {
                worldPath = path;
            }

            StartCoroutine(FollowPath());
        }
    }

    public void CancelWorldPath()
    {
        worldPath = null;
    }

    void Update()
    {
        if (entity.stats.dead || lockInput)
        {
            return;
        }

        //Contextual actions
        if (KeyDown("Contextual Actions"))
        {
            List<ContextualMenu.ContextualAction> co = ContextualMenu.GetActions();

            if (co.Count == 1)
                co[0].myAction();
            else if (co.Count > 0)
                World.userInterface.OpenContextActions(co);
        }

        if (World.userInterface.NoWindowsOpen)
        {
            if (!UserInterface.paused && cursorMode != CursorMode.Tile)
            {
                HandleKeys();
            }
        }
        else
        {
            MenuKeys();
        }

        if (Mathf.Abs(mapPositionPointer.transform.position.x - PointerPos.x) > 6 || Mathf.Abs(mapPositionPointer.transform.position.y - PointerPos.y) > 6)
            mapPositionPointer.transform.position = PointerPos;
        else
            mapPositionPointer.transform.position = Vector3.Lerp(mapPositionPointer.transform.position, PointerPos, 0.5f);
        World.userInterface.ChangeMapNameInSideBar();

        if (worldPath != null)
        {
            return;
        }

        questPointer.OnChangeWorldMapPosition();

        if (KeyDown("Map") && World.tileMap.currentElevation == 0 && World.userInterface.NoWindowsOpen 
            || fullMap && (KeyDown("GoDownStairs") || KeyDown("Enter") || KeyDown("Pause")))
        {
            TriggerLocalOrWorldMap();
            return;
        }

        if (KeyDown("Pause") && World.userInterface.NoWindowsOpen)
        {
            if (cursorMode == CursorMode.Tile)
                fireWeapon = false;
            else if (cursorMode == CursorMode.Direction)
                walking = false;

            ChangeCursorMode(CursorMode.None);
            return;
        }
        else if (KeyDown("Throw") && World.userInterface.NoWindowsOpen)
        {
            if (entity.inventory.items.Count <= 0 && !entity.inventory.firearm.HasProp(ItemProperty.Throwing_Wep))
            {
                Alert.NewAlert("No_Throw");
            }
            else
            {
                World.userInterface.OpenRelevantWindow(UIWindow.SelectItemToThrow);
            }
            
            return;
        }
        else if (KeyDown("Toggle Mouse"))
        {
            GameSettings.UseMouse = !GameSettings.UseMouse;
            return;
        }
        else if (KeyDown("Toggle Minimap"))
        {
            showMinimap = !showMinimap;
            World.userInterface.ChangeMapNameInSideBar();
        }

        if (AnyInputDown())
        {
            Camera.main.GetComponent<MouseController>().CursorIsActive = false;

            if (entity.resting || entity.canCancelWalk)
            {
                entity.resting = false;
                entity.CancelWalk();
            }

            if (localPath != null)
            {
                localPath = null;
            }
        }
    }

    void MenuKeys()
    {
        if (AnyInput())
        {
            moveTimer += Time.deltaTime;
            canHoldKeys = (moveTimer >= 0.25f);
        }

        if (AnyInputUp())
        {
            moveTimer = 0;
        }

        if (canHoldKeys)
        {
            if (!waitForRefresh)
            {
                if (KeyHeld("North"))
                {
                    HeldKeys_Menu(-1);
                }
                else if (KeyHeld("South"))
                {
                    HeldKeys_Menu(1);
                }
            }
        } else
        {
            if (KeyDown("North"))
            {
                World.userInterface.SwitchSelectedNum(-1);
            }
            else if (KeyDown("South"))
            {
                World.userInterface.SwitchSelectedNum(1);
            }
        }
    }

    void HandleKeys()
    {
        if (canHoldKeys)
        {
            HoldKeys();
        }
        else
        {
            SingleInput();
        }

        AbilityHotkeys();

        if (KeyDown("Look") && !fireWeapon)
        {
            World.userInterface.CloseWindows();
            ChangeCursorMode((cursorMode == CursorMode.Tile) ? CursorMode.None : CursorMode.Tile);
            cursorControlScript.Reset();
        }
        else if (!fullMap && KeyDown("Interact") && World.userInterface.NoWindowsOpen && cursorMode != CursorMode.Direction)
        {
            List<ContextualMenu.ContextualAction> co = ContextualMenu.GetActions();

            if (co.Count == 1)
                co[0].myAction();
            else
                ChangeCursorMode(CursorMode.Direction);
        }
        else if (KeyDown("Pickup"))
        {
            SearchArea(entity.myPos, false);
        }
        else if (KeyDown("Fire"))
        {
            if (entity.inventory.firearm.HasProp(ItemProperty.Ranged))
            {
                ChangeCursorMode(CursorMode.Tile);
                cursorControlScript.range = entity.stats.FirearmRange;
                cursorControlScript.FindClosestEnemy(0);
            }
            else if (entity.inventory.firearm.lootable && entity.inventory.firearm.amount > 0)
            {
                entity.fighter.SelectItemToThrow(entity.inventory.firearm);
                ToggleThrow();
            }

            entity.CancelWalk();
        }
        else if (KeyDown("Rest"))
        {
            if (entity.stats.health < entity.stats.maxHealth || entity.stats.stamina < entity.stats.maxStamina)
            {
                if (World.objectManager.SafeToRest())
                {
                    CombatLog.SimpleMessage("Message_Rest");
                    entity.resting = true;
                }
                else
                {
                    Alert.NewAlert("Cannot_Rest_Enemies");
                }

                entity.CancelWalk();
            }
        }
        else if (KeyDown("Walk") && World.objectManager.SafeToRest())
        {
            ChangeCursorMode(CursorMode.Direction);
            walking = true;
        }
        else if (KeyDown("Reload"))
        {
            entity.ReloadWeapon();
        }
        else if (KeyDown("GoDownStairs"))
        {
            TryChangeElevation(-1);
        }
        else if (KeyDown("GoUpStairs"))
        {
            TryChangeElevation(1);
        }

        if (AnyInput())
        {
            moveTimer += Time.deltaTime;
            canHoldKeys = (moveTimer >= 0.25f);
        }

        if (AnyInputUp())
        {
            moveTimer = 0;
        }
    }

    Coord GetNewLocalPosFromWorldMove(int x, int y)
    {
        Coord c = new Coord(0, 0);

        if (x == 0 && y == 0)
        {
            return new Coord(entity.posX, entity.posY);
        }

        switch (x)
        {
            case -1: c.x = Manager.localMapSize.x - 2;
                break;
            case 0: c.x = Manager.localMapSize.x / 2;
                break;
            case 1: c.x = 1;
                break;
            default: c.x = Manager.localMapSize.x / 2;
                break;
        }

        switch (y)
        {
            case -1: c.y = Manager.localMapSize.y - 2;
                break;
            case 0: c.y = Manager.localMapSize.y / 2;
                break;
            case 1: c.y = 1;
                break;
            default: c.y = Manager.localMapSize.y / 2;
                break;
        }

        return c;
    }

    void SingleInput()
    {
        if (KeyDown("North"))
            Action(0, 1);
        else if (KeyDown("East"))
            Action(1, 0);
        else if (KeyDown("South"))
            Action(0, -1);
        else if (KeyDown("West"))
            Action(-1, 0);
        else if (KeyDown("NorthEast"))
            Action(1, 1);
        else if (KeyDown("SouthEast"))
            Action(1, -1);
        else if (KeyDown("SouthWest"))
            Action(-1, -1);
        else if (KeyDown("NorthWest"))
            Action(-1, 1);
        else if (KeyDown("Wait"))
            Action(0, 0);
    }

    void HoldKeys()
    {
        if (!waitForRefresh)
        {
            if (KeyHeld("North"))
                HeldKeyAction(0, 1);
            else if (KeyHeld("East"))
                HeldKeyAction(1, 0);
            else if (KeyHeld("South"))
                HeldKeyAction(0, -1);
            else if (KeyHeld("West"))
                HeldKeyAction(-1, 0);
            else if (KeyHeld("NorthEast"))
                HeldKeyAction(1, 1);
            else if (KeyHeld("SouthEast"))
                HeldKeyAction(1, -1);
            else if (KeyHeld("SouthWest"))
                HeldKeyAction(-1, -1);
            else if (KeyHeld("NorthWest"))
                HeldKeyAction(-1, 1);
            else if (KeyHeld("Wait"))
                HeldKeyAction(0, 0);
        }
    }

    void HeldKeys_Menu(int amount)
    {
        float waitTime = 0.1f;

        World.userInterface.SwitchSelectedNum(amount);

        if (canHoldKeys)
        {
            waitForRefresh = true;
            Invoke("Refresh", waitTime);
        }
    }

    public void UseDirectionalSkill(Ability newSkill)
    {
        if (entity.stats.SkipTurn())
        {
            return;
        }

        activeSkill = newSkill;
        ChangeCursorMode(CursorMode.Direction);
    }

    public void UseSelectTileSkill(Ability newSkill)
    {
        if (entity.stats.SkipTurn())
        {
            return;
        }

        activeSkill = newSkill;
        ChangeCursorMode(CursorMode.Tile);
        cursorControlScript.FindClosestEnemy(0);
    }

    void Action(int x, int y)
    {
        if (x != 0)
        {
            FlipX(x < 0);
        }

        if (!fullMap)
        {
            if (cursorMode == CursorMode.Direction)
            {
                SelectDirection(x, y);
                return;
            }

            if (!entity.canAct)
            {
                return;
            }

            if (KeyHeld("AlternateAttack") && !(x == 0 && y == 0))
            {
                if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
                {
                    Cell c = World.tileMap.GetCellAt(entity.posX + x, entity.posY + y);

                    if (c.entity != null)
                    {
                        World.userInterface.CalledShot(c.entity.body);
                        return;
                    }
                }
            }

            if (KeyHeld("GrappleAttack") && !(x == 0 && y == 0))
            {
                if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
                {
                    Cell c = World.tileMap.GetCellAt(entity.posX + x, entity.posY + y);

                    if (c.entity != null)
                    {
                        World.userInterface.OpenGrapple(c.entity.body);
                        return;
                    }
                }
            }

            if (x == 0 && y == 0)
                entity.Wait();
            else
                entity.Action(x, y);

            entity.resting = false;
            entity.CancelWalk();
        }
        else if (World.tileMap.WalkableWorldTile(World.tileMap.worldCoordX + x, World.tileMap.worldCoordY + y))
        {
            Coord newLP = GetNewLocalPosFromWorldMove(x, y);
            SetLocalPosition(newLP.x, newLP.y);
            ChangeWorldPosition(x, y);
            questPointer.OnChangeWorldMapPosition();
        }
    }

    public void CancelLook()
    {
        walking = false;
        activeSkill = null;
        fireWeapon = false;
        ChangeCursorMode(CursorMode.None);
    }

    public void CheckFacingDirection(int otherXPos)
    {
        if (otherXPos != entity.posX)
        {
            FlipX(otherXPos < entity.posX);
        }
    }

    void HeldKeyAction(int x, int y)
    {
        float waitTime = 0.1f;

        if (!fullMap)
        {
            if (!entity.canAct)
            {
                return;
            }
            
            if (x != 0)
                FlipX(x < 0);
            if (x == 0 && y == 0)
                entity.Wait();
            else
                entity.Action(x, y);

            entity.resting = false;
            entity.CancelWalk();
        }
        else if (World.tileMap.WalkableWorldTile(World.tileMap.worldCoordX + x, World.tileMap.worldCoordY + y))
        {
            Coord newLP = GetNewLocalPosFromWorldMove(x, y);
            SetLocalPosition(newLP.x, newLP.y);
            ChangeWorldPosition(x, y);
            questPointer.OnChangeWorldMapPosition();
        }

        if (canHoldKeys)
        {
            waitForRefresh = true;
            Invoke("Refresh", waitTime);
        }
    }

    void AbilityHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && entity.skills.abilities.Count > 0)
        {
            entity.skills.abilities[0].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && entity.skills.abilities.Count > 1)
        {
            entity.skills.abilities[1].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && entity.skills.abilities.Count > 2)
        {
            entity.skills.abilities[2].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && entity.skills.abilities.Count > 3)
        {
            entity.skills.abilities[3].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) && entity.skills.abilities.Count > 4)
        {
            entity.skills.abilities[4].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6) && entity.skills.abilities.Count > 5)
        {
            entity.skills.abilities[5].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7) && entity.skills.abilities.Count > 6)
        {
            entity.skills.abilities[6].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8) && entity.skills.abilities.Count > 8)
        {
            entity.skills.abilities[7].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9) && entity.skills.abilities.Count > 9)
        {
            entity.skills.abilities[8].Cast(entity);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) && entity.skills.abilities.Count > 10)
        {
            entity.skills.abilities[9].Cast(entity);
        }
    }

    void Refresh()
    {
        waitForRefresh = false;
    }

    public void SelectDirection(int x, int y)
    {
        if (DidDirectionalAction(x, y))
        {
            return;
        }

        CancelLook();
        entity.Interact(x, y);
    }

    bool DidDirectionalAction(int x, int y)
    {
        if (activeSkill != null)
        {
            if (!(x == 0 && y == 0))
            {
                activeSkill.ActivateCoordinateSkill(entity.skills, new Coord(x, y));
                CheckFacingDirection(entity.posX + x);
                activeSkill = null;
                CancelLook();
            }

            return true;
        }

        if (walking)
        {
            if (!(x == 0 && y == 0))
            {
                entity.Walk(new Coord(x, y));
                CheckFacingDirection(entity.posX + x);
                walking = false;
                CancelLook();
            }

            return true;
        }

        return false;
    }

    public void ToggleThrow()
    {
        ChangeCursorMode(CursorMode.Tile);
        cursorControlScript.throwingItem = true;
        cursorControlScript.FindClosestEnemy(0);
    }

    public void ToggleBlink()
    {
        ChangeCursorMode(CursorMode.Tile);
        cursorControlScript.blinking = true;
    }

    public void SearchArea(Coord pos, bool passive)
    {
        if (World.tileMap.WalkableTile(pos.x, pos.y))
        {
            Cell c = World.tileMap.GetCellAt(pos);

            if (c.mapObjects.Count > 0)
            {
                foreach (MapObjectSprite m in c.mapObjects)
                {
                    if (passive)
                    {
                        Inventory inv = m.inv;

                        if (inv != null && inv.items.Count > 0)
                        {
                            AutoPickup(inv);
                            CombatLog.DisplayItemsBelow(inv);
                        }
                    }
                    else
                    {
                        m.Interact();
                    }
                }

                World.objectManager.CheckMapObjectInventories();
            }
        }
    }

    void AutoPickup(Inventory inv)
    {
        for (int i = 0; i < inv.items.Count; i++)
        {
            if (entity.inventory.items.Find(x => x.ID == inv.items[i].ID) != null && inv.items[i].stackable || entity.inventory.firearm.ID == inv.items[i].ID && inv.items[i].stackable)
            {
                if (entity.inventory.firearm.ID == inv.items[i].ID)
                {
                    entity.inventory.firearm.amount += inv.items[i].amount;
                    CombatLog.NameMessage("Add_To_Stash", inv.items[i].DisplayName());
                }
                else
                {
                    entity.inventory.PickupItem(inv.items[i]);
                    CombatLog.NameMessage("Add_To_Inventory", inv.items[i].DisplayName());
                }

                inv.RemoveInstance_All(inv.items[i]);
            }
        }
    }

    public void BringNPCs1()
    {
        if (npcsToMove == null)
            npcsToMove = new List<NPC>();
        else
            npcsToMove.Clear();

        if (!fullMap)
        {
            for (int mx = entity.posX - 1; mx <= entity.posX + 1; mx++)
            {
                for (int my = entity.posY - 1; my <= entity.posY + 1; my++)
                {                
                    if (mx == entity.posX && my == entity.posY)
                    {
                        continue;
                    }

                    if (World.tileMap.WalkableTile(mx, my) && World.tileMap.GetCellAt(new Coord(mx, my)).entity != null)
                    {
                        Entity other = World.tileMap.GetCellAt(new Coord(mx, my)).entity;

                        if (other == null || other.AI == null)
                        {
                            continue;
                        }

                        if (other.AI.isHostile && other.AI.HasSeenPlayer() && !other.AI.isStationary && other.CanMove && !other.stats.SkipTurn())
                        {
                            npcsToMove.Add(other.AI.npcBase);
                        }
                    }
                }
            }
        }
    }

    public void BringNPCs2()
    {
        foreach (NPC n in npcsToMove)
        {
            if (entity.GetEmptyCoords().Count > 0)
            {
                n.worldPosition = World.tileMap.WorldPosition;
                n.localPosition = entity.GetEmptyCoords().GetRandom();
                n.elevation = World.tileMap.currentElevation;
                n.hasSeenPlayer = true;
                World.objectManager.SpawnNPC(n);
            }
        }

        World.tileMap.CheckNPCTiles();
        npcsToMove.Clear();
    }

    public void ChangeWorldPosition(int x, int y)
    {
        entity.CancelWalk();

        World.tileMap.worldCoordX += x;
        World.tileMap.worldCoordY += y;

        if (fullMap)
        {
            entity.stats.health = entity.stats.maxHealth;
            entity.stats.stamina = entity.stats.maxStamina;

            int timePass = 20;

            if (x != 0 && y != 0)
                timePass = 20;
            else
                timePass = (y != 0) ? 20 : 30;

            if (entity.inventory.CanFly())
            {
                timePass /= 2;
            }

            if (entity.inventory.OverCapacity())
            {
                timePass += 5;
            }

            for (int i = 0; i < timePass / 2; i++)
            {
                entity.body.TrainLimbOfType(new ItemProperty[] { ItemProperty.Slot_Leg, ItemProperty.Slot_Tail, ItemProperty.Slot_Wing });
            }

            World.turnManager.IncrementTime(timePass);
            World.tileMap.UpdateMapFeatures();

            if (World.tileMap.CurrentMap.mapInfo.biome == WorldMap.Biome.Ocean || World.tileMap.CurrentMap.mapInfo.friendly)
            {
                return;
            }

            //Encounters!
            float encRate = (World.difficulty.Level == Difficulty.DiffLevel.Adventurer) ? 0.8f : 1.0f;

            if (SpawnController.HasFoundEncounter(encRate))
            {
                worldPath = null;
                entity.myPos = World.tileMap.CurrentMap.GetRandomFloorTile();
                entity.ForcePosition();
                TriggerLocalOrWorldMap();
                sidePanelUI.SetActive(!fullMap);

                SetStoredTravelPosition();
                World.tileMap.HardRebuild();
                SpawnController.SetupOverworldEncounter();
            }
        }
    }

    #region Elevation Changes
    public void GoUp()
    {
        ChangeElevation(1);
    }
    public void GoDown()
    {
        ChangeElevation(-1);
    }

    bool TryChangeElevation(int num)
    {
        if (num == 0)
            return false;

        if (num > 0)
        {
            //Up
            if (World.tileMap.GetTileID(entity.posX, entity.posY) != Tile.tiles["Stairs_Up"].ID)
            {
                Coord targetPos = World.tileMap.FindStairsUp();

                if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
                {
                    World.userInterface.YesNoAction("YN_TravelUp".Translate(), () => { FindStairsUp(); }, null, "");
                }

                return false;
            }
        }
        else
        {
            //Down
            if (World.tileMap.GetTileID(entity.posX, entity.posY) != Tile.tiles["Stairs_Down"].ID)
            {
                Coord targetPos = World.tileMap.FindStairsDown();

                if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
                {
                    World.userInterface.YesNoAction("YN_TravelDown".Translate(), () => { FindStairsDown(); }, null, "");
                }

                return false;
            }
        }

        ChangeElevation(num);
        return true;
    }

    public void ChangeElevation(int num)
    {
        BringNPCs1();
        World.tileMap.currentElevation += num;
        World.tileMap.HardRebuild_NoLight();

        Coord newPlayerPos = (num < 0) ? World.tileMap.CurrentMap.StairsUp() : World.tileMap.CurrentMap.StairsDown();
        ObjectManager.playerEntity.ForcePosition(new Coord(newPlayerPos.x, newPlayerPos.y));

        BringNPCs2();
        World.objectManager.NoStickNPCs();
        World.tileMap.HardRebuild();
        
        entity.EndTurn(0.1f, 10);
        CheckMinimap();
        World.userInterface.CloseWindows();
        World.objectManager.CheckFollowers();
    }

    public void CheckMinimap()
    {
        miniMap.Transition(fullMap);
    }

    void FindStairsUp()
    {
        World.userInterface.CloseWindows();
        Coord targetPos = World.tileMap.FindStairsUp();

        if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
        {
            FindStairs(targetPos);
        }
    }

    void FindStairsDown()
    {
        World.userInterface.CloseWindows();
        Coord targetPos = World.tileMap.FindStairsDown();

        if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
        {
            FindStairs(targetPos);
        }
    }

    void FindStairs(Coord targetPos)
    {
        if (targetPos != null && !World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
            return;

        localPath = new Path_AStar(entity.myPos, targetPos, entity.inventory.CanFly(), true);
    }
    #endregion

    void SetLocalPosition(int x, int y)
    {
        entity.posX = x;
        entity.posY = y;

        Camera.main.GetComponent<CameraControl>().SetTargetTransform(entity.transform);
    }

    public void TriggerLocalOrWorldMap()
    {
        World.userInterface.CloseWindows();
        CancelLook();

        if (!fullMap)
        {
            if (!World.objectManager.SafeToRest())
            {
                Alert.NewAlert("Cannot_Travel_Enemies");
                return;
            }

            World.tileMap.UpdateMapFeatures();
            SetStoredTravelPosition();
        }

        fullMap = !fullMap;
        CancelWorldPath();
        World.userInterface.ToggleFullMap(fullMap);
        CheckMinimap();
        sidePanelUI.SetActive(!fullMap);
        World.userInterface.mapFeaturePanel.gameObject.SetActive(fullMap);

        if (!fullMap)
        {
            if (storedTravelPos != World.tileMap.WorldPosition)
            {
                World.tileMap.HardRebuild();
                World.tileMap.CheckNPCTiles();
                entity.ForcePosition();
            }

            World.tileMap.LightCheck();
        }
    }

    void Initialize()
    {
        entity = gameObject.GetComponent<Entity>();
        mapPositionPointer = Instantiate(mapPointer) as GameObject;
        miniMap = mapPositionPointer.GetComponentInChildren<MiniMap>();
        cursorControlScript = gameObject.GetComponentInChildren<CursorControl>();
        SetStoredTravelPosition();
        activeSkill = null;
        sidePanelUI = GameObject.FindObjectOfType<SidePanelUI>().gameObject;

        keybindings = GameSettings.Keybindings;
    }

    #region AnyInput
    public bool AnyInputDown()
    {
        return (KeyDown("North") || KeyDown("South") || KeyDown("East") || KeyDown("West") ||
               KeyDown("NorthEast") || KeyDown("NorthWest") || KeyDown("SouthEast") || KeyDown("SouthWest") ||
               KeyDown("Pause") || KeyDown("Enter") || KeyDown("Interact") || KeyDown("Wait") ||
               KeyDown("Walk"));
    }

    public bool AnyInputUp()
    {
        return (KeyUp("North") || KeyUp("South") || KeyUp("East") || KeyUp("West") ||
               KeyUp("NorthEast") || KeyUp("NorthWest") || KeyUp("SouthEast") || KeyUp("SouthWest")
               || KeyUp("Wait"));
    }

    public bool AnyInput()
    {
        return (KeyHeld("North") || KeyHeld("South") || KeyHeld("East") || KeyHeld("West") ||
               KeyHeld("NorthEast") || KeyHeld("NorthWest") || KeyHeld("SouthEast") || KeyHeld("SouthWest")
               || KeyHeld("Wait"));
    }
    #endregion

    bool KeyDown(string keyName)
    {
        return keybindings.GetKey(keyName);
    }
    bool KeyUp(string keyName)
    {
        return keybindings.GetKey(keyName, KeyPress.Up);
    }
    bool KeyHeld(string keyName)
    {
        return keybindings.GetKey(keyName, KeyPress.Held);
    }

    public void NewKeybindingClass()
    {
        keybindings = GameSettings.Keybindings;
    }

    public enum CursorMode
    {
        None, Direction, Tile
    }
}

