using UnityEngine;
using Pathfinding;

public class MouseController : MonoBehaviour
{
    public Transform cursorObject;
    public Vector3 cursorPosition;
    public Sprite[] sprites;

    GameObject arrow;
    UserInterface userInterface;
    Transform cursor, worldCursor;
    SpriteRenderer sRenderer;
    Entity playerEntity;
    PlayerInput playerInput;
    Camera worldCamera;

    void Start()
    {
        cursor = Instantiate(cursorObject).transform;
        arrow = cursor.GetChild(0).GetChild(0).gameObject;
        sRenderer = cursor.GetComponentInChildren<SpriteRenderer>();
        userInterface = GameObject.FindObjectOfType<UserInterface>();
    }

    void Update()
    {
        if (ObjectManager.playerEntity == null)
            return;
        else
        {
            playerEntity = ObjectManager.playerEntity;

            if (playerEntity != null)
            {
                playerInput = playerEntity.GetComponent<PlayerInput>();
            }
        }

        if (GameSettings.UseMouse)
        {
            HandleMouseInput();
        }
    }

    void HandleMouseInput()
    {
        Vector3 pos = Input.mousePosition;
        pos = Camera.main.ScreenToWorldPoint(pos);
        pos.x = Mathf.FloorToInt(pos.x);
        pos.y = Mathf.FloorToInt(pos.y);
        pos.z = -1;
        cursorPosition = pos;

        if (Input.GetAxis("MouseX") != 0 || Input.GetAxis("MouseY") != 0)
            CursorIsActive = true;

        if (World.userInterface != null && !World.userInterface.NoWindowsOpen)
        {
            CursorIsActive = false;
            return;
        }

        if (PlayerInput.fullMap)
        {
            //WorldMapHandling();
        }
        else if (CursorIsActive)
        {
            LocalMapHandling(pos);
        }
    }

