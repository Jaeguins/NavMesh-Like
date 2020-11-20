using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.GridNav.Astar;
using Assets.Scripts.GridNav.NodeOptimizer;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Testing.ProvisTesting {

    public class ProvisIsland : MonoBehaviour, IAStarGraph<int>, IPoiManager<int, int, ProvisIslandNode, ProvisGrid> {
#region Implementation of IAStarGraph<int>

        public object TicketLock { get; } = new object();
        public int Ticket { get; set; } = 0;
        public float GetCost(int @from, int to) {
            return Vector3.Distance(Grids[from].transform.position, Grids[to].transform.position) + Grids[to].Size.magnitude;
        }
        public float GetHeuristic(int @from, int target) {
            return Vector3.Distance(Grids[from].transform.position, Grids[target].transform.position);
        }
        public IEnumerable<AStarFinder<int>.AstarNodeRuntime> GetAllNodes(int start, int end) {
            ProvisIslandNode tempStart = new ProvisIslandNode {
                                 Id = start
                             },
                             tempEnd = new ProvisIslandNode {
                                 Id = end
                             };
            List<AStarFinder<int>.AstarNodeRuntime> ret = new List<AStarFinder<int>.AstarNodeRuntime>();
            var tempResult = PoiNodeManage<int, int, ProvisIslandNode, ProvisGrid>.GetWith(this, tempStart, tempEnd);
            foreach (var t in tempResult) {
                ret.Add(t.ToRuntime());
            }
            return ret;
        }

#endregion

#region Implementation of IPoiManager<int,int,ProvisIslandNode,IPoiGenerator<int,int,ProvisIslandNode>>

        public List<ProvisGrid> Generators => Grids.Values.ToList();
        public List<ProvisIslandNode> SpecialNodes { get; }
        public Dictionary<int, ProvisIslandNode> OptimizedRuntimeNodes { get; set; } = new Dictionary<int, ProvisIslandNode>();

        public bool CanBeEdge(int a, int b) {
            ProvisGrid gridA = Grids[a],
                       gridB = Grids[b];
            foreach (var t in portals) {
                if (gridA == t.GridA) {
                    if (gridB == t.GridB) return true;
                } else if (gridA == t.GridB) {
                    if (gridB == t.GridA) return true;
                }
            }
            return false;
        }
        public bool Possible(int a) {
            return true;
        }
        public int GenerateGroup(ProvisIslandNode start, ProvisIslandNode end) {
            return end.Id;
        }
        public bool IsOptimizeNeeded(int start, int sameGroup, int end) {
            return false;
        }
        public int InverseGroup(int @group) {
            return -group;
        }

#endregion

        public Dictionary<int, ProvisGrid> Grids;
        public List<GridCoordPair> portals;
        public void Start() {
            Grids = new Dictionary<int, ProvisGrid>();
            foreach (var t in GetComponentsInChildren<ProvisGrid>(true)) {
                Grids.Add(t.Id, t);
            }
        }
        public void OnDrawGizmos() {
            if (DrawJumpPoint) {
                Gizmos.color = Color.green;

                foreach (var p in portals) {
                    if (p.GridA == null || p.GridB == null) continue;
                    Gizmos.matrix = p.GridA.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(p.CoordA.x, .1f, p.CoordA.y), Vector3.one * .95f);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.matrix = p.GridB.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(new Vector3(p.CoordB.x, .1f, p.CoordB.y), Vector3.one * .95f);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.DrawLine(p.GridA.transform.TransformPoint(new Vector3(p.CoordA.x, 0, p.CoordA.y)), p.GridB.transform.TransformPoint(new Vector3(p.CoordB.x, 0, p.CoordB.y)));
                }
            }
            if (DrawGridMap) {

                foreach (var t in Grids.Values) {
                    t.DrawMap();
                }
            }
            if (DrawPath) {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < LastPath.Count - 1; i++) {
                    Vector3 from = Grids[LastPath[i].GridId].transform.TransformPoint(new Vector3(LastPath[i].Coord.x, 1, LastPath[i].Coord.y)),
                            to = Grids[LastPath[i + 1].GridId].transform.TransformPoint(new Vector3(LastPath[i + 1].Coord.x, 1, LastPath[i + 1].Coord.y));
                    Gizmos.DrawLine(from, to);
                }
            }
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(PathInfo.GridA.transform.TransformPoint(new Vector3(PathInfo.CoordA.x, 1, PathInfo.CoordA.y)), 1);
            Gizmos.DrawSphere(PathInfo.GridB.transform.TransformPoint(new Vector3(PathInfo.CoordB.x, 1, PathInfo.CoordB.y)), 1);
        }
        public bool Refresh;
        public bool FindPath;
        public bool DrawGridMap,
                    DrawJumpPoint,
                    DrawPath;
        public GridCoordPair PathInfo;
        public List<GridCoord> LastPath = new List<GridCoord>();
        public void Update() {
            if (FindPath) {
                FindingPathAsync();
                FindPath = false;
            }
            if (Refresh) {
                RefreshAsync();
                Refresh = false;
            }
        }
        public async Task RefreshAsync() {
            try {
                var toChange = PoiNodeManage<int, int, ProvisIslandNode, ProvisGrid>.BakeOptimized(this);

                await AStarFinder<int>.RefreshFieldAsync(this, () => {
                    OptimizedRuntimeNodes = toChange;
                });
                foreach (var g in Grids.Values) await g.RefreshTask();
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
        public async Task FindingPathAsync() {
            LastPath.Clear();
            try {
                Stack<int> ret = await AStarFinder<int>.GetPath(this, PathInfo.GridA.Id, PathInfo.GridB.Id);
                List<int> gridWays = new List<int>();
                if (ret == null) return;
                while (ret.Count > 0) {
                    gridWays.Add(ret.Pop());
                }
                GridCoord lastPoint = new GridCoord {
                    GridId = gridWays.First(),
                    Coord = PathInfo.CoordA
                };
                Task<Stack<Vector2Int>>[] innerGridPathTask = new Task<Stack<Vector2Int>>[gridWays.Count];
                for (int i = 0; i < gridWays.Count; i++) {
                    int nowGrid = gridWays[i],
                        nextGrid = i == gridWays.Count - 1 ? -1 : gridWays[i + 1];
                    var tempNeighbors = GetNeighborPortal(nowGrid, nextGrid);
                    GridCoordPair toNextPortal = tempNeighbors[Random.Range(0,tempNeighbors.Count)];
                    innerGridPathTask[i] = toNextPortal.GridA.FindPathAsync(lastPoint.Coord, (i != (gridWays.Count - 1)) ? toNextPortal.CoordA : PathInfo.CoordB);
                    lastPoint=new GridCoord{Coord=toNextPortal.CoordB,GridId = toNextPortal.GridB.Id};
                }
                for (int j = 0; j < gridWays.Count; j++) {
                    Stack<Vector2Int> innerGridPath = await innerGridPathTask[j];
                    while (innerGridPath.Count > 0) {
                        LastPath.Add(new GridCoord() {
                            Coord = innerGridPath.Pop(),
                            GridId = gridWays[j]
                        });
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
        public List<GridCoordPair> GetNeighborPortal(int id, int targetId = -1) {
            List<GridCoordPair> ret = new List<GridCoordPair>();
            foreach (var t in portals) {
                if (t.GridA.Id == id) {
                    if (targetId == -1 || targetId == t.GridB.Id)
                        ret.Add(new GridCoordPair {
                            GridA = t.GridA,
                            GridB = t.GridB,
                            CoordA = t.CoordA,
                            CoordB = t.CoordB
                        });
                } else if (t.GridB.Id == id) {
                    if (targetId == -1 || targetId == t.GridA.Id)
                        ret.Add(new GridCoordPair {
                            GridB = t.GridA,
                            GridA = t.GridB,
                            CoordB = t.CoordA,
                            CoordA = t.CoordB
                        });
                }
            }
            return ret;
        }
    }

    [Serializable]
    public struct GridCoord {
        public int GridId;
        public Vector2Int Coord;
    }

}