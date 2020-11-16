using System;
using System.Collections.Generic;
using Assets.Scripts.GridNav.NodeOptimizer;
using UnityEngine;

namespace Assets.Scripts.Testing.ProvisTesting {

    [Serializable]
    public class ProvisFurniture : IPoiGenerator<Vector2Int, Vector2, SearchNode> {
#region Implementation of IPoiGenerator<Vector2Int,Vector2,SearchNode>

        [SerializeField]
        public RectInt Area;
        public List<SearchNode> GetPoiNodes() {
            List<SearchNode> ret = new List<SearchNode> {
                new SearchNode {
                    Id = Area.min - Vector2Int.one
                },
                new SearchNode {
                    Id = Area.max + Vector2Int.one
                },
                new SearchNode {
                    Id = new Vector2Int(Area.xMin - 1, Area.yMax + 1)
                },
                new SearchNode {
                    Id = new Vector2Int(Area.xMax + 1, Area.yMin - 1)
                }
            };
            return ret;
        }

#endregion
    }

}