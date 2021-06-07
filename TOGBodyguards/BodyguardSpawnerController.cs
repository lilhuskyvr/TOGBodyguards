using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOGBodyguards
{
    public class BodyguardSpawnerController : MonoBehaviour
    {
        public Dictionary<string, bool> bodyguards = new Dictionary<string, bool>();

        public List<string> femaleBodyguardNames = new List<string>()
        {
            "BG_Julia",
            "BG_Helga",
            "BG_Fury",
            "BG_Alessia"
        };
    }
}