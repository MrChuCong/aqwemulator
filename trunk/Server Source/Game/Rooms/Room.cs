﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AQWE.Net;
using AQWE.Game.Managers;

namespace AQWE.Game.Rooms
{
    public class Room
    {
        internal int ID; //You have joined battleon-XXX
        internal int mapID;
        internal string mapName;
        internal string fileName;
        public Dictionary<int, userManager> activeUsers;

        public Room(int mapID)
        {
            this.mapID = mapID;
            activeUsers = new Dictionary<int, userManager>();

            roomManager.Add(this.mapID, this);
        }

        public void AddUser(int userID, userManager User)
        {
            if (!activeUsers.ContainsKey(userID))
                activeUsers.Add(userID, User);
        }

        public void RemoveUser(int userID)
        {
            if (activeUsers.ContainsKey(userID))
                activeUsers.Remove(userID);
        }

        public int CountUsers()
        {
            return activeUsers.Count;
        }
    }
}