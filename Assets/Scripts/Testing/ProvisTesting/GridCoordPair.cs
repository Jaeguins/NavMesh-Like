using System;
using UnityEngine;

namespace Assets.Scripts.Testing.ProvisTesting {

    [Serializable]
    public struct GridCoordPair {
        public ProvisGrid GridA,
                          GridB;
        public Vector2Int CoordA,
                          CoordB;
    }

}