using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Entity : MonoBehaviour
{
    public Body body { get; protected set; }
    public Stats stats { get; protected set; }
    public Inventory inventory { get; protected set; }
    public BaseAI AI { get; protected set; }
    public CombatComponent fighter { get; protected set; }
    public EntitySkills skills { get; protected set; }
    public int actionPoints = 10;
    public Cell cell;

    [HideInInspector] public bool isPlayer, canAct, resting, canCancelWalk;
    [HideInInspector] public Coord walkDirection;

    int _posX = 0, _posY = 0;
    PlayerInput playerInput;
    CameraControl cControl;
    SpriteRenderer spriteRenderer;
    Vector3 targetPosition;
    Coord mPos;

    bool swimming
    {
        get
        {
            return (World.tileMap != null && World.tileMap.IsWaterTile(posX, posY) && !inventory.CanFly());
        }
    }

    bool underwater
    {
        get
        {
            return stats != null && stats.HasEffect("Underwater");
        }
    }

    public string Name
    {
        get { return gameObject.name; }
    }
    public string MyName
    {
        get { return Name; }
    }

    public bool Walking
    {
        get { return (isPlayer && walkDirection != null); }
    }

    public int posX
    {
        get { return _posX; }
        set
        {
            value = Mathf.Clamp(value, 0, Manager.localMapSize.x - 1);
            _posX = value;
            targetPosition = new Vector3(_posX, _posY - Manager.localMapSize.y, _posY * 0.01f);

            if (isPlayer)
            {
                SetCamera();
            }
        }
    }

    public int posY
    {
        get { return _posY; }
        set
        {
            value = Mathf.Clamp(value, 0, Manager.localMapSize.y - 1);
            _posY = value;
            targetPosition = new Vector3(_posX, _posY - Manager.localMapSize.y, _posY * 0.01f);
            GetComponentInChildren<SpriteRenderer>().sortingOrder = -_posY;

            if (isPlayer)
            {
                SetCamera();
            }
        }
    }

    public int sightRange
    {
        get
        {
            if (stats != null && stats.HasEffect("Blind"))
            {
                return 0;
            }

            int lgt = 4;

            if (World.turnManager == null)
            {
                return lgt;
            }

            if (World.tileMap.currentElevation < 0)
            {
                Vault v = World.tileMap.GetVaultAt(World.tileMap.WorldPosition);

                if (v != null)
                {
                    lgt = v.blueprint.light;
                }
            }

            int l = (World.tileMap.currentElevation != 0) ? lgt : Manager.localMapSize.x - (int)World.turnManager.VisionInhibit();
            return l + LightBonus();
        }
    }

    public Coord myPos
    {
        get
        {
            if (mPos == null)
            {
                mPos = new Coord(0, 0);
            }

            mPos.x = posX;
            mPos.y = posY;

            return mPos;
        }
        set
        {
            if (value != null)
            {
                posX = value.x;
                posY = value.y;
                targetPosition = new Vector3(_posX, _posY - Manager.localMapSize.y, _posY * 0.01f);
            }
        }
    }

    void SetCamera()
    {
        if (cControl == null)
        {
            cControl = Camera.main.GetComponent<CameraControl>();
        }

        cControl.SetTargetTransform(transform);
    }

    void Start()
    {
        Setup();
    }

    void Setup()
    {
        if (GetComponent<PlayerInput>())
        {
            isPlayer = true;
            World.tileMap.OnScreenChange += OnScreenChange;
            playerInput = gameObject.GetComponent<PlayerInput>();
            CacheVariables();
        }
        else
        {
            isPlayer = false;
            AI = gameObject.GetComponent<BaseAI>();
            AI.entity = this;
            CacheVariables();
            AI.Init(this);
        }

        SetCell();
        ForcePosition();

        if (isPlayer)
        {
            LuaManager.SetGlobals();
            SetCamera();
        }
    }

    public void SetCell()
    {
        Cell c = World.tileMap.GetCellAt(myPos);
        
        if (c != null)
        {
            c.SetEntity(this);
        }
    }

    public void UnSetCell()
    {
        if (cell != null)
        {
            cell.UnSetEntity(this);
        }
    }

    public bool OnScreenChange(TileMap_Data oldMap, TileMap_Data newMap)
    {
        SetCell();
        return true;
    }

    void OnDisable()
    {
        UnSetCell();

        if (isPlayer)
        {
            World.tileMap.OnScreenChange -= OnScreenChange; 
        }
    }

    //Go directly to a tile without lerping
    public void ForcePosition(Coord pos = null)
    {
        if (pos != null)
        {
            UnSetCell();
            myPos = pos;
            SetCell();
        }

        targetPosition = transform.position = new Vector3(_posX, posY - Manager.localMapSize.y, 0);

        if (isPlayer)
        {
            Camera.main.SendMessage("ForcePosition");
        }
    }

    float LerpSpeed
    {
        get
        {
            float lspeed = 0.8f;
            return lspeed * (float)GameSettings.Animation_Speed * 0.01f;
        }
    }

    void Update()
    {
        if (isPlayer)
        {
            ContinuousActions();
        }

        if (transform.position != targetPosition)
        {
            transform.position = (GameSettings.Animation_Speed >= 55) ? targetPosition : Vector3.Lerp(transform.position, targetPosition, LerpSpeed);
        }
    }

    bool FollowersNeedHealing()
    {
        foreach (Entity npc in World.objectManager.onScreenNPCObjects)
        {
            if (npc.AI.isFollower() && !npc.AI.npcBase.HasFlag(NPC_Flags.Deteriortate_HP))
            {
                if (npc.stats.health < npc.stats.maxHealth || npc.stats.stamina < npc.stats.maxStamina)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void ContinuousActions()
    {
        if (playerInput != null)
        {
            playerInput.restingIcon.SetActive(resting);
        }

        if (canAct)
        {            
            if (playerInput.localPath != null) //Pathing
            {
                if (playerInput.localPath.Traversable)
                {
                    Coord c = playerInput.localPath.GetNextStep();

                    if (c.x == posX && c.y == posY && playerInput.localPath.StepCount > 0)
                    {
                        c = playerInput.localPath.GetNextStep();
                    }

                    if (c != null)
                    {
                        int moveX = c.x - posX, moveY = c.y - posY;

                        playerInput.CheckFacingDirection(posX + moveX);
                        Action(moveX, moveY);
                    }
                }

                if (!World.objectManager.SafeToRest())
                {
                    CancelWalk();
                }                
            }
            else if (resting) //Resting
            {
                if (!World.objectManager.SafeToRest())
                {
                    resting = false;
                }

                if (stats.health < stats.maxHealth || stats.stamina < stats.maxStamina || FollowersNeedHealing())
                    Wait();
                else
                    resting = false;
                
            }
            else if (walkDirection != null) //Walking
            {
                if (!World.objectManager.SafeToRest())
                {
                    CancelWalk();
                    return;
                }

                Action(walkDirection.x, walkDirection.y);                
            }
            else //None
            {
                CancelWalk();
            }
        }
    }

    public bool CanMove
    {
        get
        {
            return !stats.SkipTurn();
        }
    }

    public bool Action(int x, int y)
    {
        if (stats.dead || !canAct || UserInterface.paused || !ObjectManager.doneLoading || ObjectManager.player == null)
            return false;

        resting = false;

        if (walkDirection != null && !canCancelWalk)
        {
            canCancelWalk = true;
        }

        if (!CanMove)
        {
            EndTurn(0.3f, 10);
            return true;
        }

        if (isPlayer && World.tileMap.CheckEdgeLocalMap(posX + x, posY + y))
        {
            EndTurn();
            return true;
        }

        if (stats.HasEffect("Confuse") && SeedManager.combatRandom.CoinFlip() || stats.HasEffect("Drunk") && SeedManager.combatRandom.Next(100) < 20)
        {
            x = SeedManager.combatRandom.Next(-1, 2);
            y = SeedManager.combatRandom.Next(-1, 2);
        }

        if (World.tileMap.WalkableTile(posX + x, posY + y))
        {
            Cell targetCell = World.tileMap.GetCellAt(posX + x, posY + y);

            if (targetCell == null)
            {
                return false;
            }

            if (targetCell.entity != null || targetCell.BlocksSpearAttacks())
            {
                if (targetCell.entity != null)
                {
                    if (Walking)
                    {
                        CancelWalk();
                        return true;
                    }

                    return EntityBasedDecision(targetCell, x, y);
                }
                else
                {
                    //Handle solid objects and doors.
                    foreach (MapObjectSprite m in targetCell.mapObjects)
                    {
                        if (isPlayer && m.isDoor_Closed)
                        {
                            if (Walking)
                            {
                                CancelWalk();
                                return true;
                            }

                            OpenDoor(m);
                            return true;
                        }
                        else if (m.objectBase.Solid)
                        {
                            CancelWalk();
                            return true;
                        }
                    }
                }
            }
            else if(inventory.HasSpearEquipped())
            {
                //try other tiles if you have a spear
                const int range = 2;
                int rangeX = x * range;
                int newY = y * range;

                if (!World.tileMap.WalkableTile(posX + rangeX, posY + newY))
                {
                    Move(x, y);
                    return true;
                }

                Cell tCell = World.tileMap.GetCellAt(posX + rangeX, posY + newY);

                if (tCell != null && tCell.entity != null)
                {
                    return EntityBasedDecision(tCell, rangeX, newY, true);
                }
            }

            Move(x, y);

        }
        else if (inventory.DiggingEquipped())
        {
            if (walkDirection != null)
                CancelWalk();
            else
                Dig(x, y);
        }
        else if (inventory.CanFly() && World.tileMap.PassThroughableTile(posX + x, posY + y))
            Move(x, y);
        else
            CancelWalk();

        return true;
    }

    bool EntityBasedDecision(Cell targetCell, int x, int y, bool stopSwap = false)
    {
        Entity otherEntity = targetCell.entity;

        if (otherEntity == null)
        {
            return false;
        }

        if (isPlayer)
        {
            BaseAI bai = otherEntity.AI;

            if (bai != null && bai.isHostile)
            {
                Swipe(x, y);
                return true;
            }
            else if (!stopSwap)
            {
                SwapPosition(new Coord(x, y), otherEntity);
                return true;
            }
            else if (Mathf.Abs(x) == 2 || Mathf.Abs(y) == 2)
            {
                Move(x / 2, y / 2);
                return true;
            }

            return false;
        }
        else
        {
            if (otherEntity.isPlayer && AI.isHostile || AI.ShouldAttack(otherEntity.AI))
            {
                Swipe(x, y);
                return true;
            }
            else if (!otherEntity.isPlayer && AI.npcBase.faction == otherEntity.AI.npcBase.faction && !stopSwap)
            {
                SwapPosition(new Coord(x, y), otherEntity);
                return true;
            }
        }

        if (inventory.HasSpearEquipped() && (Mathf.Abs(x) == 2 || Mathf.Abs(y) == 2))
        {
            if (isPlayer)
            {
                if (otherEntity.AI.isHostile)
                {
                    Swipe(x, y);
                    return true;
                }
            }
            else if (otherEntity.isPlayer && AI.isHostile || AI.ShouldAttack(otherEntity.AI))
            {
                Cell c = World.tileMap.GetCellAt(posX + (x / 2), posY + (y / 2));

                if (c.entity == null || c.entity.isPlayer && AI.isHostile || AI.ShouldAttack(c.entity.AI))
                {
                    Swipe(x, y);
                    return true;
                }
            }
            else
            {
                Move(x / 2, y / 2);               
            }
        }

        return true;
    }

    //Movement
    void Move(int x, int y)
    {
        if (!World.tileMap.WalkableTile(posX + x, posY + y) || !World.tileMap.GetCellAt(posX + x, posY + y).Walkable_IgnoreEntity)
        {
            if (!isPlayer)
            {
                EndTurn(0.01f);
            }

            return;
        }

        if (!isPlayer && AI.isStationary || stats.SkipTurn() || !body.FreeToMove() || stats.HasEffect("Stuck"))
        {
            EndTurn(0.01f);
            return;
        }

        UnSetCell();
        body.CheckGripIntegrities();

        if (World.turnManager.turn % 4 == 0)
        {
            body.TrainLimbOfType(new ItemProperty[] { ItemProperty.Slot_Leg, ItemProperty.Slot_Tail, ItemProperty.Slot_Wing });
        }

        posX += x;
        posY += y;
        SetCell();

        if (GameSettings.Particle_Effects && !ObjectManager.playerEntity.Walking && (isPlayer || spriteRenderer.enabled == true))
        {
            if (World.tileMap.IsWaterTile(_posX, _posY) && !inventory.CanFly())
            {
                World.soundManager.Splash();
                SimplePool.Spawn(World.poolManager.splash, new Vector3(transform.position.x + 0.5f + x, transform.position.y + 0.2f + y, transform.position.z));
            }
        }

        if (isPlayer)
        {
            playerInput.SearchArea(myPos, true);
            World.tileMap.LightCheck();
        }

        EndTurn();
    }

    void Swipe(int x, int y)
    {
        if (stats.SkipTurn() || World.OutOfLocalBounds(posX + x, posY + y))
        {
            return;
        }

        int wepNum = fighter.CheckWepType();

        if (wepNum == 3)
        {
            SweepAttack(x, y);
        }
        else
        {
            int offsetX = Mathf.Clamp(x, -1, 1);
            int offsetY = Mathf.Clamp(y, -1, 1);

            if (World.tileMap.GetCellAt(posX + x, posY + y).InSight)
            {
                GameObject s = SimplePool.Spawn(World.poolManager.slashEffects[wepNum], targetPosition + new Vector3(offsetX, offsetY, 0));
                int playerDir = ((isPlayer) ? playerInput.childSprite.flipX : spriteRenderer.flipX) ? -1 : 1;
                s.GetComponent<WeaponHitEffect>().FaceChildOtherDirection(playerDir, x, y, body.MainHand.EquippedItem);
            }

            if (!AttackTile(posX + x, posY + y))
            {
                if (inventory.HasSpearEquipped())
                {
                    x += x;
                    y += y;

                    if (!AttackTile(posX + x, posY + y))
                    {
                        EndTurn(0.01f, fighter.AttackAPCost());
                    }
                }
            }
        }
    }

    void Dig(int x, int y)
    {
        if (isPlayer && inventory.DiggingEquipped())
        {
            if (inventory.EquippedItems().Find(i => i.HasProp(ItemProperty.Dig)).UseCharge() && World.tileMap.DigTile(posX + x, posY + y, false))
            {
                EndTurn(0.01f, 20);
            }
        }
    }

    /// <summary>
	/// Moved by another character forcefully.
    /// </summary>
    public void ForceMove(int x, int y, int strength)
    {
        if (!isPlayer && AI.isStationary)
        {
            return;
        }

        int amount = (strength - stats.Strength);
        amount = Mathf.Clamp(amount, 0, 3);

        if (amount > 0)
        {
            bool stopMove = false;

            for (int i = 0; i < amount; i++)
            {
                if (stopMove)
                {
                    break;
                }

                if (IsOtherEntityInTheWay(x, y))
                {
                    Entity e = World.tileMap.GetCellAt(posX + x, posY + y).entity;
                    e.stats.AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 3));
                    e.stats.IndirectAttack(SeedManager.combatRandom.Next(1, amount + 2), DamageTypes.Blunt, null, LocalizationManager.GetContent("Impact"), true);
                    stopMove = true;
                    break;
                }
                else if (World.tileMap.WalkableTile(posX + x, posY + y))
                {
                    if (!World.tileMap.GetCellAt(myPos + new Coord(x, y)).Walkable_IgnoreEntity)
                    {
                        stopMove = true;
                        break;
                    }

                    UnSetCell();
                    posX += x;
                    posY += y;
                    SetCell();

                    if (isPlayer)
                    {
                        World.tileMap.LightCheck();
                    }

                    if (i > 0 && stats.HasEffect("OffBalance") && SeedManager.combatRandom.CoinFlip())
                    {
                        stats.AddStatusEffect("Topple", SeedManager.combatRandom.Next(1, 5));
                        break;
                    }
                }
                else
                {
                    stopMove = true;
                    break;
                }
            }

            if (stopMove)
            {
                stats.IndirectAttack(SeedManager.combatRandom.Next(1, amount + 2), DamageTypes.Blunt, null, LocalizationManager.GetContent("Impact"), true);
            }

        }
        else if (SeedManager.combatRandom.Next(100) < 10)
        {
            stats.AddStatusEffect("Stun", SeedManager.combatRandom.Next(1, 3));
        }
    }

    bool IsOtherEntityInTheWay(int x, int y)
    {
        if (!World.tileMap.WalkableTile(posX + x, posY + y))
        {
            return false;
        }

        return (World.tileMap.GetCellAt(posX + x, posY + y).entity != null);
    }

    public void Charge(Coord direction, int amount)
    {
        StartCoroutine("ChargeCo", new object[] { direction, amount });
    }

    //Charge in a direction
    IEnumerator ChargeCo(object[] objs)
    {
        Coord dir = (Coord)objs[0];
        dir.x = Mathf.Clamp(dir.x, -1, 1);
        dir.y = Mathf.Clamp(dir.y, -1, 1);

        int numTiles = (int)objs[1];

        for (int i = 0; i < numTiles; i++)
        {
            if (World.tileMap.WalkableTile(posX + dir.x, posY + dir.y))
            {
                Cell targetCell = World.tileMap.GetCellAt(myPos + dir);

                if (!targetCell.Walkable_IgnoreEntity)
                {
                    break;
                }

                if (targetCell.entity != null)
                {
                    AttackTile(posX + dir.x, posY + dir.y, true);

                    if (targetCell.entity != null)
                    {
                        targetCell.entity.ForceMove(dir.x, dir.y, stats.Strength);
                    }

                    break;
                }
                else
                {
                    UnSetCell();
                    posX += dir.x;
                    posY += dir.y;
                    SetCell();

                    if (i == numTiles - 1)
                    {
                        AttackTile(posX + dir.x, posY + dir.y, true);
                    }
                }
            }
            else
            {
                break;
            }

            if (isPlayer)
            {
                World.tileMap.LightCheck();
            }

            yield return new WaitForSeconds(0.01f);
        }
    }

    public void SwapPosition(Coord direction, Entity otherEntity)
    {
        if (isPlayer && walkDirection != null)
        {
            canCancelWalk = true;
            CancelWalk();
            return;
        }

        direction.x = Mathf.Clamp(direction.x, -1, 1);
        direction.y = Mathf.Clamp(direction.y, -1, 1);

        UnSetCell();
        otherEntity.UnSetCell();

        Coord tempPos = new Coord(myPos.x, myPos.y);
        myPos += direction;
        otherEntity.myPos = tempPos;

        SetCell();
        otherEntity.SetCell();

        if (isPlayer)
        {
            World.tileMap.LightCheck();
            playerInput.SearchArea(myPos, true);
        }
    }

    void OpenDoor(MapObjectSprite door)
    {
        if (walkDirection != null)
        {
            canCancelWalk = true;
            CancelWalk();
            return;
        }

        door.Interact();
    }

    public void Interact(int x, int y)
    {
        if (!World.tileMap.WalkableTile(posX + x, posY + y))
        {
            return;
        }

        Cell targetCell = World.tileMap.GetCellAt(posX + x, posY + y);

        if (targetCell.entity != null || targetCell.mapObjects.Count > 0)
        {
            CancelWalk();
            if (targetCell.entity != null && targetCell.entity != ObjectManager.playerEntity)
            {
                BaseAI bai = targetCell.entity.GetComponent<BaseAI>();

                if (!bai.isHostile && bai.npcBase.HasFlag(NPC_Flags.Can_Speak) && World.objectManager.SafeToRest())
                {
                    World.userInterface.ShowNPCDialogue(targetCell.entity.GetComponent<DialogueController>());
                    bai.FaceMe(myPos);
                    return;
                }
            }
            else if (targetCell.mapObjects.Count > 0)
            {
                for (int i = 0; i < targetCell.mapObjects.Count; i++)
                {
                    MapObjectSprite obj = targetCell.mapObjects[i];

                    if (isPlayer && obj.isDoor_Closed)
                        OpenDoor(obj);
                    else
                        obj.Interact();
                }

                EndTurn(0.05f, 15);
            }
        }
    }

    public bool ReloadWeapon()
    {
        if (!inventory.firearm.HasProp(ItemProperty.Ranged) || inventory.firearm.MagFull())
            return false;

        if (inventory.Reload(inventory.firearm))
        {
            if (isPlayer)
            {
                CombatLog.NewMessage(string.Format(LocalizationManager.GetContent("Message_Reload"), inventory.firearm.DisplayName(), 
                    ItemList.GetItemByID(inventory.firearm.GetCComponent<CFirearm>().currentAmmo).DisplayName()));
            }

            int time = inventory.firearm.HasProp(ItemProperty.Quick_Reload) ? 8 : 14;

            //quiver
            if (inventory.firearm.HasProp(ItemProperty.Bow) && body.GetBodyPartBySlot(ItemProperty.Slot_Back).equippedItem.HasProp(ItemProperty.Quick_Reload))
                time /= 2;

            EndTurn(0.2f, time);
            return true;
        }
        else if (isPlayer)
            Alert.NewAlert("No_Ammo");

        return false;
    }

    public void ShootAtTile(int x, int y)
    {
        if (x == posX && y == posY)
            return;

        if (inventory.firearm.Charges() <= 0)
        {
            if (isPlayer)
                Alert.NewAlert("No_Ammo");

            return;
        }

        StartCoroutine(FireWeapon(x, y));
        EndTurn(0.25f, inventory.firearm.GetAttackAPCost() + stats.AttackDelay);
    }

    IEnumerator FireWeapon(int x, int y)
    {
        if (inventory.firearm.HasProp(ItemProperty.Ranged))
        {
            if (inventory.firearm.HasProp(ItemProperty.Bow))
                World.soundManager.ShootBow();
            else
                World.soundManager.ShootFirearm();
        }

        CombatLog.NameItemMessage("Message_FireWeapon", MyName, inventory.firearm.DisplayName());

        int numShots = inventory.firearm.GetCComponent<CFirearm>().shots;

        for (int i = 0; i < numShots; i++)
        {
            fighter.ShootFireArm(new Coord(x, y), i);

            if (inventory.firearm.HasProp(ItemProperty.Burst) || i == 0)
            {
                inventory.firearm.Fire();
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return null;
    }

    public void BulletTrail(Vector2 start, Vector2 end)
    {
        GameObject bullet = SimplePool.Spawn(World.poolManager.shootEffect, targetPosition);
        LineRenderer lr = bullet.GetComponent<LineRenderer>();
        lr.SetPosition(0, start + new Vector2(0.5f, 0.5f));
        lr.SetPosition(1, end + new Vector2(0.5f, 0.5f));
    }

    public void InstatiateThrowingEffect(Coord destination, float spdMul)
    {
        GameObject lo = Instantiate(World.poolManager.throwEffect, transform.position, Quaternion.identity);
        lo.GetComponent<LerpPos>().Init(destination, spdMul);
    }

    public void Wait()
    {
        EndTurn();
    }

    public void Walk(Coord direction)
    {
        walkDirection = direction;
    }

    public void CancelWalk()
    {
        if (isPlayer)
        {
            walkDirection = null;
            canCancelWalk = false;
            playerInput.localPath = null;
        }
    }

    //Check to see if enities or items are in sight
    public bool inSight(Coord otherPos)
    {
        return inSight(otherPos.x, otherPos.y);
    }

    public bool inSight(int cX, int cY)
    {
        if (!Manager.lightingOn)
        {
            return true;
        }

        if (stats != null && stats.HasEffect("Blind") || myPos.DistanceTo(new Coord(cX, cY)) >= sightRange && !World.tileMap.IsTileLit(cX, cY))
        {
            return false;
        }

        int dx = cX - posX, dy = cY - posY;
        int nx = Mathf.Abs(dx), ny = Mathf.Abs(dy);
        int sign_x = dx > 0 ? 1 : -1, sign_y = dy > 0 ? 1 : -1;
        Coord p = new Coord(posX, posY);

        for (int ix = 0, iy = 0; ix < nx || iy < ny;)
        {
            if (!World.tileMap.LightPassableTile(p.x, p.y) && p != myPos)
                return false;

            float fx = (0.5f + ix) / nx, fy = (0.5f + iy) / ny;

            if (fx == fy)
            {
                p.x += sign_x;
                p.y += sign_y;
                ix++;
                iy++;
            }
            else if (fx < fy)
            {
                p.x += sign_x;
                ix++;
            }
            else
            {
                p.y += sign_y;
                iy++;
            }
        }

        return true;
    }

    public List<Coord> GetEmptyCoords(int radius = 1)
    {
        List<Coord> emptyCoords = new List<Coord>();

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (World.tileMap.WalkableTile(posX + x, posY + y))
                {
                    Cell c = World.tileMap.GetCellAt(posX + x, posY + y);

                    if (c.Walkable)
                    {
                        emptyCoords.Add(new Coord(posX + x, posY + y));
                    }
                }
            }
        }

        return emptyCoords;
    }

    public void RefreshActionPoints()
    {
        actionPoints += GetSpeed();
    }

    public int GetSpeed()
    {
        if (this == null || stats != null && stats.dead || inventory == null)
        {
            return 0;
        }

        if (stats == null)
        {
            stats = GetComponent<Stats>();
            stats.entity = this;
        }

        float spd = (stats.Attributes.ContainsKey("Speed")) ? stats.Speed : 10f;

        if (swimming)
        {
            if (!isPlayer && AI.npcBase.HasFlag(NPC_Flags.Aquatic))
            {
                spd *= 1.0f;
            }
            else
            {
                if (stats.FasterSwimmer())
                {
                    spd *= 1.5f;
                }
                else if (!stats.FastSwimmer())
                {
                    spd *= 0.5f;
                }
            }
        }

        if (stats.HasEffect("Slow"))
        {
            spd -= stats.statusEffects["Slow"];
        }

        if (stats.HasEffect("Topple"))
        {
            spd -= 5;
        }

        spd -= inventory.BurdenPenalty();
        spd = Mathf.Clamp(spd, 1, 50);

        return (int)spd;
    }

    public void EndTurn(float waitTime = 0, int cost = 10)
    {
        canAct = false;
        stats.PostTurn();

        if (isPlayer)
            World.turnManager.EndTurn(waitTime, cost);
        else
            actionPoints -= cost;
    }

    public bool TeleportToSurface()
    {
        if (World.tileMap.currentElevation == 0)
        {
            Alert.NewAlert("On_Surface", UIWindow.Inventory);
            return false;
        }

        World.tileMap.currentElevation = 0;
        World.tileMap.HardRebuild_NoLight();
        myPos = World.tileMap.FindStairsDown();

        ForcePosition();
        World.tileMap.SoftRebuild();
        BeamDown();
        World.playerInput.CheckMinimap();

        return true;
    }

    public void BeamDown()
    {
        Instantiate(World.poolManager.teleBeam, targetPosition, Quaternion.identity);
    }

    public void CreateBloodstain(bool overrideRandom = false, int chance = 6)
    {
        if (!GameSettings.Particle_Effects || SeedManager.combatRandom.Next(100) < chance && !overrideRandom || !isPlayer && AI.npcBase.HasFlag(NPC_Flags.No_Blood))
            return;

        int tNum = World.tileMap.GetTileID(posX, posY);

        if (World.tileMap.IsWaterTile(posX, posY) || TileManager.IsTile(tNum, "Stairs_Up") || TileManager.IsTile(tNum, "Stairs_Down"))
        {
            return;
        }

        World.objectManager.NewObjectOnCurrentScreen("Bloodstain", new Coord(posX, posY));
    }

    public void UnDie()
    {
        canAct = true;
        stats.dead = false;

        Alert.NewAlert("Undie");

        if (World.difficulty.Level == Difficulty.DiffLevel.Scavenger)
        {
            inventory.gold -= (inventory.gold / 10);

            if (SeedManager.combatRandom.Next(100) < 10)
            {
                stats.MyLevel.XP = 0;
            }

            if (SeedManager.combatRandom.Next(100) < 10)
            {
                int ranNum = SeedManager.combatRandom.Next(4);

                if (ranNum == 0 && stats.Strength > 1)
                {
                    stats.ChangeAttribute("Strength", -1);
                    CombatLog.NameMessage("Message_Det_Stat", LocalizationManager.GetContent("Strength"));
                }
                else if (ranNum == 1 && stats.Dexterity > 1)
                {
                    stats.ChangeAttribute("Dexterity", -1);
                    CombatLog.NameMessage("Message_Det_Stat", LocalizationManager.GetContent("Dexterity"));
                }
                else if (ranNum == 2 && stats.Intelligence > 1)
                {
                    stats.ChangeAttribute("Intelligence", -1);
                    CombatLog.NameMessage("Message_Det_Stat", LocalizationManager.GetContent("Intelligence"));
                }
                else if (ranNum == 3 && stats.Endurance > 1)
                {
                    stats.ChangeAttribute("Endurance", -1);
                    CombatLog.NameMessage("Message_Det_Stat", LocalizationManager.GetContent("Endurance"));
                }
            }
        }
    }

    bool AttackTile(int x, int y, bool freeAction = false)
    {
        bool hitAnEnemy = false;

        if (!World.tileMap.WalkableTile(x, y))
        {
            return false;
        }

        Entity e = World.tileMap.GetCellAt(x, y).entity;

        if (e != null)
        {
            if (isPlayer && !e.AI.isHostile && e != fighter.lastTarget)
            {
                World.userInterface.YesNoAction("YN_AttackPassive".Translate(), () =>
                {
                    fighter.Attack(e.stats, freeAction, null);
                    hitAnEnemy = true;
                    World.userInterface.CloseWindows();
                }, null, "");
            }
            else
            {
                fighter.Attack(e.stats, freeAction);
                hitAnEnemy = true;
            }
        }

        return hitAnEnemy;
    }

    void SweepAttack(int x, int y)
    {
        //Diagonals
        if (Mathf.Abs(x) + Mathf.Abs(y) > 1)
        {
            AttackTile(posX + x, posY, true);
            AttackTile(posX, posY + y, true);
            AttackTile(posX + x, posY + y, true);

            Vector3 intPos = targetPosition + new Vector3(0.5f, 0.5f, 0);

            if (x < 0)
                intPos.x += x;
            if (y < 0)
                intPos.y += y;

            GameObject ss = SimplePool.Spawn(World.poolManager.slashEffects[4], intPos);
            ss.GetComponent<WeaponHitEffect>().DiagonalSlashDirection(x, y, body.MainHand.EquippedItem);
        }
        else //Non-diagonals
        {
            for (int p = -1; p <= 1; p++)
            {
                if (x != 0)
                    AttackTile(posX + x, posY + p, true);
                if (y != 0)
                    AttackTile(posX + p, posY + y, true);
            }

            GameObject ss = SimplePool.Spawn(World.poolManager.slashEffects[3], targetPosition + new Vector3(x, y, 0));
            ss.GetComponent<WeaponHitEffect>().FaceChildOtherDirection((Flipped()) ? -1 : 1, x, y, body.MainHand.EquippedItem);
        }

        EndTurn(0.1f, fighter.AttackAPCost());
    }

    bool Flipped()
    {
        if (isPlayer)
            return isPlayer && playerInput.childSprite.flipX;
        else
            return spriteRenderer.flipX;
    }

    //TODO: Cache this.
    public int LightBonus()
    {
        if (inventory == null || stats == null || body.bodyParts == null)
        {
            return 0;
        }

        int lightAmount = 0;
        List<Item> equippedItems = inventory.EquippedItems();

        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i] != null)
            {
                Item eq = equippedItems[i];
                Stat_Modifier sm = eq.statMods.Find(x => x.Stat == "Light");

                if (sm != null)
                {
                    if (eq.HasProp(ItemProperty.Degrade))
                    {
                        if (eq.Charges() > 0)
                        {
                            lightAmount += sm.Amount;
                        }
                    }
                    else
                    {
                        lightAmount += sm.Amount;
                    }
                }
            }
        }

        return Mathf.Clamp(lightAmount + stats.IllumunationCheck(), 1, Manager.localMapSize.x);
    }

    void CacheVariables()
    {
        stats = GetComponent<Stats>();
        body = GetComponent<Body>();
        inventory = GetComponent<Inventory>();
        skills = GetComponent<EntitySkills>();
        body.entity = this;
        inventory.entity = this;
        stats.entity = this;
        stats.Init();
        inventory.Init();
        fighter = new CombatComponent(this);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (!isPlayer)
        {
            GetComponent<DialogueController>().SetupDialogueOptions();
            stats.InitializeNPCTraits(AI.npcBase);
        }
    }

    //Only used for the player's character to be transfered to a writeable string.
    public PlayerCharacter ToCharacter(bool includeInventory = true)
    {
        SStats myStats = stats.ToSimpleStats();

        List<SItem> items = new List<SItem>();

        if (includeInventory)
        {
            for (int i = 0; i < inventory.items.Count; i++)
            {
                items.Add(inventory.items[i].ToSerializedItem());
            }
        }

        List<SBodyPart> bodyParts = new List<SBodyPart>();
        for (int b = 0; b < body.bodyParts.Count; b++)
        {
            SBodyPart sb = body.bodyParts[b].ToSerializedBodyPart();
            
            if (!includeInventory)
            {
                sb.item = null;
            }

            bodyParts.Add(sb);
        }

        List<STrait> traits = new List<STrait>();
        for (int t = 0; t < stats.traits.Count; t++)
        {
            traits.Add(new STrait(stats.traits[t].ID, stats.traits[t].turnAcquired));
        }

        List<SSkill> sskills = new List<SSkill>();
        for (int s = 0; s < skills.abilities.Count; s++)
        {
            sskills.Add(skills.abilities[s].ToSerializedSkill());
        }

        Journal journal = GetComponent<Journal>();

        List<SQuest> quests = new List<SQuest>();
        for (int q = 0; q < journal.quests.Count; q++)
        {
            quests.Add(journal.quests[q].ToSQuest());
        }

        List<SItem> handItems = new List<SItem>();
        for (int i = 0; i < body.Hands.Count; i++)
        {
            SItem handItem = body.Hands[i].EquippedItem.ToSerializedItem();

            if (!includeInventory && body.Hands[i].baseItem != handItem.ID)
            {
                handItem = (GameData.Get<Item>(body.Hands[i].baseItem)).ToSerializedItem();
            }

            handItems.Add(handItem);
        }

        PlayerCharacter me = new PlayerCharacter(Manager.worldSeed, MyName, Manager.profName, stats.MyLevel, myStats,
            World.tileMap.WorldPosition, myPos, World.tileMap.currentElevation, traits, stats.proficiencies.GetProfs(),
            bodyParts, inventory.gold, items, handItems, inventory.firearm.ToSerializedItem(), sskills, stats.Attributes["Charisma"], 
            quests, World.turnManager.currentWeather, ObjectManager.playerJournal.AllFlags());

        return me;
    }
}
