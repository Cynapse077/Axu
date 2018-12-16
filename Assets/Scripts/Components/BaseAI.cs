using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class BaseAI : MonoBehaviour
{
    public NPC npcBase;
    public Coord worldPos;
    public GameObject passiveMarker;
    public SpriteRenderer spriteRenderer;
    public GameObject explosive;
    public Entity entity;
    public EntitySkills entitySkills;
    public Path_AStar path { get; protected set; }

    public Entity target { get; protected set; }
    Body body;
    readonly int sightRange = 17;
    int perception = 10;
    bool hasSeenPlayer = false;
    List<Entity> possibleTargets = new List<Entity>();
    NPCSprite spriteComponent;
    EntitySkills skills;
    bool canAct, hasAskedForHelp = false, doneInit = false;
    readonly float distanceToTarget;
    Coord walkDirection = null;

    System.Random RNG
    {
        get { return SeedManager.combatRandom; }
    }

    public bool isHostile
    {
        get { return npcBase.isHostile || npcBase.hostilityOverride; }
    }

    public bool isStationary
    {
        get { return npcBase.HasFlag(NPC_Flags.Stationary); }
    }

    public void OverrideHostility(bool hostile)
    {
        npcBase.hostilityOverride = hostile;
    }

    public void Init()
    {
        canAct = false;
        CacheVars();
        GrabSkillsFromBase();
        GetComponent<NPCSprite>().SetSprite(npcBase.spriteID);

        if (npcBase.worldPosition.x < 0 || npcBase.worldPosition.y < 0)
            npcBase.worldPosition = worldPos;

        if (ObjectManager.playerEntity != null)
            target = ObjectManager.playerEntity;

        entity.stats.onDeath += OnDeath;
        GetComponent<DialogueController>().SetupDialogueOptions();

        npcBase.hostilityOverride = isHostile;
        npcBase.onScreen = true;

        passiveMarker.SetActive(!npcBase.isHostile && InSightOfPlayer());

        if (!World.tileMap.WalkableTile(entity.myPos))
            entity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());

        doneInit = true;
    }

    public void SetTarget(Entity t)
    {
        if (target == null)
            target = t;
    }

    public void SetPath(Path_AStar newPath)
    {
        path = newPath;
    }

    //FIXME: This is doing more than 1 function.
    bool CanDoAnAction()
    {
        if (entity == null)
            return false;

        if (!doneInit || ObjectManager.playerEntity == null || entity.stats.HasEffect("Blind"))
        {
            Wander();
            return false;
        }

        if (npcBase.faction != null && npcBase.faction.HostileToPlayer())
            npcBase.hostilityOverride = true;

        if (!canAct)
        {
            canAct = true;
            return false;
        }

        if (entity != null)
            entity.canAct = canAct;

        if (isStationary)
        {
            if (isHostile)
                Hostile_Action();

            return false;
        }

        return true;
    }

    void ThrowItem()
    {
        Item i = entity.inventory.items.FindAll(x => x.HasProp(ItemProperty.Throwing_Wep) || x.HasProp(ItemProperty.Explosive)).GetRandom();

        if (i == null)
            return;

        entity.fighter.SelectItemToThrow(i);
        GameObject ex = (GameObject)Instantiate(explosive, target.transform.position, Quaternion.identity);
        Explosive exScript = ex.GetComponent<Explosive>();
        exScript.localPosition = target.myPos;
        entity.fighter.ThrowItem(target.myPos, exScript);

        FaceMe(target.myPos);
    }

    void Update()
    {
        passiveMarker.SetActive(!npcBase.isHostile && InSightOfPlayer());
    }

    public void Decision()
    {
        if (npcBase.worldPosition != World.tileMap.WorldPosition)
        {
            Destroy(gameObject);
            return;
        }

        if (npcBase.HasFlag(NPC_Flags.Quantum_Locked) && InSightOfPlayer())
        {
            hasSeenPlayer = true;

            if (entity != null)
                entity.Wait();

            return;
        }

        if (!CanDoAnAction() || npcBase.HasFlag(NPC_Flags.Inactive))
        {
            if (entity != null)
                entity.Wait();

            return;
        }

        List<BodyPart.Grip> gripsAgainst = body.AllGripsAgainst();

        if (gripsAgainst.Count > 0 && SeedManager.combatRandom.Next(100) < 20)
        {
            Coord pos = entity.GetEmptyCoords().GetRandom();
            entity.Action(pos.x - entity.posX, pos.y - entity.posY);
            return;
        }

        if (isHostile)
        {
            if (hasSeenPlayer)
            {
                int hearing = (npcBase.HasFlag(NPC_Flags.Quantum_Locked)) ? 99 : 91;

                if (!InSightOfPlayer() && RNG.Next(100) + ObjectManager.playerEntity.stats.StealthCheck() / 2 > hearing)
                {
                    hasSeenPlayer = false;
                    Wander();
                }
                else
                {
                    Hostile_Action();
                }
            }
            else
            {
                if (HasDetectedPlayer())
                {
                    NoticePlayer();
                }
                else {
                    Wander();
                }
            }
        }
        else if (isFollower())
        {
            Follower();
        }
        else
        {
            Wander();
        }
    }

    public void NoticePlayer()
    {
        if (spriteRenderer.enabled)
        {
            Transform g = SimplePool.Spawn(World.poolManager.exclamationMark, transform.position + new Vector3(0.5f, 0.5f, 0)).transform;
            g.parent = transform;
            hasSeenPlayer = true;

            if (!hasAskedForHelp && skills.HasAndCanUseSkill("help") && SeedManager.combatRandom.Next(100) < 25)
            {
                skills.CallForHelp();
                hasAskedForHelp = true;
            }

            return;
        }

        entity.Wait();
    }

    public void ForgetPlayer()
    {
        hasAskedForHelp = false;
        hasSeenPlayer = false;
    }

    public bool ShouldAttack(BaseAI other)
    {
        if (other == null)
            return false;

        return (npcBase.faction.isHostileTo(other.npcBase.faction) || isFollower() && other.isHostile || other.isFollower() && isHostile || other.target == entity);
    }

    public bool isFollower()
    {
        return npcBase.HasFlag(NPC_Flags.Follower) || npcBase.faction.ID == "followers";
    }

    bool HasThrowingItem(Entity target)
    {
        return (entity.inventory.items.FindAll(x => x.HasProp(ItemProperty.Throwing_Wep) || x.HasProp(ItemProperty.Explosive)).Count > 0);
    }

    bool HasRanged(Entity target)
    {
        if (entity.inventory.firearm == null)
            entity.inventory.firearm = ItemList.GetNone();

        if (!entity.inventory.firearm.HasProp(ItemProperty.Ranged) || isFollower() && target == ObjectManager.playerEntity)
            return false;

        float dist = GetDistance(target);

        if (RNG.Next(100) > 40 && dist < sightRange)
        {
            RangedAttack(target.myPos);
            return true;
        }

        return false;
    }

    void PeacefulAction()
    {
        if (target == null)
            SearchForTarget();

        if (target == null)
            Wander();
        else
            Hostile_Action();
    }

    void Hostile_Action()
    {
        if (target == null || SeedManager.combatRandom.Next(100) < 20)
            SearchForTarget();

        if (target == null)
        {
            Wander();
            return;
        }

        if (RNG.Next(100) < 35 && UseSkills(target) && TargetInSight())
        {
            entity.Wait();
            return;
        }

        float distance = GetDistance(target);

        if (isStationary && distance > 1.6f)
        {
            if (!HasRanged(target))
            {
                entity.Wait();
                return;
            }
        }

        if (HasThrowingItem(target) && distance < 7 && RNG.Next(100) < 5 && TargetInSight())
        {
            ThrowItem();
            return;
        }
        else if (HasRanged(target))
        {
            return;
        }

        path = new Path_AStar(entity.myPos, target.myPos, entity.inventory.CanFly());
        Coord next = path.GetNextStep();

        if (next.x == entity.posX && next.y == entity.posY)
            next = path.GetNextStep();

        int moveX = next.x - entity.posX, moveY = next.y - entity.posY;

        if (npcBase.HasFlag(NPC_Flags.Aquatic) && !World.tileMap.IsWaterTile(entity.posX + moveX, entity.posY + moveY))
        {
            if (target.myPos != new Coord(entity.posX + moveX, entity.posY + moveY))
            {
                Wander();
                return;
            }
        }

        if (npcBase.HasFlag(NPC_Flags.Hit_And_Run) && RNG.Next(100) < 20)
        {
            moveX *= -1;
            moveY *= -1;
        }

        ConfirmAction(moveX, moveY);
    }

    void SearchForTarget()
    {
        possibleTargets.Refresh();

        if (npcBase.faction.isHostileTo("player") || isHostile)
            possibleTargets.Add(ObjectManager.playerEntity);

        foreach (Entity ent in World.objectManager.onScreenNPCObjects)
        {
            if (ShouldAttack(ent.AI) && entity.inSight(ent.myPos))
                possibleTargets.Add(ent);
        }

        target = (possibleTargets.Count == 0) ? null : GetClosestTarget(possibleTargets);
    }

    void Wander()
    {
        if (entity == null)
        {
            entity = GetComponent<Entity>();
            entity.myPos = new Coord((int)transform.position.x, (int)transform.position.y);
        }

        int posX = entity.posX, posY = entity.posY;
        if (target != null && target != ObjectManager.playerEntity)
        {
            Hostile_Action();
            return;
        }

        if (RNG.Next(100) > 40 || (!isHostile && npcBase.HasFlag(NPC_Flags.Stationary_While_Passive)))
        {
            entity.Wait();
            return;
        }

        if (walkDirection == null || RNG.Next(100) < 20)
            walkDirection = new Coord(RNG.Next(-1, 2), RNG.Next(-1, 2));


        if (walkDirection != null && World.tileMap.WalkableTile(posX + walkDirection.x, posY + walkDirection.y))
        {
            if ((!World.tileMap.GetCellAt(entity.myPos + walkDirection).Walkable) ||
                (npcBase.HasFlag(NPC_Flags.Aquatic) && !World.tileMap.IsWaterTile(entity.posX + walkDirection.x, entity.posY + walkDirection.y)))
            {
                walkDirection = null;

                entity.Wait();
                return;
            }

            ConfirmAction(walkDirection.x, walkDirection.y);
        }
        else
            walkDirection = null;

        if (walkDirection == new Coord(0, 0))
            walkDirection = null;
    }

    public void ForceTarget(Entity newTarget)
    {
        target = newTarget;
    }

    void Follower()
    {
        if (target == null || target == ObjectManager.playerEntity && World.turnManager.turn % 3 == 0)
        {
            List<Entity> possibleTargets = new List<Entity>();

            for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
            {
                Entity ent = World.objectManager.onScreenNPCObjects[i].GetComponent<Entity>();

                if (ent.AI.isHostile)
                    possibleTargets.Add(ent);
            }

            target = (possibleTargets.Count > 0) ? GetClosestTarget(possibleTargets) : ObjectManager.playerEntity;
        }

        if (target == ObjectManager.playerEntity && GetDistance(ObjectManager.playerEntity) < 3 || npcBase.HasFlag(NPC_Flags.At_Home))
        {
            Wander();
            walkDirection = null;
            return;
        }

        if (HasRanged(target))
            return;

        path = new Path_AStar(entity.myPos, target.myPos, entity.inventory.CanFly());
        FollowPath();
    }

    public void FollowPath()
    {
        if (path != null)
        {
            Coord next = path.GetNextStep();

            if (next == entity.myPos)
                next = path.GetNextStep();

            ConfirmAction(next.x - entity.posX, next.y - entity.posY);
        }
    }

    List<Coord> OpenTilesInSameDirection(int x, int y)
    {
        List<Coord> adjacentToOther = new List<Coord>();

        //iterate through the destination tile and find open adjacent tiles.
        for (int tx = x - 1; tx < x + 1; tx++)
        {
            for (int ty = y - 1; ty < y + 1; ty++)
            {
                if (Mathf.Abs(tx) < 2 && Mathf.Abs(ty) < 2)
                {
                    int dx = entity.posX + tx, dy = entity.posY + ty;

                    if (World.tileMap.WalkableTile(dx, dy) && World.tileMap.GetCellAt(new Coord(dx, dy)).Walkable)
                        adjacentToOther.Add(new Coord(tx, ty));
                }
            }
        }

        return adjacentToOther;
    }

    void RangedAttack(Coord targetPosition)
    {
        float dist = GetDistance(target);
        //Fleeing
        if (dist < 3 && RNG.Next(100) < 20 && !isStationary)
        {
            int dirX = entity.posX - targetPosition.x, dirY = entity.posY - targetPosition.y;
            dirX = Mathf.Clamp(dirX, -1, 1);
            dirY = Mathf.Clamp(dirY, -1, 1);
            ConfirmAction(dirX, dirY);
        }
        else
        {
            //Out of sight
            if (!entity.inSight(targetPosition) || dist > 10)
                entity.Wait();
            else
            {
                //Shoot
                if (entity.inventory.firearm.Charges() > 0 || entity.inventory.firearm.HasProp(ItemProperty.Cannot_Remove))
                {
                    entity.ShootAtTile(targetPosition.x, targetPosition.y);
                    FaceMe(target.myPos);
                    //Reload/Do nothing. FIXME: Flee instead of waiting.
                }
                else if (!entity.ReloadWeapon())
                {
                    entity.Wait();
                }
            }
        }
    }

    //Summon slime allies to attack player.
    public void SummonAdd(string groupName)
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        List<Coord> emptyCoords = entity.GetEmptyCoords();

        if (emptyCoords != null && emptyCoords.Count > 0)
            SpawnController.SummonFromGroup(groupName, emptyCoords.GetRandom());
    }

    public bool HasDetectedPlayer(int perceptionBonus = 0)
    {
        if (ObjectManager.playerEntity == null)
            return false;

        float dist = GetDistance(target);
        int playerStealth = ObjectManager.playerEntity.stats.StealthCheck();

        int stealthRoll = RNG.Next(playerStealth);
        int perceptionRoll = RNG.Next(1, perception + perceptionBonus + 1);

        if (isStationary && dist < 1.6f && stealthRoll < perceptionRoll + 2)
            return true;

        int newSightRange = sightRange;

        if (World.tileMap.currentElevation != 0)
            newSightRange -= 4;

        if (dist >= newSightRange / 2)
            perceptionRoll--;

        return (dist < newSightRange && stealthRoll < perceptionRoll);
    }

    public void AnswerCallForHelp(BaseAI bai)
    {
        target = ObjectManager.playerEntity;
        hasAskedForHelp = true;

        if (bai.npcBase.faction == npcBase.faction)
        {
            hasSeenPlayer = true;
            npcBase.hostilityOverride = true;
        }
    }

    public void BecomeHostile()
    {
        hasSeenPlayer = true;
        npcBase.hostilityOverride = true;

        if (isHostile)
        {
            return;
        }

        if (GetComponent<DialogueController>() != null)
            GetComponent<DialogueController>().SetupDialogueOptions();

        if (npcBase.ID == "oromir")
            ObjectManager.playerJournal.AddFlag(ProgressFlags.Hostile_To_Oromir);

        for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
        {
            if (World.objectManager.onScreenNPCObjects[i].AI.npcBase.faction == npcBase.faction)
            {
                if (World.objectManager.onScreenNPCObjects[i].AI.InSightOfPlayer())
                {
                    World.objectManager.onScreenNPCObjects[i].AI.npcBase.questID = "";
                    World.objectManager.onScreenNPCObjects[i].GetComponent<DialogueController>().SetupDialogueOptions();
                }
            }
        }
    }
    
    void ConfirmAction(int x, int y)
    {
        if (entity.posX + x < 0 || entity.posX + x >= Manager.localMapSize.x)
            x = 0;
        if (entity.posY + y < 0 || entity.posY + y >= Manager.localMapSize.y)
            y = 0;

        x = Mathf.Clamp(x, -1, 1);
        y = Mathf.Clamp(y, -1, 1);

        if (Mathf.Abs(x) + Mathf.Abs(y) > 1)
        {
            if (!World.tileMap.WalkableTile(entity.posX + x, entity.posY + y))
            {
                if (!World.tileMap.WalkableTile(entity.posX + x, entity.posY))
                    x = 0;
                if (!World.tileMap.WalkableTile(entity.posX, entity.posY + y))
                    y = 0;
            }
        }

        if (npcBase.HasFlag(NPC_Flags.Quantum_Locked))
        {
            if (World.tileMap.InSightCoords().Contains(new Coord(entity.posX + x, entity.posY + y)))
            {
                entity.Wait();
                return;
            }
        }

        if (x != 0)
            spriteComponent.SetXScale(-x);

        entity.Action(x, y);
        entity.canAct = false;
    }

    float GetDistance(Entity targetEntity)
    {
        if (targetEntity == null || entity == null)
            return Mathf.Infinity;

        return entity.myPos.DistanceTo(targetEntity.myPos);
    }

    Entity GetClosestTarget(List<Entity> entities)
    {
        Entity closest = ObjectManager.playerEntity;
        float distance = Mathf.Infinity;

        foreach (Entity en in entities)
        {
            if (!entity.inSight(en.myPos))
                continue;

            float dist = GetDistance(en);

            if (closest == null)
            {
                distance = dist;
                closest = en;
            }
            else
            {
                if (dist < distance)
                {
                    closest = en;
                    distance = dist;
                }
            }
        }

        if (closest == null)
            return entity;

        return closest;
    }

    public void FaceMe(Coord pos)
    {
        if (pos.x != entity.posX)
            spriteComponent.SetXScale((pos.x > entity.myPos.x) ? -1 : 1);
    }

    void OnDeath(Entity ent)
    {
        if (ObjectManager.playerJournal != null)
            ObjectManager.playerJournal.OnNPCDeath_CheckQuestProgress(npcBase);

        if (npcBase.HasFlag(NPC_Flags.OnDeath_Explode))
        {
            for (int x = entity.posX - 2; x <= entity.posX + 2; x++)
            {
                for (int y = entity.posY - 2; y <= entity.posY + 2; y++)
                {
                    if (x == entity.posX && y == entity.posY)
                        continue;

                    if (x == entity.posX || y == entity.posY && World.tileMap.WalkableTile(x, y))
                        World.objectManager.SpawnExplosion(entity, new Coord(x, y));
                }
            }

            World.soundManager.Explosion();
        }

        if (npcBase.HasFlag(NPC_Flags.OnDeath_PoisonGas))
        {
            for (int x = entity.posX - 1; x <= entity.posX + 1; x++)
            {
                for (int y = entity.posY - 1; y <= entity.posY + 1; y++)
                {
                    if (x == entity.posX || y == entity.posY && World.tileMap.WalkableTile(x, y))
                        World.objectManager.SpawnPoisonGas(entity, new Coord(x, y));
                }
            }
        }
    }

    void CacheVars()
    {
        spriteRenderer.enabled = false;
        body = GetComponent<Body>();
        spriteComponent = GetComponent<NPCSprite>();
        skills = GetComponent<EntitySkills>();
    }

    void GrabSkillsFromBase()
    {
        entity = GetComponent<Entity>();
        entitySkills = GetComponent<EntitySkills>();
        entity.stats.maxHealth = npcBase.maxHealth;
        entity.stats.health = npcBase.health;
        gameObject.name = npcBase.name;
        entity.stats.Attributes["Speed"] = npcBase.Attributes["Speed"];
        perception = npcBase.Attributes["Perception"];
        entity.stats.Attributes["Accuracy"] = npcBase.Attributes["Accuracy"];

        if (npcBase.faction != null && npcBase.faction.HostileToPlayer())
            npcBase.hostilityOverride = true;

        if (!isHostile && npcBase.faction.isHostileTo("player"))
            BecomeHostile();

        if (npcBase.worldPosition.x != World.tileMap.worldCoordX || npcBase.worldPosition.y != World.tileMap.worldCoordY)
        {
            Destroy(gameObject);
            return;
        }

        InSightOfPlayer();
    }

    void OnDisable()
    {
        if (entity != null && entity.stats != null && entity.inventory != null && npcBase != null)
        {
            CacheAll();
            entity.stats.onDeath -= OnDeath;
        }
    }

    public void CacheAll()
    {
        if (entity != null && entity.stats != null && entity.inventory != null && npcBase != null && entity.body != null)
        {
            npcBase.isAlive = (entity.stats.health > 0);
            npcBase.onScreen = false;
            Inventory inv = GetComponent<Inventory>();
            npcBase.localPosition = entity.myPos;
            npcBase.hasSeenPlayer = hasSeenPlayer;
            npcBase.health = (npcBase.HasFlag(NPC_Flags.OnDisable_Regen)) ? entity.stats.maxHealth : entity.stats.health;
            npcBase.stamina = (npcBase.HasFlag(NPC_Flags.OnDisable_Regen)) ? entity.stats.maxStamina : entity.stats.stamina;

            npcBase.inventory = inv.items;
            npcBase.bodyParts = body.bodyParts;

            npcBase.handItems = new List<Item>();
            if (body.Hands.Count > 0)
            {
                for (int i = 0; i < body.Hands.Count; i++)
                {
                    if (body.Hands[i].EquippedItem == null)
                        body.Hands[i].SetEquippedItem(ItemList.GetItemByID(entity.inventory.baseWeapon), entity);

                    npcBase.handItems.Add(body.Hands[i].EquippedItem);
                }
            }
            else
            {
                npcBase.handItems = new List<Item>() { body.defaultHand.EquippedItem };
            }

            npcBase.firearm = inv.firearm;
        }
    }

    public void HireAsFollower()
    {
        if (!isFollower())
        {
            npcBase.MakeFollower();
            GetComponent<DialogueController>().SetupDialogueOptions();
            CombatLog.NameMessage("Message_Hire", gameObject.name);
        }
    }

    public bool HasSeenPlayer()
    {
        return hasSeenPlayer;
    }

    public bool InSightOfPlayer()
    {
        if (entity.cell == null)
            entity.SetCell();

        bool sight = entity.cell.InSight;

        spriteRenderer.enabled = sight;

        return sight;
    }

    public bool TargetInSight()
    {
        if (target == null)
            return false;

        return entity.inSight(target.myPos);
    }

    bool UseSkills(Entity targetEntity)
    {
        if (entity.stats.stamina <= 0 || entity.stats.HasEffect("Stun") || entity.stats.HasEffect("Topple") ||
            entity.stats.Frozen() || entity.stats.HasEffect("Blind"))
            return false;

        if (!InSightOfPlayer())
            return false;

        EntitySkills mySkills = GetComponent<EntitySkills>();
        float distance = GetDistance(targetEntity);
        int randomNumber = RNG.Next(100);

        //Call for help
        if (!hasAskedForHelp && skills.HasAndCanUseSkill("help") && randomNumber < 10)
        {
            mySkills.CallForHelp();
            hasAskedForHelp = true;
            skills.abilities.Find(x => x.ID == "help").InitializeCooldown();
            return true;
        }

        //The End - Spawn tentacle
        if (npcBase.ID == "empty" && RNG.Next(100) < 60 && distance > 1)
        {
            List<Entity> tentacles = World.objectManager.onScreenNPCObjects.FindAll(x => x.AI.npcBase.ID == "theempty-tentacle");

            if (tentacles.Count < 6)
            {
                List<Coord> m = target.GetEmptyCoords(RNG.Next(2, 4));
                List<Coord> lps = new List<Coord>();

                for (int i = 0; i < m.Count; i++)
                {
                    if (World.tileMap.GetTileID(m[i].x, m[i].y) < 2)
                    {
                        lps.Add(m[i]);
                    }
                }

                if (lps.Count > 0)
                {
                    Coord lp = lps.GetRandom(RNG);

                    NPC n = EntityList.GetNPCByID("theempty-tentacle", World.tileMap.WorldPosition, new Coord(lp.x, lp.y));
                    World.objectManager.SpawnNPC(n);
                    return true;
                }
            }
            else
            {
                tentacles.GetRandom(RNG).fighter.Remove();
            }
        }

        //Summon Slimes
        if (npcBase.HasFlag(NPC_Flags.Summon_Adds) && randomNumber < 12 && mySkills.CanUseSkill(2))
        {
            entity.stats.UseStamina(2);
            SummonAdd("Slimeites 1");
            return true;
        }

        //Stream skills
        if (distance >= 3f && randomNumber < 25)
        {
            //Firestream
            if (skills.HasAndCanUseSkill("firestream") && distance <= skills.abilities.Find(x => x.ID == "firestream").range)
            {
                skills.abilities.Find(x => x.ID == "firestream").ActivateCoordinateSkill(skills, target.myPos);
                return true;
            }
            //Ice Beam
            if (skills.HasAndCanUseSkill("icestream") && distance <= skills.abilities.Find(x => x.ID == "icestream").range)
            {
                skills.abilities.Find(x => x.ID == "icestream").ActivateCoordinateSkill(skills, target.myPos);
                return true;
            }
            //Ink spray
            if (skills.HasAndCanUseSkill("ink") && distance <= skills.abilities.Find(x => x.ID == "ink").range)
            {
                skills.abilities.Find(x => x.ID == "ink").ActivateCoordinateSkill(skills, target.myPos);
                return true;
            }
        }


        //Beam
        if ((targetEntity.posX == entity.posX || targetEntity.posY == entity.posY) && randomNumber < 25)
        {
            int dirX = (targetEntity.posX - entity.posX), dirY = (targetEntity.posY - entity.posY);

            if (dirX != 0)
                dirX = (dirX > 0) ? 1 : -1;
            if (dirY != 0)
                dirY = (dirY > 0) ? 1 : -1;

            //Lightning
            if (skills.HasAndCanUseSkill("zapline"))
            {
                skills.abilities.Find(x => x.ID == "zapline").ActivateCoordinateSkill(skills, new Coord(dirX, dirY));
                return true;
            }
        }

        //Grappling
        if (distance < 2f && skills.HasAndCanUseSkill("grapple") && randomNumber < 30)
        {
            if (body.AllGrips() == null || body.AllGrips().Count == 0)
            {
                skills.Grapple_GrabPart(target.GetComponent<Body>().SeverableBodyParts().GetRandom());
                return true;
            }
            else
            {
                if (body.AllGrips()[0].heldPart.slot == ItemProperty.Slot_Head && RNG.Next(100) < 20 && entity.stats.Strength > 6)
                {
                    skills.Grapple_Strangle(target.stats);
                }
                else
                {
                    int ran = RNG.Next(100);

                    if (ran <= 40)
                        skills.Grapple_TakeDown(target.stats, body.AllGrips()[0].heldPart.displayName);
                    else if (ran <= 60)
                        skills.Grapple_Shove(target);
                    else if (ran <= 90 && entity.stats.Strength >= 5)
                        skills.Grapple_Pull(body.AllGrips()[0]);
                }

                return true;
            }
        }

        //Short Range
        if (distance < 2f && randomNumber < 20)
        {
            //Wall Skills
            if (skills.HasAndCanUseSkill("firewall"))
            {
                skills.abilities.Find(x => x.ID == "firewall").ActivateCoordinateSkill(skills, entity.myPos);
                return true;
            }
            //Hallucinogenic Gas
            if (skills.HasAndCanUseSkill("confsquare"))
            {
                skills.abilities.Find(x => x.ID == "confsquare").ActivateCoordinateSkill(skills, entity.myPos);
                return true;
            }

            //Leprosy
            if (npcBase.HasFlag(NPC_Flags.Skills_Leprosy) && randomNumber <= 10)
            {
                CombatLog.CombatMessage("bites", gameObject.name, targetEntity.name, false);

                if (RNG.Next(100) < 5)
                    targetEntity.stats.InitializeNewTrait(TraitList.GetTraitByID("leprosy"));

                entity.fighter.Attack(target.stats, true);
                return true;
            }

            //Short distance directional
            Coord direction = targetEntity.myPos - entity.myPos;
            List<Skill> sk = skills.abilities.FindAll(x => x.castType == CastType.Direction && x.cooldown <= 0 && entity.stats.stamina >= x.staminaCost);

            if (sk.Count > 0)
            {
                Skill s = sk.GetRandom();

                s.ActivateCoordinateSkill(skills, direction);
                return true;
            }
        }

        return false;
    }

    public void OnHitEffects(Stats target, int damage)
    {
        if (target.entity.isPlayer && npcBase.HasFlag(NPC_Flags.Radiation) && SeedManager.combatRandom.Next(100) < 5)
            target.Radiate(SeedManager.combatRandom.Next(1, 6));
    }
}