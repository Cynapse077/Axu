using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class PlayerInput : MonoBehaviour
{
    public static bool fullMap = false;
    public static bool lockInput = false;

    public GameObject mapPointer;
    public GameObject directionSelectObject;
    public SpriteRenderer childSprite;
    public bool grabbing = false;

    Coord storedTravelPos;
    Coord targetPosition;
    bool waitForRefresh, canHoldKeys, fireWeapon, walking;
    bool canWorldMove = true;
    Entity entity;
    Inventory playerInventory;
    GameObject mapPositionPointer;
    MiniMap miniMap;
    CursorControl cursorControlScript;
    EntitySkills skills;
    Stats stats;
    float moveTimer = 0;
    GameObject sidePanelUI;
    Vector3 mapOffset = Vector3.zero;
    MapPointer questPointer;
    List<NPC> npcsToMove;

    Path_AStar worldPath;

    [HideInInspector] public Skill activeSkill;
    [HideInInspector] public bool activeSkillUseStam = true;
    [HideInInspector] public CursorMode cursorMode = CursorMode.None;
    public GameObject restingIcon;
    public InputKeys keybindings;

    void Start()
    {
        World.playerInput = this;
        Initialize();

        if (questPointer == null)
            questPointer = GameObject.FindObjectOfType<MapPointer>();

        questPointer.OnChangeWorldMapPosition();
        World.tileMap.UpdateMapFeatures();
    }

    void SetStoredTravelPosition()
    {
        if (storedTravelPos == null)
            storedTravelPos = new Coord(World.tileMap.worldCoordX, World.tileMap.worldCoordY);

        storedTravelPos.x = World.tileMap.worldCoordX;
        storedTravelPos.y = World.tileMap.worldCoordY;
    }

    void FlipX(bool flip)
    {
        childSprite.flipX = flip;
    }

    public void ChangeCursorMode(CursorMode cm)
    {
        cursorMode = cm;
        directionSelectObject.SetActive(cursorMode == CursorMode.Direction);
        cursorControlScript.enabled = (cursorMode == CursorMode.Tile);

        if (!cursorControlScript.enabled)
            Camera.main.GetComponent<CameraControl>().SetTargetTransform(entity.transform);
    }

    Vector3 PointerPos
    {
        get
        {
            mapOffset.x = World.tileMap.worldCoordX + 50;
            mapOffset.y = World.tileMap.worldCoordY - 200;
            return mapOffset;
        }
    }

    public IEnumerator FollowPath()
    {
        if (worldPath == null)
            yield break;

        canWorldMove = false;

        if (World.tileMap.WorldPosition == targetPosition)
        {
            worldPath = null;
            targetPosition = null;
            canWorldMove = true;
            yield break;
        }

        Coord next = worldPath.GetNextStep();

        if (next == World.tileMap.WorldPosition)
            next = worldPath.GetNextStep();

        ChangeWorldPosition(next.x - World.tileMap.worldCoordX, next.y - World.tileMap.worldCoordY);

        yield return new WaitForSeconds(0.05f);
        canWorldMove = true;
    }

    public void SetWorldPath(Coord targetPos)
    {
        if (World.tileMap.WalkableWorldTile(targetPos.x, targetPos.y))
        {
            targetPosition = targetPos;
            Path_AStar path = new Path_AStar(World.tileMap.WorldPosition, targetPosition, World.tileMap.worldMap);
            worldPath = path;
            canWorldMove = true;
        }
    }

    public void CancelWorldPath()
    {
        worldPath = null;
        canWorldMove = true;
        targetPosition = null;
    }

    void Update()
    {
        if (stats.dead || lockInput)
            return;

        //Contextual actions
        if (KeyDown("Contextual Actions"))
        {
            List<ContextualMenu.ContextualAction> co = ContextualMenu.GetActions();

            if (co.Count == 1)
            {
                co[0].myAction();
            }
            else if (co.Count > 0)
            {
                World.userInterface.OpenContextActions(co);
            }
        }

        if (World.userInterface.canMove)
        {
            if (!UserInterface.paused && cursorMode != CursorMode.Tile)
                HandleKeys();
        }
        else
            MenuKeys();

        if (fullMap && worldPath != null && canWorldMove)
        {
            StartCoroutine("FollowPath");
            return;
        }

        if (Mathf.Abs(mapPositionPointer.transform.position.x - PointerPos.x) > 6 || Mathf.Abs(mapPositionPointer.transform.position.y - PointerPos.y) > 6)
            mapPositionPointer.transform.position = PointerPos;
        else
            mapPositionPointer.transform.position = Vector3.Lerp(mapPositionPointer.transform.position, PointerPos, 0.5f);

        questPointer.OnChangeWorldMapPosition();
        World.userInterface.ChangeMapNameInSideBar();

        if (KeyDown("Map") && World.tileMap.currentElevation == 0 && World.userInterface.canMove)
        {
            TriggerLocalOrWorldMap();
            return;
        }
        else if (fullMap && (KeyDown("GoDownStairs") || KeyDown("Enter") || KeyDown("Pause")))
        {
            TriggerLocalOrWorldMap();
            return;
        }

        if (KeyDown("Pause") && World.userInterface.canMove)
        {
            if (cursorMode == CursorMode.Tile)
                fireWeapon = false;
            else if (cursorMode == CursorMode.Direction)
                walking = false;
            ChangeCursorMode(CursorMode.None);
            return;
        }
        else if (KeyDown("Throw"))
        {
            if (playerInventory.items.Count <= 0 && !playerInventory.firearm.HasProp(ItemProperty.Throwing_Wep))
            {
                Alert.NewAlert("No_Throw");
                return;
            }

            World.userInterface.OpenRelevantWindow(UIWindow.SelectItemToThrow);
            return;
        }
        else if (KeyDown("Toggle Mouse"))
        {
            GameSettings.UseMouse = !GameSettings.UseMouse;
            return;
        }

        if (AnyInputDown())
        {
            Camera.main.GetComponent<MouseController>().CursorIsActive = false;

            if (entity.resting || entity.canCancelWalk)
            {
                entity.resting = false;
                entity.CancelWalk();
            }

            if (entity.path != null)
                entity.path = null;
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
            moveTimer = 0;

        if (canHoldKeys)
        {
            if (!waitForRefresh)
            {
                if (KeyHeld("North"))
                    HeldKeys_Menu(-1);
                else if (KeyHeld("South"))
                    HeldKeys_Menu(1);
            }
        } else
        {
            if (KeyDown("North"))
                World.userInterface.SwitchSelectedNum(-1);
            else if (KeyDown("South"))
                World.userInterface.SwitchSelectedNum(1);
        }
    }

    void HandleKeys()
    {
        if (canHoldKeys)
            HoldKeys();
        else
            SingleInput();

        if (KeyDown("Look") && !fireWeapon)
        {
            World.userInterface.CloseWindows();
            ChangeCursorMode((cursorMode == CursorMode.Tile) ? CursorMode.None : CursorMode.Tile);
            cursorControlScript.Reset();
        }
        else if (KeyDown("Interact") && World.userInterface.canMove && cursorMode != CursorMode.Direction)
        {
            ChangeCursorMode(CursorMode.Direction);
            return;
        }
        else if (KeyDown("Pickup"))
        {
            SearchArea(entity.myPos, false);
        }
        else if (KeyDown("Fire"))
        {
            if (playerInventory.firearm.HasProp(ItemProperty.Ranged))
            {
                ChangeCursorMode(CursorMode.Tile);
                cursorControlScript.range = entity.stats.FirearmRange;
                cursorControlScript.FindClosestEnemy(0);
            }
            else if (playerInventory.firearm.lootable)
            {
                entity.fighter.SelectItemToThrow(playerInventory.firearm);
                ToggleThrow();
            }

            entity.CancelWalk();
        }
        else if (KeyDown("Rest"))
        {
            if (stats.health < stats.maxHealth || stats.stamina < stats.maxStamina)
            {
                if (stats.Hunger <= Globals.Starving)
                {
                    Alert.NewAlert("Cannot_Rest_Enemies");
                    return;
                }
                if (World.objectManager.SafeToRest())
                {
                    CombatLog.SimpleMessage("Message_Rest");
                }
                else
                {
                    Alert.NewAlert("Cannot_Rest_Enemies");
                    return;
                }

                entity.CancelWalk();
                entity.resting = true;
            }
        }
        else if (KeyDown("Walk") && World.objectManager.SafeToRest())
        {
            ChangeCursorMode(CursorMode.Direction);
            walking = true;
        }
        else if (KeyDown("Reload"))
            entity.ReloadWeapon();

        else if (KeyDown("GoDownStairs"))
            TryChangeElevation(-1);

        else if (KeyDown("GoUpStairs"))
            TryChangeElevation(1);

        if (AnyInput())
        {
            moveTimer += Time.deltaTime;
            canHoldKeys = (moveTimer >= 0.25f);
        }
        if (AnyInputUp())
            moveTimer = 0;
    }

    void SingleInput()
    {
        if (KeyDown("North"))
            Action(0, 1, Manager.localMapSize.x / 2, 2);
        else if (KeyDown("East"))
            Action(1, 0, 1, Manager.localMapSize.y / 2);
        else if (KeyDown("South"))
            Action(0, -1, Manager.localMapSize.x / 2, Manager.localMapSize.y - 2);
        else if (KeyDown("West"))
            Action(-1, 0, Manager.localMapSize.x - 2, Manager.localMapSize.y / 2);
        else if (KeyDown("NorthEast"))
            Action(1, 1, 1, 2);
        else if (KeyDown("SouthEast"))
            Action(1, -1, 1, Manager.localMapSize.y - 2);
        else if (KeyDown("SouthWest"))
            Action(-1, -1, Manager.localMapSize.x - 2, Manager.localMapSize.y - 2);
        else if (KeyDown("NorthWest"))
            Action(-1, 1, Manager.localMapSize.x - 2, 2);
        else if (KeyDown("Wait"))
            Action(0, 0, entity.posX, entity.posY);
    }

    void HoldKeys()
    {
        if (!waitForRefresh)
        {
            if (KeyHeld("North"))
                HeldKeyAction(0, 1, Manager.localMapSize.x / 2, 2);
            else if (KeyHeld("East"))
                HeldKeyAction(1, 0, 1, Manager.localMapSize.y / 2);
            else if (KeyHeld("South"))
                HeldKeyAction(0, -1, Manager.localMapSize.x / 2, Manager.localMapSize.y - 2);
            else if (KeyHeld("West"))
                HeldKeyAction(-1, 0, Manager.localMapSize.x - 2, Manager.localMapSize.y / 2);
            else if (KeyHeld("NorthEast"))
                HeldKeyAction(1, 1, 1, 2);
            else if (KeyHeld("SouthEast"))
                HeldKeyAction(1, -1, 1, Manager.localMapSize.y - 2);
            else if (KeyHeld("SouthWest"))
                HeldKeyAction(-1, -1, Manager.localMapSize.x - 2, Manager.localMapSize.y - 2);
            else if (KeyHeld("NorthWest"))
                HeldKeyAction(-1, 1, Manager.localMapSize.x - 2, 2);
            else if (KeyHeld("Wait"))
                HeldKeyAction(0, 0, entity.posX, entity.posY);
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

    public void UseDirectionalSkill(Skill newSkill)
    {
        if (stats.HasEffect("Stun"))
            return;

        activeSkill = newSkill;
        ChangeCursorMode(CursorMode.Direction);
    }

    public void UseSelectTileSkill(Skill newSkill, bool useStam = true)
    {
        if (stats.HasEffect("Stun"))
            return;

        activeSkill = newSkill;
        activeSkillUseStam = useStam;
        ChangeCursorMode(CursorMode.Tile);
        cursorControlScript.FindClosestEnemy(0);
    }

    void Action(int x, int y, int wx, int wy)
    {
        if (x != 0)
            FlipX(x < 0);

        if (!fullMap)
        {
            if (cursorMode == CursorMode.Direction)
            {
                SelectDirection(x, y);
                return;
            }

            if (!entity.canAct)
                return;

            if (KeyHeld("ForceAttack"))
            {
                Attack(x, y);
                return;
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
            SetLocalPosition(wx, wy);
            ChangeWorldPosition(x, y);
            questPointer.OnChangeWorldMapPosition();
        }
    }

    void Attack(int x, int y)
    {
        canHoldKeys = false;

        if (fullMap || !canHoldKeys)
            entity.Swipe(x, y);
    }

    public void CancelLook()
    {
        walking = false;
        activeSkill = null;
        fireWeapon = false;
        grabbing = false;
        ChangeCursorMode(CursorMode.None);
    }

    public void CheckFacingDirection(int otherXPos)
    {
        if (otherXPos != entity.posX)
            FlipX(otherXPos < entity.posX);
    }

    void HeldKeyAction(int x, int y, int wx, int wy)
    {
        float waitTime = 0.1f;

        if (!fullMap)
        {
            if (!entity.canAct)
                return;
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
            SetLocalPosition(wx, wy);
            ChangeWorldPosition(x, y);
            questPointer.OnChangeWorldMapPosition();
        }

        if (canHoldKeys)
        {
            waitForRefresh = true;
            Invoke("Refresh", waitTime);
        }
    }

    void Refresh()
    {
        waitForRefresh = false;
    }

    public void SelectDirection(int x, int y)
    {
        if (DidDirectionalAction(x, y))
            return;

        CancelLook();
        entity.Interact(x, y);
    }

    bool DidDirectionalAction(int x, int y)
    {
        if (activeSkill != null)
        {
            if (!(x == 0 && y == 0))
            {
                activeSkill.ActivateCoordinateSkill(skills, new Coord(x, y));
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

        if (grabbing)
        {
            if (!(x == 0 && y == 0))
            {
                if (World.tileMap.WalkableTile(entity.posX + x, entity.posY + y) && World.tileMap.GetCellAt(entity.posX + x, entity.posY + y).entity != null)
                {
                    World.userInterface.Grab(World.tileMap.GetCellAt(entity.posX + x, entity.posY + y).entity.GetComponent<Body>());
                    grabbing = false;
                    CancelLook();
                }
                else
                {
                    grabbing = false;
                    CancelLook();
                }
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
            if (playerInventory.items.Find(x => x.ID == inv.items[i].ID) != null && inv.items[i].stackable || playerInventory.firearm.ID == inv.items[i].ID && inv.items[i].stackable)
            {
                if (playerInventory.firearm.ID == inv.items[i].ID)
                {
                    playerInventory.firearm.amount += inv.items[i].amount;
                    CombatLog.NameMessage("Add_To_Stash", inv.items[i].DisplayName());
                }
                else
                {
                    playerInventory.PickupItem(inv.items[i]);
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

        if (!fullMap)
        {
            for (int mx = entity.posX - 1; mx <= entity.posX + 1; mx++)
            {
                for (int my = entity.posY - 1; my <= entity.posY + 1; my++)
                {

                    if (mx == entity.posX && my == entity.posY)
                        continue;

                    if (World.tileMap.WalkableTile(mx, my) && World.tileMap.GetCellAt(new Coord(mx, my)).entity != null)
                    {
                        Entity other = World.tileMap.GetCellAt(new Coord(mx, my)).entity;

                        if (other == null || other.AI == null)
                            continue;

                        if (other.AI.isHostile && other.AI.HasSeenPlayer() && !other.AI.isStationary && other.CanMove)
                            npcsToMove.Add(other.AI.npcBase);
                    }
                }
            }
        }
    }

    public void BringNPCs2()
    {
        foreach (NPC n in npcsToMove)
        {
            n.worldPosition = World.tileMap.WorldPosition;
            n.localPosition = entity.GetEmptyCoords().GetRandom();
            n.elevation = World.tileMap.currentElevation;
            n.hasSeenPlayer = true;
        }

        World.tileMap.CheckNPCTiles(false);
        npcsToMove.Clear();
    }

    public void ChangeWorldPosition(int x, int y)
    {
        entity.CancelWalk();

        World.tileMap.worldCoordX += x;
        World.tileMap.worldCoordY += y;

        if (fullMap)
        {
            if (playerInventory.overCapacity())
            {
                Alert.NewAlert("Inv_Full");
                return;
            }
            else if (stats.Hunger < Globals.Starving)
            {
                SetStoredTravelPosition();

                if (fullMap)
                    TriggerLocalOrWorldMap();

                Alert.NewAlert("Starving");
                World.tileMap.HardRebuild();
                World.tileMap.SoftRebuild();
                World.objectManager.NoStickNPCs(entity.posX, entity.posY);

                if (fullMap)
                    TriggerLocalOrWorldMap();

                sidePanelUI.SetActive(!fullMap);
                return;
            }

            stats.health = stats.maxHealth;
            stats.stamina = stats.maxStamina;

            int timePass = 20;

            if (x != 0 && y != 0)
                timePass = 20;
            else
                timePass = (y != 0) ? 20 : 30;

            if (playerInventory.CanFly())
                timePass /= 2;

            for (int i = 0; i < timePass / 2; i++)
            {
                entity.body.TrainLimbOfType(new ItemProperty[] { ItemProperty.Slot_Leg, ItemProperty.Slot_Tail, ItemProperty.Slot_Wing });
            }

            stats.Hunger -= timePass;
            World.turnManager.IncrementTime(timePass);
            World.tileMap.UpdateMapFeatures();

            if (World.tileMap.CurrentMap.mapInfo.biome == WorldMap.Biome.Ocean || World.tileMap.CurrentMap.mapInfo.friendly)
                return;

            //Encounters!
            float encRate = (World.difficulty.Level == Difficulty.DiffLevel.Adventurer || World.difficulty.Level == Difficulty.DiffLevel.Scavenger) ? 0.8f : 1.0f;
            if (SpawnController.HasFoundEncounter(encRate))
            {
                entity.myPos = World.tileMap.CurrentMap.GetRandomFloorTile();
                entity.ForcePosition();
                TriggerLocalOrWorldMap();
                sidePanelUI.SetActive(!fullMap);

                Item item = null;
                int goldAmount = (playerInventory.gold > 0) ? UnityEngine.Random.Range(playerInventory.gold / 2, playerInventory.gold + 1) : 100;

                if (playerInventory.items.Count > 0 && SeedManager.combatRandom.CoinFlip())
                    item = playerInventory.items.GetRandom();

                if (item != null && SeedManager.combatRandom.CoinFlip())
                {
                    World.userInterface.YesNoAction("YN_BanditAmbush_Item", () =>
                    {
                        World.userInterface.BanditYes(goldAmount, item);
                        playerInventory.RemoveInstance_All(item);
                    }, () => World.userInterface.BanditNo(), item.DisplayName());
                }
                else
                {
                    World.userInterface.YesNoAction("YN_BanditAmbush", () =>
                    {
                        World.userInterface.BanditYes(goldAmount, item);
                        playerInventory.gold -= goldAmount;
                    }, () => World.userInterface.BanditNo(), goldAmount.ToString());
                }

                SetStoredTravelPosition();
                World.tileMap.HardRebuild();
                SpawnController.SpawnAttackers();
                World.tileMap.SoftRebuild();
                World.objectManager.NoStickNPCs(entity.posX, entity.posY);
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
            if (World.tileMap.CheckTileID(entity.posX, entity.posY) != Tile.tiles["Stairs_Up"].ID)
            {
                Coord targetPos = World.tileMap.FindStairsUp();

                if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
                    World.userInterface.YesNoAction("YN_TravelUp", () => { FindStairsUp(); }, null, "");

                return false;
            }
        }
        else
        {
            //Down
            if (World.tileMap.CheckTileID(entity.posX, entity.posY) != Tile.tiles["Stairs_Down"].ID)
            {
                Coord targetPos = World.tileMap.FindStairsDown();

                if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
                    World.userInterface.YesNoAction("YN_TravelDown", () => { FindStairsDown(); }, null, "");

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
        World.tileMap.goingDown = num < 0;
        World.tileMap.HardRebuild_NoLight();

        Coord newPlayerPos = (num < 0) ? World.tileMap.CurrentMap.StairsUp() : World.tileMap.CurrentMap.StairsDown();
        ObjectManager.playerEntity.ForcePosition(new Coord(newPlayerPos.x, newPlayerPos.y));

        BringNPCs2();
        World.objectManager.NoStickNPCs(entity.posX, entity.posY);
        World.tileMap.LightCheck(ObjectManager.playerEntity);

        entity.EndTurn(0.1f, entity.GetSpeed());
        CheckMinimap();
        World.userInterface.CloseWindows();
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
            FindStairs(targetPos);
    }

    void FindStairsDown()
    {
        World.userInterface.CloseWindows();
        Coord targetPos = World.tileMap.FindStairsDown();

        if (targetPos != null && World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
            FindStairs(targetPos);
    }

    void FindStairs(Coord targetPos)
    {
        if (targetPos != null && !World.tileMap.CurrentMap.has_seen[targetPos.x, targetPos.y])
            return;

        entity.path = new Path_AStar(entity.myPos, targetPos);
    }
    #endregion

    void SetLocalPosition(int x, int y)
    {
        entity.posX = x;
        entity.posY = y;

        Camera.main.GetComponent<CameraControl>().SetTargetTransform(entity.transform);
    }

    void ResetPath()
    {
        worldPath = null;
        targetPosition = null;
    }

    public void TriggerLocalOrWorldMap()
    {
        World.userInterface.CloseWindows();
        CancelLook();

        if (!fullMap)
        {
            if (stats.Hunger < Globals.Starving)
            {
                Alert.NewAlert("Cannot_Travel_Hunger");
                return;
            }
            else if (!World.objectManager.SafeToRest())
            {
                Alert.NewAlert("Cannot_Travel_Enemies");
                return;
            }
            World.tileMap.UpdateMapFeatures();
            SetStoredTravelPosition();
        }

        fullMap = !fullMap;
        ResetPath();
        World.userInterface.ToggleFullMap(fullMap);
        miniMap.Transition(fullMap);
        sidePanelUI.SetActive(!fullMap);
        World.userInterface.mapFeaturePanel.gameObject.SetActive(fullMap);

        if (!fullMap)
        {
            if (storedTravelPos != World.tileMap.WorldPosition)
                World.tileMap.HardRebuild();

            World.tileMap.CheckNPCTiles(true);
            World.tileMap.LightCheck(GetComponent<Entity>());
            entity.ForcePosition();
        }
    }

    void Initialize()
    {
        entity = gameObject.GetComponent<Entity>();
        playerInventory = gameObject.GetComponent<Inventory>();
        skills = gameObject.GetComponent<EntitySkills>();
        stats = gameObject.GetComponent<Stats>();
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
