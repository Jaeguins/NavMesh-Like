using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.GridNav.Astar;
using Assets.Scripts.GridNav.NodeOptimizer;
using UnityEngine;

namespace Assets.Scripts.Testing.ProvisTesting {

    public class ProvisGrid : MonoBehaviour, IAStarGraph<Vector2Int>, IPoiManager<Vector2Int, Vector2, SearchNode, ProvisFurniture>, IPoiGenerator<int, int, ProvisIslandNode> {
        public int Id;
        public ProvisIsland Manager;

#region Implementation of IAStarGraph<Vector2Int>

        public object TicketLock { get; } = new object();
        public int Ticket { get; set; } = 0;
        public float GetCost(Vector2Int @from, Vector2Int to) {
            return Vector2Int.Distance(from, to);
        }
        public float GetHeuristic(Vector2Int @from, Vector2Int target) {
            return Vector2Int.Distance(from, target);
        }
        public IEnumerable<AStarFinder<Vector2Int>.AstarNodeRuntime> GetAllNodes(Vector2Int start, Vector2Int end) {
            SearchNode tempStart = new SearchNode {
                           Id = start
                       },
                       tempEnd = new SearchNode {
                           Id = end
                       };
            List<AStarFinder<Vector2Int>.AstarNodeRuntime> ret = new List<AStarFinder<Vector2Int>.AstarNodeRuntime>();
            var tempResult = PoiNodeManage<Vector2Int, Vector2, SearchNode, ProvisFurniture>.GetWith(this, tempStart, tempEnd);
            foreach (var t in tempResult) {
                ret.Add(t.ToRuntimeNode());
            }
            return ret;
        }

#endregion

#region Implementation of IPoiManager<Vector2Int,Vector2,SearchNode,ProvisFurniture>

        public List<ProvisFurniture> Generators {
            get => Furnitures;
            set => Furnitures = value;
        }
        public List<ProvisFurniture> Furnitures = new List<ProvisFurniture>();
        public List<SearchNode> SpecialNodes { get; set; } = new List<SearchNode>();
        public Dictionary<Vector2Int, SearchNode> OptimizedRuntimeNodes { get; set; } = new Dictionary<Vector2Int, SearchNode>();
        public bool CanBeEdge(Vector2Int a, Vector2Int b) {
            if (!Possible(a) || !Possible(b)) return false;

            for (float t = 0f; t < 1; t += .01f) {
                if (!Possible(Vector2.Lerp(a, b, t))) return false;
            }

            return true;
        }
        public bool Possible(Vector2Int pos) {
            if (!new RectInt(-Size/2, Size).Contains(pos)) return false;
            foreach (var t in Furnitures) {
                if (pos.x <= t.Area.xMax && pos.x >= t.Area.xMin && pos.y >= t.Area.yMin && pos.y <= t.Area.yMax) return false;
            }
            return true;
        }
        public bool Possible(Vector2 pos) => Possible(new Vector2Int {
            x = (int) pos.x,
            y = (int) pos.y
        });
        public Vector2 GenerateGroup(SearchNode start, SearchNode end) {
            return ((Vector2) (start.Id - end.Id)).normalized;
        }
        public bool IsOptimizeNeeded(Vector2Int start, Vector2Int sameGroup, Vector2Int end) {
            return (Vector2Int.Distance(start, end) < Vector2Int.Distance(start, sameGroup));
        }
        public Vector2 InverseGroup(Vector2 @group) {
            return -group;
        }

#endregion

        public Vector2Int Size;

#region Implementation of IPoiGenerator<int,int,ProvisIslandNode>

        public List<ProvisIslandNode> GetPoiNodes() {
            return new List<ProvisIslandNode> {
                new ProvisIslandNode {
                    Id = Id
                }
            };
        }

#endregion

        public async Task RefreshTask() {
            SpecialNodes.Clear();
            foreach (var t in Manager.GetNeighborPortal(Id)) {
                SpecialNodes.Add(new SearchNode(){Id=t.CoordA});
            }
            var toChange = PoiNodeManage<Vector2Int,Vector2, SearchNode, ProvisFurniture>.BakeOptimized(this);

            await AStarFinder<Vector2Int>.RefreshFieldAsync(this, () => {
                OptimizedRuntimeNodes = toChange;
            });
        }
        public async Task<Stack<Vector2Int>> FindPathAsync(Vector2Int start, Vector2Int end) {
            return await AStarFinder<Vector2Int>.GetPath(this, start, end);
        }

        public void OnDrawGizmos() {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero,new Vector3(Size.x,0f,Size.y));
            foreach (var t in Furnitures) {
                Gizmos.DrawCube(new Vector3(t.Area.center.x, 0, t.Area.center.y), new Vector3(t.Area.size.x,0,t.Area.size.y));
            }
            
            foreach (var t in OptimizedRuntimeNodes.Values) {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(new Vector3(t.Id.x,.05f,t.Id.y),Vector3.one*.8f );
                Gizmos.color = Color.yellow;
                foreach (var l in t.Neighbor) {
                    Gizmos.DrawLine(new Vector3(t.Id.x,.05f,t.Id.y),new Vector3(l.x,.05f,l.y));
                }
            }
            Gizmos.matrix=Matrix4x4.identity;
        }
    }

}