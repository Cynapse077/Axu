using System;
using System.Collections.Generic;

public static class ContextualMenu
{
    static List<ContextualAction> actions;

    public static List<ContextualAction> GetActions()
    {
        if (actions == null)
            actions = new List<ContextualAction>();
        else
            actions.Clear();

        SurroundingTileActions();
        return actions;
    }

    static void SurroundingTileActions()
    {
        Entity ent = ObjectManager.playerEntity;

        for (int x = ent.posX - 1; x <= ent.posX + 1; x++)
        {
            for (int y = ent.posY - 1; y <= ent.posY + 1; y++)
            {
                Coord c = new Coord(x, y);

                if (!World.tileMap.WalkableTile(x, y) || World.tileMap.GetCellAt(c) == null)
                    continue;

                SearchCell(World.tileMap.GetCellAt(c), Direction(c - ent.myPos));
            }
        }
    }

    static void SearchCell(Cell c, string dir)
    {
        if (c.position == ObjectManager.playerEntity.myPos)
        {
            int tileID = World.tileMap.GetTileID(c.position.x, c.position.y);
            PlayerInput inp = ObjectManager.playerEntity.GetComponent<PlayerInput>();

            if (tileID == TileManager.tiles["Stairs_Up"].ID)
            {
                actions.Add(new ContextualAction("Ascend Stairs", () => 
                    World.userInterface.YesNoAction("YN_GoUp".Translate(), inp.GoUp, World.userInterface.CloseWindows, "") 
                ));
            }
            else if (tileID == TileManager.tiles["Stairs_Down"].ID)
            {
                actions.Add(new ContextualAction("Ascend Stairs", () => 
                    World.userInterface.YesNoAction("YN_GoDown".Translate(), inp.GoDown, World.userInterface.CloseWindows, "") 
                ));
            }
        }

        if (c.entity != null && !c.entity.isPlayer)
        {
            if (!c.entity.AI.isHostile && c.entity.AI.npcBase.HasFlag(NPC_Flags.Can_Speak))
            {
                actions.Add(new ContextualAction(string.Format("Talk to {0} {1}", c.entity.MyName, dir), () => 
                    World.userInterface.ShowNPCDialogue(c.entity.GetComponent<DialogueController>()) 
                ));
            }

        }

        List<MapObjectSprite> objects = c.mapObjects;

        for (int i = 0; i < objects.Count; i++)
        {
            MapObjectSprite mos = objects[i];

            if (mos.inv != null && mos.inv.items.Count > 0)
            {
                string acName = string.Format("Interact with {0} {1}", mos.name, dir);

                actions.Add(new ContextualAction(acName, () => {
                    World.userInterface.OpenLoot(mos.inv);
                    mos.Interact();
                } ));
            }
            else if (mos.objectBase.HasEvent("OnInteract"))
            {
                actions.Add(new ContextualAction(string.Format("Interact with {0} {1}", mos.name, dir), () => mos.Interact() ));
            }
            else if (mos.isDoor_Closed)
            {
                actions.Add(new ContextualAction(string.Format("Open {0} {1}", mos.name, dir), () => mos.Interact() ));
            }
            else
            {
                switch (mos.objectType)
                {
                    case "Terminal_Off":
                    case "Crystal":
                    case "Robot_Frame":
                        actions.Add(new ContextualAction(string.Format("Interact with {0} {1}", mos.name, dir), () => mos.Interact() ));
                        break;
                    case "Barrel":
                    case "Chest":
                    case "Chest_Open":
                    case "Cryopod_Close":
                        actions.Add(new ContextualAction(string.Format("Open {0} {1}", mos.name, dir), () => mos.Interact() ));
                        break;
                    case "Grave":
                        if (ObjectManager.playerEntity.inventory.DiggingEquipped())
                        {
                            actions.Add(new ContextualAction(string.Format("Dig {0} {1}", mos.name, dir), () => mos.Interact() ));
                        }
                        break;
                    case "Statue":
                        actions.Add(new ContextualAction(string.Format("Break {0} {1}", mos.name, dir), () => mos.Interact() ));
                        break;
                    case "Ore":
                        if (ObjectManager.playerEntity.inventory.DiggingEquipped())
                        {
                            actions.Add(new ContextualAction(string.Format("Mine {0} {1}", mos.name, dir), () => mos.Interact() ));
                        }
                        break;
                    case "Headstone":
                        actions.Add(new ContextualAction(string.Format("Read {0} {1}", mos.name, dir), () => mos.Interact() ));
                        break;
                    case "Bookshelf":
                        actions.Add(new ContextualAction(string.Format("Check the {0} {1}", mos.name, dir), () => mos.Interact() ));
                        break;
                }
            }
        }
    }

    static string Direction(Coord dir)
    {
        if (dir.x == 0 && dir.y == 0) return "(Below)";
        if (dir.x == 0 && dir.y == 1) return "(N)";
        if (dir.x == 1 && dir.y == 0) return "(E)";
        if (dir.x == 0 && dir.y == -1) return "(S)";
        if (dir.x == -1 && dir.y == 0) return "(W)";

        if (dir.x == 1 && dir.y == 1) return "(NE)";
        if (dir.x == -1 && dir.y == 1) return "(NW)";
        if (dir.x == 1 && dir.y == -1) return "(SE)";
        if (dir.x == -1 && dir.y == -1) return "(SW)";

        return string.Empty;
    }

    public struct ContextualAction
    {
        public string actionName;
        public Action myAction;

        public ContextualAction(string _actionName, Action _myAction)
        {
            actionName = _actionName;
            myAction = _myAction;
        }
    }
}
