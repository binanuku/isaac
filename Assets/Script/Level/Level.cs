﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Level : MonoBehaviour
{
    [Header("房间")]
    [SerializeField]
    private Room roomPrefab;
    [SerializeField]
    private int roomNum;
    [HideInInspector]
    public Room[,] roomArray = new Room[20, 20];
    private List<Room> haveBeenToRoomList = new List<Room>();
    [HideInInspector]
    public Room currentRoom;

    [Header("道具房间池")]
    [HideInInspector]
    public Pools pools;

    Player player;
    UIManager UI;

    private void Awake()
    {
        pools = GetComponent<Pools>();
    }

    private void Start()
    {
        UI = UIManager.Instance;
        player = GameManager.Instance.player;
        GenerateRooms();
        LinkDoors();
        SetRoomsType();
        UI.miniMap.CreatMiniMap();
        StartCoroutine(MoveToNextRoom(Vector2.zero));
    }

    /// <summary>
    /// 创建所有的房间
    /// </summary>
    private void GenerateRooms()
    {
        //储存备选生成房间的位置列表
        List<Vector2> alternativeRoomList = new List<Vector2>();
        List<Vector2> hasBeenRemoveRoomList = new List<Vector2>();

        //创建起始房间
        int outsetX = roomArray.GetLength(0) / 2;
        int outsetY = roomArray.GetLength(1) / 2;
        Room lastRoom = roomArray[outsetX, outsetY] = createRoom(new Vector2(outsetX, outsetY));
        currentRoom = lastRoom;

        //创建其他房间
        for (int i = 1; i < roomNum; i++)
        {
            int x = (int)lastRoom.coordinate.x; int y = (int)lastRoom.coordinate.y;

            Action<int, int> action = (newX, newY) =>
             {
                 Vector2 coordinate = new Vector2(newX, newY);
                 if (roomArray[newX, newY] == null)
                 {
                     if (alternativeRoomList.Contains(coordinate))
                     {
                         alternativeRoomList.Remove(coordinate);
                         hasBeenRemoveRoomList.Add(coordinate);
                     }
                     else if (!hasBeenRemoveRoomList.Contains(coordinate))
                     {
                         alternativeRoomList.Add(coordinate);
                     }
                 }
             };
            action(x + 1, y);
            action(x - 1, y);
            action(x, y - 1);
            action(x, y + 1);

            Vector2 newRoomCoordinate = alternativeRoomList[UnityEngine.Random.Range(0, alternativeRoomList.Count)];
            lastRoom = roomArray[(int)newRoomCoordinate.x, (int)newRoomCoordinate.y] = createRoom(newRoomCoordinate);
            alternativeRoomList.Remove(newRoomCoordinate);
        }
    }
    private Room createRoom(Vector2 coordinate)
    {
        Room newRoom = Instantiate(roomPrefab, transform);
        newRoom.coordinate = coordinate;

        int x = (int)coordinate.x - roomArray.GetLength(0) / 2;
        int y = (int)coordinate.y - roomArray.GetLength(1) / 2;
        float roomHeight = 2 * newRoom.roomSize.x;
        float roomWidth = 2 * newRoom.roomSize.y;
        newRoom.transform.position = new Vector2(y * roomWidth, x * roomHeight);

        return newRoom;
    }

    /// <summary>
    /// 打通各个房间相连的门，并记录相连信息
    /// </summary>
    private void LinkDoors()
    {
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                int x = (int)room.coordinate.x; int y = (int)room.coordinate.y;
                if (roomArray[x + 1, y] != null)
                {
                    Room neighboringRoom = roomArray[x + 1, y];
                    GameObject neighboringDoor = neighboringRoom.doorList[1];
                    room.ActivationDoor(Room.Didirection.Up, neighboringRoom, neighboringDoor);
                }
                if (roomArray[x - 1, y] != null)
                {
                    room.ActivationDoor(Room.Didirection.Down, roomArray[x - 1, y], (roomArray[x - 1, y].doorList[0]));
                }
                if (roomArray[x, y - 1] != null)
                {
                    room.ActivationDoor(Room.Didirection.Left, roomArray[x, y - 1], roomArray[x, y - 1].doorList[3]);
                }
                if (roomArray[x, y + 1] != null)
                {
                    room.ActivationDoor(Room.Didirection.Right, roomArray[x, y + 1], roomArray[x, y + 1].doorList[2]);
                }
            }
        }
    }

    /// <summary>
    /// 设置各个房间的类型
    /// </summary>
    private void SetRoomsType()
    {
        //设置类型
        //获取所有单门房间
        List<Room> singleDoorRoomList = new List<Room>();
        foreach (Room room in roomArray)
        {
            if (room != null && room.ActiveDoorCount == 1 && room != currentRoom)
            {
                singleDoorRoomList.Add(room);
            }
        }

        //先全部设为普通
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.roomType = Room.RoomType.Normal;
            }
        }
        //宝藏
        if (singleDoorRoomList.Count > 2)
        {
            for (int i = 0; i < singleDoorRoomList.Count - 2; i++)
            {
                singleDoorRoomList[i].roomType = Room.RoomType.Treasure;
            }
        }
        //Boss
        singleDoorRoomList[singleDoorRoomList.Count - 1].roomType = Room.RoomType.Boss;
        ////商店
        //singleDoorRoomList[singleDoorRoomList.Count - 2].roomType = Room.RoomType.Shop;
        //起始
        currentRoom.roomType = Room.RoomType.Start;

        //初始化
        foreach (Room room in roomArray)
        {
            if (room != null)
            {
                room.InitializeRoom();
            }
        }
    }

    /// <summary>
    /// 更新玩家所在房间
    /// </summary>
    /// <param name="MoveDirection">移动方向，使用Vector2默认的几个类型</param>
    public IEnumerator MoveToNextRoom(Vector2 MoveDirection)
    {
        Camera mainCamera = GameManager.Instance.myCamera;
        float delaySeconds = 0.3f;

        int x = (int)currentRoom.coordinate.x + (int)MoveDirection.y;
        int y = (int)currentRoom.coordinate.y + (int)MoveDirection.x;
        currentRoom = roomArray[x, y];

        //如果没去过该房间便生成房间内容
        if (!haveBeenToRoomList.Contains(currentRoom))
        {
            currentRoom.GenerateRoomContent(delaySeconds);
            haveBeenToRoomList.Add(currentRoom);
        }
        UI.miniMap.UpdateMiniMap(MoveDirection);

        //暂停并移动玩家
        player.PlayerPause();
        player.transform.position += (Vector3)MoveDirection;

        //移动镜头
        Vector3 originPos = mainCamera.transform.position;
        Vector3 targetPos = currentRoom.transform.position;
        targetPos.z += mainCamera.transform.position.z;
        float time = 0;
        while (time <= delaySeconds)
        {
            mainCamera.transform.position = Vector3.Lerp(originPos, targetPos, (1 / delaySeconds) * (time += Time.deltaTime));
            yield return 0;
        }

        //恢复玩家暂停
        player.PlayerQuitPause();
    }
}