    void LocalMapHandling(Vector3 pos)
    {
        if (playerEntity == null)
            return;

        cursor.transform.position = pos;
        int posX = (int)cursor.position.x, posY = (int)cursor.position.y;

        if (posX < 0 || posX > Manager.localMapSize.x - 1 || posY >= 0 || posY < -Manager.localMapSize.y)
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (Vector2.Distance(cursor.position, playerEntity.transform.position) < 2)
                {
                    MoveOffScreen(new Coord(Mathf.FloorToInt(cursor.position.x), Mathf.FloorToInt(cursor.position.y)));
                }
                else
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (Mathf.Abs(x) + Mathf.Abs(y) == 2 || posX + x < 0 || posX + x > Manager.localMapSize.x - 1
                                || posY + y >= 0 || posY + y < -Manager.localMapSize.y)
                                continue;
                            MovePlayer(new Coord(posX + x, posY + y));
                        }
                    }
                }
            }

            arrow.SetActive(true);
            Vector3 rot = new Vector3(0, 0, 0);

            if (posX < 0)
            {
                if (posY >= 0)
                {
                    rot.z = 45;
                }
                else if (posY < -Manager.localMapSize.y)
                {
                    rot.z = 135;
                }
                else
                {
                    rot.z = 90;
                }
            }
            else if (posX >= Manager.localMapSize.x)
            {
                if (posY >= 0)
                {
                    rot.z = -45;
                }
                else if (posY < -Manager.localMapSize.y)
                {
                    rot.z = -135;
                }
                else
                {
                    rot.z = -90;
                }
            }
            else if (posY < -Manager.localMapSize.y)
            {
                rot.z = 180;
            }
            else if (posY >= 0)
            {
                rot.z = 0;
            }

            arrow.transform.localRotation = Quaternion.Euler(rot);
        }
        else
        {
            bool canSee = (playerEntity.inSight(posX, posY) || World.tileMap.CurrentMap.has_seen[posX, posY + Manager.localMapSize.y]);
            sRenderer.sprite = (canSee) ? sprites[0] : sprites[1];

            Coord targetPos = new Coord((int)cursor.position.x, (int)cursor.position.y);

            if (canSee)
            {
                if (Input.GetMouseButtonUp(0))
                    MovePlayer(targetPos);
                if (Input.GetMouseButtonDown(1) && ObjectManager.playerEntity.inventory.firearm.HasProp(ItemProperty.Ranged))
                    ObjectManager.playerEntity.ShootAtTile(targetPos.x, targetPos.y);
            }

            arrow.SetActive(false);
        }
    }

    void WorldMapHandling()
    {
        if (worldCamera == null)
            worldCamera = GameObject.FindObjectOfType<MiniMap>().GetComponent<Camera>();
        if (worldCursor == null)
            worldCursor = GameObject.Find("WorldPointerObject").transform;

        Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 pos = hit.point;

            pos.x = Mathf.FloorToInt(pos.x);
            pos.y = Mathf.FloorToInt(pos.y);

            Coord targetPos = new Coord(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
            worldCursor.position = pos + new Vector3(0.5f, 0.5f, 0);
            cursor.position = pos;

            if (Input.GetMouseButtonUp(0))
            {
                MoveWorld(targetPos);
            }
        }
    }

    void MoveWorld(Coord targetPos)
    {
        playerInput.CancelWorldPath();
        targetPos.x -= 50;
        targetPos.y += 200;
        playerInput.SetWorldPath(targetPos);
    }

    void MoveOffScreen(Coord targetPos)
    {
        int playerPosX = playerEntity.posX;
        int playerPosY = playerEntity.posY - Manager.localMapSize.y;

        Coord direction = new Coord(targetPos.x - playerPosX, targetPos.y - playerPosY);
        World.tileMap.CheckEdgeLocalMap(playerEntity.posX + direction.x, playerEntity.posY + direction.y);
    }

    public void MovePlayer(Coord targetPos)
    {
        targetPos.y += Manager.localMapSize.y;

        if (playerInput.cursorMode == PlayerInput.CursorMode.Direction)
        {
            if (Vector2.Distance(targetPos.toVector2(), playerEntity.myPos.toVector2()) > 1.5f)
            {
                playerInput.CancelLook();
                return;
            }

            playerInput.SelectDirection(targetPos.x - playerEntity.posX, targetPos.y - playerEntity.posY);
            return;
        }

        //if you are clicking on the character's tile.
        if (targetPos == playerEntity.myPos)
        {
            if (World.tileMap.GetTileID(playerEntity.posX, playerEntity.posY) == Tile.tiles["Stairs_Up"].ID)
            {
                userInterface.YesNoAction("YN_GoUp", () => { playerInput.GoUp(); }, null, "");
                return;
            }
            else if (World.tileMap.GetTileID(playerEntity.posX, playerEntity.posY) == Tile.tiles["Stairs_Down"].ID)
            {
                userInterface.YesNoAction("YN_GoDown", () => { playerInput.GoDown(); }, null, "");
                return;
            }


            if (playerInput)
            {
                if (World.tileMap.GetCellAt(targetPos).mapObjects.Count > 0 && World.tileMap.GetCellAt(targetPos).HasInventory())
                    playerInput.SelectDirection(targetPos.x - playerEntity.posX, targetPos.y - playerEntity.posY);
                else
                    playerEntity.Wait();
            }

            return;
        }

        //There is an adjacent NPC here. Interact with them.
        if (playerEntity.myPos.DistanceTo(targetPos) < 2f && World.tileMap.GetCellAt(targetPos) != null)
        {
            Entity e = World.tileMap.GetCellAt(targetPos).entity;

            if (e != null && !e.GetComponent<BaseAI>().isHostile)
            {
                playerInput.SelectDirection(targetPos.x - playerEntity.posX, targetPos.y - playerEntity.posY);
                return;
            }
            if (World.tileMap.GetCellAt(targetPos).mapObjects.Count > 0)
                playerInput.SelectDirection(targetPos.x - playerEntity.posX, targetPos.y - playerEntity.posY);
        }

        //Move to the selected position.
        Path_AStar path = new Path_AStar(playerEntity.myPos, targetPos, playerEntity.inventory.CanFly());
        playerEntity.CancelWalk();
        playerEntity.path = path;
    }

    public bool CursorIsActive
    {
        get
        {
            return cursor.gameObject.activeSelf;
        }
        set
        {
            cursor.gameObject.SetActive(value);
        }
    }
}
