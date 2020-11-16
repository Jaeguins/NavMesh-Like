using System.Collections.Generic;
using Assets.Scripts.GridNav.Astar;
using Assets.Scripts.GridNav.NodeOptimizer;
using UnityEngine;

namespace Assets.Scripts.Testing.ProvisTesting {

    public class SearchNode : IPoiNode<Vector2Int, Vector2> {
#region Implementation of IPoiNode<Vector2Int,Vector2>

        public Vector2Int Id { get; set; }
        public List<Vector2Int> Neighbor { get; set; } = new List<Vector2Int>();
        public List<Vector2> Group { get; set; } = new List<Vector2>();
        public IPoiNode<Vector2Int, Vector2> DeepCopy() {
            SearchNode ret = new SearchNode {
                Id = Id
            };
            foreach (var t in Neighbor) ret.Neighbor.Add(t);
            foreach (var t in Group) ret.Group.Add(t);
            return ret;
        }

#endregion

        public AStarFinder<Vector2Int>.AstarNodeRuntime ToRuntimeNode() {
            AStarFinder<Vector2Int>.AstarNodeRuntime ret = new AStarFinder<Vector2Int>.AstarNodeRuntime {
                Id = Id,
                GoodToBePath = true
            };
            foreach (var t in Neighbor) ret.Neighbors.Add(t);
            return ret;
        }
    }

}