using Axu.Constants;
using UnityEngine;
using Pathfinding;
using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class BaseAI : MonoBehaviour
{
    public NPC npcBase;
    public SpriteRenderer spriteRenderer;
    public GameObject explosive;
    public Entity entity;
    public DialogueController dialogueController;
    public Entity target { get; protected set; }

    readonly int sightRange = 14;

    Path_AStar path;
    float distanceToTarget;
    List<Entity> possibleTargets = new List<Entity>();
    NPCSprite spriteComponent;
    int perception = 10;
    bool canAct, hasAskedForHelp = false, doneInit = false;
    int currentPathFails = 0;
    int turnsLeftToPath = 30;

    public bool isHostile
    {
        get { return npcBase.isHostile || npcBase.hostilityOverride; }
    }

    public bool hasSeenPlayer
    {
        get { return npcBase.hasSeenPlayer; }
        set { npcBase.hasSeenPlayer = value; }
    }

    public bool isStationary
    {
        get { return npcBase.HasFlag(NPC_Flags.Stationary); }
    }

    public void OverrideHostility(bool hostile)
    {
        npcBase.hostilityOverride = hostile;
    }

    public void Init(Entity e)
    {
        entity = e;
        canAct = false;
        CacheVars();
        spriteComponent.SetSprite(npcBase.spriteID);

        if (ObjectManager.playerEntity != null)
        {
            target = ObjectManager.playerEntity;
        }

        entity.stats.onDeath += OnDeath;
        dialogueController = GetComponent<DialogueController>();
        dialogueController.SetupDialogueOptions();
        npcBase.onScreen = true;

        if (!World.tileMap.WalkableTile(entity.myPos))
        {
            entity.ForcePosition(World.tileMap.CurrentMap.GetRandomFloorTile());
        }

        InSightOfPlayer();

        turnsLeftToPath = SeedManager.combatRandom.Next(3, 10);

        doneInit = true;
    }

    public void SetTarget(Entity t)
    {
        if (target == null)
        {
            target = t;
        }
    }

    //Called from lua
    public void SetPath(Path_AStar newPath)
    {
        path = newPath;
    }

    bool CanDoAnAction()
    {
        if (entity == null || !doneInit || ObjectManager.playerEntity == null || entity.stats.HasEffect(C_StatusEffects.Blind))
        {
            return false;
        }

        if (npcBase.faction != null && npcBase.faction.HostileToPlayer())
        {
            npcBase.hostilityOverride = true;
        }

        if (!canAct)
        {
            canAct = true;
            return false;
        }

        if (entity)
        {
            entity.canAct = canAct;
        }

        return true;
    }

    void ThrowItem()
    {
        Item i = entity.inventory.items.FindAll(x => x.HasProp(ItemProperty.Throwing_Wep) || x.HasProp(ItemProperty.Explosive)).GetRandom();

        if (i != null)
        {
            entity.fighter.SelectItemToThrow(i);
            GameObject ex = Instantiate(explosive, target.transform.position, Quaternion.identity);
            Explosive exScript = ex.GetComponent<Explosive>();
            exScript.localPosition = target.myPos;
            entity.fighter.ThrowItem(target.myPos, exScript);

            FaceMe(target.myPos);
        }
    }

    public void Decision()
    {
        if (entity == null)
        {
            return;
        }

        if (npcBase.worldPosition != World.tileMap.WorldPosition)
        {
            Destroy(gameObject);
            return;
        }

        if (npcBase.HasFlag(NPC_Flags.Quantum_Locked) && InSightOfPlayer())
        {
            hasSeenPlayer = true;

            if (entity != null)
            {
                entity.Wait();
            }

            return;
        }

        if (npcBase.HasFlag(NPC_Flags.Inactive) || !CanDoAnAction())
        {
            if (entity)
            {
                entity.Wait();
            }

            return;
        }

        if (isStationary)
        {
            if (isHostile)
            {
                Hostile_Action();
            }

            return;
        }

        List<BodyPart.Grip> gripsAgainst = entity.body.AllGripsAgainst();

        if (gripsAgainst.Count > 0 && RNG.Chance(30))
        {
            Coord pos = entity.GetEmptyCoords().GetRandom();

            if (pos != null)
            {
                entity.Action(pos.x - entity.posX, pos.y - entity.posY);
                return;
            }
        }

        if (isHostile)
        {
            if (hasSeenPlayer)
            {
                int hearing = (npcBase.HasFlag(NPC_Flags.Quantum_Locked)) ? 99 : 94;

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
                else
                {
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
            PeacefulAction();
        }
    }

    public void NoticePlayer()
    {
        if (InSightOfPlayer())
        {
            Transform g = SimplePool.Spawn(World.poolManager.exclamationMark, transform.position + new Vector3(0.5f, 0.5f, 0)).transform;
            g.parent = transform;
            hasSeenPlayer = true;

            if (!hasAskedForHelp && entity.skills.HasAndCanUseSkill(C_Abilities.Help) && RNG.Chance(25))
            {
                entity.skills.CallForHelp();
                hasAskedForHelp = true;
            }
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
        {
            return false;
        }

        return (npcBase.faction.isHostileTo(other.npcBase.faction) || isFollower() && other.isHostile || other.isFollower() && isHostile || other.target == entity);
    }

    public bool isFollower()
    {
        return npcBase.HasFlag(NPC_Flags.Follower) || npcBase.faction.ID == C_Factions.Followers;
    }

    bool HasThrowingItem()
    {
        return (entity.inventory.items.FindAll(x => x.HasProp(ItemProperty.Throwing_Wep) || x.HasProp(ItemProperty.Explosive)).Count > 0);
    }

    bool UseRangedAttack(Entity target)
    {
        if (entity.inventory.firearm == null)
        {
            entity.inventory.firearm = ItemList.NoneItem;
        }

        if (!entity.inventory.firearm.HasProp(ItemProperty.Ranged) || isFollower() && target == ObjectManager.playerEntity)
        {
            return false;
        }

        distanceToTarget = GetDistance(target);

        if (RNG.Chance(60) && distanceToTarget < sightRange)
        {
            RangedAttack(target.myPos);
            return true;
        }

        return false;
    }

    void PeacefulAction()
    {
        if (target == null)
        {
            if (SearchForTarget())
            {
                Wander();
            }
            else
            {
                Hostile_Action();
            }
        }
        else
        {
            Hostile_Action();
        }
    }

    void Hostile_Action()
    {
        if (target == null || RNG.OneIn(5))
        {
            if (!SearchForTarget())
            {
                Wander();
                return;
            }
        }

        if (!isHostile && target == ObjectManager.playerEntity)
        {
            Wander();
            return;
        }

        if (RNG.Chance(35) && UsedAbility(target) && TargetInSight())
        {
            entity.Wait();
            return;
        }

        distanceToTarget = GetDistance(target);

        if (isStationary && distanceToTarget > 1.6f)
        {
            if (!UseRangedAttack(target))
            {
                entity.Wait();
                return;
            }
        }

        if (HasThrowingItem() && distanceToTarget.IsBetweenInclusive(3, 7) && RNG.Chance(3) && TargetInSight())
        {
            ThrowItem();
        }
        else if (!UseRangedAttack(target))
        {
            GetNewPath(target.myPos);
        }        
    }

    void GetNewPath(Coord dest)
    {
        path = new Path_AStar(entity.myPos, dest, entity.inventory.CanFly(), entity);

        if (path == null || !path.Traversable)
        {
            currentPathFails++;
            turnsLeftToPath = RNG.Next(5, 40);
            Wander();
            return;
        }

        Coord next = path.Pop();

        if (next == null)
        {
            Wander();
            return;
        }

        if (next.x == entity.posX && next.y == entity.posY && path.StepCount > 0)
        {
            next = path.Pop();
        }

        int moveX = next.x - entity.posX, moveY = next.y - entity.posY;

        if (npcBase.HasFlag(NPC_Flags.Aquatic) && !World.tileMap.IsWaterTile(entity.posX + moveX, entity.posY + moveY))
        {
            if (target.myPos != new Coord(entity.posX + moveX, entity.posY + moveY))
            {
                Wander();
                return;
            }
        }

        if (npcBase.HasFlag(NPC_Flags.Hit_And_Run) && RNG.OneIn(5))
        {
            moveX *= -1;
            moveY *= -1;
        }

        ConfirmAction(moveX, moveY);
        currentPathFails = 0;
    }

    bool SearchForTarget()
    {
        possibleTargets.Refresh();

        if (npcBase.faction.HostileToPlayer() || isHostile)
        {
            possibleTargets.Add(ObjectManager.playerEntity);
        }

        foreach (Entity ent in World.objectManager.onScreenNPCObjects)
        {
            if (!ent.isPlayer && ShouldAttack(ent.AI) && entity.InSight(ent.myPos))
            {
                possibleTargets.Add(ent);
            }
        }

        target = possibleTargets.Empty() ? null : GetClosestTarget(possibleTargets);

        return target != null;
    }

    void Wander()
    {
        if (entity == null)
        {
            entity = GetComponent<Entity>();
            entity.myPos = new Coord((int)transform.position.x, (int)transform.position.y);
        }

        if (target && target != ObjectManager.playerEntity)
        {
            Hostile_Action();
            return;
        }

        if (RNG.Chance(35) || (!isHostile && npcBase.HasFlag(NPC_Flags.Stationary_While_Passive)) || npcBase.HasFlag(NPC_Flags.Aquatic))
        {
            entity.Wait();
            return;
        }

        if (path != null && path.Traversable)
        {
            FollowPath();
        }
        else
        {
            Coord targetPosition = World.tileMap.CurrentMap.GetRandomFloorTile();

            if (targetPosition != null && currentPathFails < 3 && turnsLeftToPath <= 0)
            {
                GetNewPath(targetPosition);
            }
            else
            {
                turnsLeftToPath--;
                ConfirmAction(RNG.Next(-1, 2), RNG.Next(-1, 2));
            }
        }
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
                {
                    possibleTargets.Add(ent);
                }
            }

            target = (possibleTargets.Count > 0) ? GetClosestTarget(possibleTargets) : ObjectManager.playerEntity;
        }

        if (target == ObjectManager.playerEntity && GetDistance(ObjectManager.playerEntity) < 3 || npcBase.HasFlag(NPC_Flags.At_Home))
        {
            Wander();
            return;
        }

        if (!UseRangedAttack(target))
        {
            GetNewPath(target.myPos);
        } 
    }

    public void FollowPath()
    {
        if (path != null && path.Traversable)
        {
            Coord next = path.Pop();

            if (next == entity.myPos && path.StepCount > 0)
            {
                next = path.Pop();
            }

            ConfirmAction(next.x - entity.posX, next.y - entity.posY);
        }
        else
        {
            Wander();
        }
    }

    void RangedAttack(Coord targetPosition)
    {
        distanceToTarget = GetDistance(target);
        //Fleeing
        if (distanceToTarget < 3 && RNG.OneIn(5) && !isStationary)
        {
            int dirX = entity.posX - targetPosition.x, dirY = entity.posY - targetPosition.y;
            dirX = Mathf.Clamp(dirX, -1, 1);
            dirY = Mathf.Clamp(dirY, -1, 1);
            ConfirmAction(dirX, dirY);
        }
        else
        {
            //Out of sight
            if (!entity.InSight(targetPosition) || distanceToTarget > 10)
            {
                entity.Wait();
            }
            else
            {
                //Shoot
                if (entity.inventory.firearm.Charges() > 0 || entity.inventory.firearm.HasProp(ItemProperty.Cannot_Remove))
                {
                    entity.ShootAtTile(targetPosition.x, targetPosition.y);
                    FaceMe(target.myPos);
                }
                else if (!entity.ReloadWeapon())
                {
                    //Flee
                    int dirX = entity.posX - targetPosition.x, dirY = entity.posY - targetPosition.y;
                    dirX = Mathf.Clamp(dirX, -1, 1);
                    dirY = Mathf.Clamp(dirY, -1, 1);
                    ConfirmAction(dirX, dirY);
                }
            }
        }
    }

    //Summon slime allies to attack player.
    public void SummonAdd(string groupName)
    {
        if (entity == null)
        {
            entity = GetComponent<Entity>();
        }

        List<Coord> emptyCoords = entity.GetEmptyCoords();

        if (emptyCoords != null && emptyCoords.Count > 0)
        {
            SpawnController.SummonFromGroup(groupName, emptyCoords.GetRandom());
        }
    }

    public bool HasDetectedPlayer()
    {
        if (ObjectManager.playerEntity == null)
        {
            return false;
        }

        float dist = GetDistance(target);
        int playerStealth = ObjectManager.playerEntity.stats.StealthCheck() + ObjectManager.playerEntity.inventory.DisguiseStrength(npcBase.faction);
        int stealthRoll = RNG.Next(playerStealth);
        int perceptionRoll = RNG.Next(1, perception + 1);

        if (isStationary && dist < 1.6f && stealthRoll < perceptionRoll + 2)
        {
            return true;
        }

        int newSightRange = sightRange;

        if (dist >= newSightRange / 2)
        {
            perceptionRoll--;
        }

        return dist < newSightRange && stealthRoll < perceptionRoll;
    }

    public void AnswerCallForHelp(BaseAI bai, Entity targ)
    {
        target = targ;
        hasAskedForHelp = true;

        if (bai.npcBase.faction == npcBase.faction && targ == ObjectManager.playerEntity)
        {
            hasSeenPlayer = true;
            npcBase.hostilityOverride = true;
        }
    }

    public void BecomeHostile()
    {
        if (!isHostile && npcBase.HasFlag(NPC_Flags.Faction_Leader) && !ObjectManager.playerJournal.HasFlag(C_Factions.HostileTo_(npcBase.faction)))
        {
            ObjectManager.playerJournal.AddFlag(C_Factions.HostileTo_(npcBase.faction));
            npcBase.questID = string.Empty;
            dialogueController.SetupDialogueOptions();
        }

        hasSeenPlayer = true;
        npcBase.hostilityOverride = true;

        for (int i = 0; i < World.objectManager.onScreenNPCObjects.Count; i++)
        {
            if (World.objectManager.onScreenNPCObjects[i].AI.npcBase.faction == npcBase.faction && !World.objectManager.onScreenNPCObjects[i].AI.isHostile)
            {
                BaseAI bai = World.objectManager.onScreenNPCObjects[i].AI;

                if (bai != null && bai.InSightOfPlayer())
                {
                    bai.AnswerCallForHelp(this, target);
                    bai.npcBase.questID = string.Empty;
                    bai.dialogueController.SetupDialogueOptions();
                }
            }
        } 
    }
    
    void ConfirmAction(int x, int y)
    {
        if (npcBase.HasFlag(NPC_Flags.Quantum_Locked) && World.tileMap.InSightCoords().Contains(new Coord(entity.posX + x, entity.posY + y)))
        {
            entity.Wait();
        }
        else
        {
            if (x != 0)
            {
                SetXScale(-x);
            }

            entity.Action(x, y);
            entity.canAct = false;
        }
    }

    void SetXScale(int x)
    {
        if (spriteComponent == null)
        {
            spriteComponent = GetComponentInChildren<NPCSprite>();

        }

        spriteComponent.SetXScale(x);
    }

    float GetDistance(Entity targetEntity)
    {
        if (targetEntity == null || entity == null)
        {
            return Mathf.Infinity;
        }

        return entity.myPos.DistanceTo(targetEntity.myPos);
    }

    Entity GetClosestTarget(List<Entity> entities)
    {
        Entity closest = ObjectManager.playerEntity;
        float distance = Mathf.Infinity;

        foreach (Entity en in entities)
        {
            if (!entity.InSight(en.myPos))
            {
                continue;
            }

            float dist = GetDistance(en);

            if (closest == null)
            {
                distance = dist;
                closest = en;
            }
            else if (dist < distance)
            {
                closest = en;
                distance = dist;
            }
        }

        if (closest == null)
        {
            return entity;
        }

        return closest;
    }

    public void FaceMe(Coord pos)
    {
        if (pos.x != entity.posX)
        {
            SetXScale((pos.x > entity.myPos.x) ? -1 : 1);
        }
    }

    void OnDeath(Entity ent)
    {
        EventHandler.instance.OnNPCDeath(npcBase);

        if (npcBase.HasFlag(NPC_Flags.OnDeath_Explode))
        {
            for (int x = entity.posX - 1; x <= entity.posX + 1; x++)
            {
                for (int y = entity.posY - 1; y <= entity.posY + 1; y++)
                {
                    if (x == entity.posX && y == entity.posY)
                    {
                        continue;
                    }

                    if (x == entity.posX || y == entity.posY && World.tileMap.WalkableTile(x, y))
                    {
                        World.objectManager.SpawnExplosion(entity, new Coord(x, y));
                    }
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
                    if ((x == entity.posX || y == entity.posY) && World.tileMap.WalkableTile(x, y))
                    {
                        World.objectManager.SpawnPoisonGas(entity, new Coord(x, y));
                    }
                }
            }
        }

        //Destroy all tentacles.
        if (npcBase.ID == C_NPCs.TheEmpty)
        {
            List<Entity> tentacles = World.objectManager.onScreenNPCObjects.FindAll(x => x.AI.npcBase.ID == C_NPCs.TheEmpty_Tentacle);

            foreach (Entity e in tentacles)
            {
                e.fighter.Remove();
            }

            MapObject m = new MapObject(ItemList.GetMOB("Crystal_Surface"), entity.GetEmptyCoords().GetRandom(), World.tileMap.WorldPosition, World.tileMap.currentElevation);
            World.objectManager.SpawnObject(m);
        }
    }

    void CacheVars()
    {
        spriteComponent = GetComponent<NPCSprite>();
        gameObject.name = npcBase.name;
        perception = npcBase.Attributes[C_Attributes.Perception];

        if (npcBase.faction != null && npcBase.faction.HostileToPlayer())
        {
            npcBase.hostilityOverride = true;
        }

        if (npcBase.faction.HostileToPlayer())
        {
            OverrideHostility(true);
        }

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
            npcBase.inventory = inv.items;
            npcBase.bodyParts = entity.body.bodyParts;
            npcBase.handItems = new List<Item>();

            List<BodyPart.Hand> hands = entity.body.Hands;

            if (hands.Count > 0)
            {
                for (int i = 0; i < hands.Count; i++)
                {
                    if (hands[i].EquippedItem == null)
                    {
                        hands[i].SetEquippedItem(ItemList.GetItemByID(entity.body.Hands[i].baseItem), entity);
                    }

                    npcBase.handItems.Add(hands[i].EquippedItem);
                }
            }
            else
            {
                npcBase.handItems = new List<Item>() { entity.body.defaultHand.EquippedItem };
            }

            int traitCount = entity.stats.traits.Count;

            for (int i = 0; i < traitCount; i++)
            {
                npcBase.traits.Add(entity.stats.traits[0].ID);
                entity.stats.RemoveTrait(entity.stats.traits[0].ID);
            }

            npcBase.firearm = inv.firearm;
        }
    }

    public void HireAsFollower()
    {
        if (!isFollower())
        {
            npcBase.MakeFollower();
            dialogueController.SetupDialogueOptions();
            CombatLog.NameMessage("Message_Hire", entity.Name);
        }
    }

    public bool HasSeenPlayer()
    {
        return hasSeenPlayer;
    }

    public bool InSightOfPlayer()
    {
        if (entity.cell == null)
        {
            entity.SetCell();
        }

        bool sight = entity.cell.InSight;

        spriteRenderer.color = sight ? Color.white : Color.clear;

        return sight;
    }

    public bool TargetInSight()
    {
        if (target == null)
        {
            return false;
        }

        return entity.InSight(target.myPos);
    }

    bool UsedAbility(Entity targetEntity)
    {
        if (entity.stats.stamina <= 0 || entity.stats.SkipTurn() || !InSightOfPlayer() || target == null || entity.stats.SkipTurn())
        {
            return false;
        }

        if (UseAbility_Unique() || UseAbility_Lua())
        {
            return true;
        }

        return false;
    }

    List<Ability> possible;

    bool UseAbility_Lua()
    {
        if (RNG.Chance(50) || entity.stats.HasEffect(C_StatusEffects.Blind))
        {
            return false;
        }

        if (possible == null)
        {
            possible = new List<Ability>();
        }
        else
        {
            possible.Clear();
        }

        for (int i = 0; i < entity.skills.abilities.Count; i++)
        {
            Ability skill = entity.skills.abilities[i];

            if (skill.aiAction == null || skill.cooldown > 0 || !entity.skills.CanUseSkill(skill.staminaCost) || 
                skill.castType == CastType.Target && entity.myPos.DistanceTo(target.myPos) > skill.range)
            {
                continue;
            }

            DynValue result = LuaManager.CallScriptFunction(skill.aiAction, new object[] { skill, entity, target });
            bool canUse = result.Boolean;

            if (canUse)
            {
                possible.Add(skill);
            }
        }

        if (possible.Count > 0)
        {
            Ability choice = possible.GetRandom();

            switch (choice.castType)
            {
                case CastType.Direction:
                    int dirX = (target.posX - entity.posX), dirY = (target.posY - entity.posY);

                    if (dirX != 0) dirX = dirX > 0 ? 1 : -1;
                    if (dirY != 0) dirY = dirY > 0 ? 1 : -1;

                    choice.ActivateCoordinateSkill(entity.skills, new Coord(dirX, dirY));
                    break;
                case CastType.Instant:
                    choice.Cast(entity);
                    break;
                case CastType.Target:
                    choice.ActivateCoordinateSkill(entity.skills, target.myPos);
                    break;
            }

            return true;
        }

        return false;
    }

    bool UseAbility_Unique()
    {
        distanceToTarget = GetDistance(target);

        //Call for help
        if (!hasAskedForHelp && entity.skills.HasAndCanUseSkill(C_Abilities.Help) && RNG.Chance(10))
        {
            entity.skills.CallForHelp();
            hasAskedForHelp = true;
            entity.skills.abilities.Find(x => x.ID == C_Abilities.Help).InitializeCooldown();

            return true;
        }

        //The End - Spawn tentacle
        if (npcBase.ID == C_NPCs.TheEmpty && RNG.Chance(20))
        {
            List<Entity> tentacles = World.objectManager.onScreenNPCObjects.FindAll(x => x.AI.npcBase.ID == C_NPCs.TheEmpty_Tentacle);

            if (tentacles.Count < 6)
            {
                List<Coord> m = target.GetEmptyCoords(RNG.Next(1, 3));
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
                    NPC n = EntityList.GetNPCByID(C_NPCs.TheEmpty_Tentacle, World.tileMap.WorldPosition, lps.GetRandom());
                    World.objectManager.SpawnNPC(n);
                    return true;
                }
            }
            else
            {
                tentacles.GetRandom().fighter.Remove();
            }
        }

        //Summon Slimes
        if (npcBase.HasFlag(NPC_Flags.Summon_Adds) && RNG.OneIn(10) && entity.skills.CanUseSkill(3))
        {
            entity.stats.UseStamina(3);
            SummonAdd(C_NPCGroups.Slimeites);
            return true;
        }

        //Leprosy
        if (npcBase.HasFlag(NPC_Flags.Skills_Leprosy) && RNG.OneIn(5))
        {
            CombatLog.CombatMessage("Bites", entity.Name, target.Name, entity.isPlayer);

            if (RNG.Chance(10))
            {
                target.stats.InitializeNewTrait(TraitList.GetTraitByID(C_Traits.Leprosy));
            }

            entity.fighter.Attack(target.stats, true);
            return true;
        }

        //Grappling
        if (distanceToTarget < 2f && entity.skills.HasAndCanUseSkill(C_Abilities.Grapple) && RNG.OneIn(5))
        {
            if (entity.body.AllGrips() == null || entity.body.AllGrips().Count == 0)
            {
                entity.skills.Grapple_GrabPart(target.body.SeverableBodyParts().GetRandom());
            }
            else
            {
                BodyPart.Grip currentGrip = entity.body.AllGrips().FirstOrDefault();

                if (currentGrip != null && currentGrip.CanStrangle())
                {
                    entity.skills.Grapple_Strangle(target.stats); //Choke me, daddy.
                }
                else
                {
                    int ran = RNG.Next(100);

                    if (ran <= 40)
                        entity.skills.Grapple_TakeDown(target.stats, currentGrip.heldPart.displayName);
                    else if (ran <= 60)
                        entity.skills.Grapple_Shove(currentGrip);
                    else if (ran <= 85 && entity.stats.Strength >= 5)
                        entity.skills.Grapple_Pull(currentGrip);
                    else if (ran <= 93 && entity.stats.Dexterity >= 6)
                        entity.skills.Grapple_Disarm(currentGrip);
                    else
                        return false;
                }
            }

            return true;
        }

        //Dive
        if (RNG.OneIn(5) && npcBase.HasFlag(NPC_Flags.Aquatic) 
            && TileManager.isWaterTile(World.tileMap.GetTileID(entity.posX, entity.posY)))
        {
            if (entity.body.AllGripsAgainst().Count <= 0 && !entity.stats.HasEffect(C_StatusEffects.Underwater))
            {
                entity.stats.AddStatusEffect(C_StatusEffects.Underwater, RNG.Next(3, 11));
                return true;
            }
        }

        return false;
    }
}