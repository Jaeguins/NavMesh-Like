using System.Collections.Generic;
using Assets.Scripts.GridNav.Astar;
using Assets.Scripts.GridNav.NodeOptimizer;

namespace Assets.Scripts.Testing.ProvisTesting {

    public class ProvisIslandNode : IPoiNode<int, int> {
#region Implementation of IPoiNode<int,int>

        public int Id { get; set; }
        public List<int> Neighbor { get; set; } = new List<int>();
        public List<int> Group { get; set; } = new List<int>();
        public IPoiNode<int, int> DeepCopy() {
            ProvisIslandNode ret = new ProvisIslandNode {
                Id = Id
            };
            foreach (var t in Neighbor) ret.Neighbor.Add(t);
            foreach (var t in Group) ret.Group.Add(t);
            return ret;
        }

#endregion

        public AStarFinder<int>.AstarNodeRuntime ToRuntime() {
            AStarFinder<int>.AstarNodeRuntime ret = new AStarFinder<int>.AstarNodeRuntime {
                Id = Id
            };
            foreach (var t in Neighbor) {
                ret.Neighbors.Add(t);
            }
            return ret;
        }
    }

}