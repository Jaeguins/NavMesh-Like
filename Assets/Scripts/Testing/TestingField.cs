using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.GridNav.Astar;
using Assets.Scripts.GridNav.NodeOptimizer;
using UnityEngine;

namespace Assets.Scripts.Testing {

    public class TestingField : MonoBehaviour, IAStarGraph<Vector2Int>, IPoiManager<Vector2Int,Vector2, TestingNode, TestingObstacle> {
        private Dictionary<Vector2Int, TestingNode> _field = new Dictionary<Vector2Int, TestingNode>();

        public TestingNode GetNode(Vector2Int id) {
            if (!OptimizedRuntimeNodes.ContainsKey(id)) {
                OptimizedRuntimeNodes.Add(id, new TestingNode() {
                    Id = id,
                    Neighbors = new List<Vector2Int>(new[] {id + Vector2Int.up, id + Vector2Int.down, id + Vector2Int.right, id + Vector2Int.left})
                });
            }
            return OptimizedRuntimeNodes[id];
        }
        public bool Possible(Vector2 pos) => Possible(new Vector2Int((int)pos.x, (int)pos.y));
        public Vector2 GenerateGroup(TestingNode start, TestingNode end) {
            return ((Vector2) (start.Id - end.Id)).normalized;
        }
        public bool IsOptimizeNeeded(Vector2Int start, Vector2Int sameGroup, Vector2Int end) {
            float startToEnd = Vector2.Distance(start, end),
                  startToSame = Vector2.Distance(start, sameGroup);
            return (startToSame > startToEnd);
        }
        public Vector2 InverseGroup(Vector2 group) {
            return -group;
        }
        public bool Possible(Vector2Int pos) {
            if(!new RectInt(Vector2Int.zero,Size).Contains(pos))return false;
            foreach (var t in _generators) {
                if (pos.x <= t.Area.xMax && pos.x >= t.Area.xMin && pos.y >= t.Area.yMin && pos.y <= t.Area.yMax) return false;
            }
            return true;
        }

#region FlagSwitch

        public bool FindPath = false;
        public bool RefreshMap = false;

#endregion

#region OnlyForTestingField

        public Collider Col;
        public Vector2Int StartNode,
                          EndNode;
        public List<Vector2Int> LastPath = new List<Vector2Int>();
        public Vector2Int Size;

        private Vector2Int _lastClick = -Vector2Int.one;

        public void Update() {
            
            if (Input.GetMouseButtonDown(0)) {
                RaycastHit hit;
                Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000f);
                Vector2Int clickedPos = new Vector2Int((int) hit.point.x, (int) hit.point.z);
                if (hit.collider == Col) {
                    StartNode = clickedPos;
                }
            }
            if (Input.GetMouseButtonDown(1)) {
                RaycastHit hit;
                Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000f);
                Vector2Int clickedPos = new Vector2Int((int) hit.point.x, (int) hit.point.z);
                if (hit.collider == Col) {
                    EndNode= clickedPos;
                }
            }
            if (FindPath) {
                FindPathAsync();
                FindPath = false;
            }
            if (RefreshMap) {
                RefreshAsync();
                RefreshMap = false;
            }
        }
        public float edgeDivider = .1f;
        public void OnDrawGizmos() {
            if (OptimizedRuntimeNodes != null) {
                for (int i = 0; i <= Size.x; i++)
                for (int j = 0; j <= Size.y; j++) {
                    Vector2Int pos = new Vector2Int(i, j);
                    Gizmos.color = !Possible(pos)?Color.red:(!OptimizedRuntimeNodes.ContainsKey(pos) ? Color.yellow : Color.blue);
                    Gizmos.DrawCube(new Vector3(pos.x, 0, pos.y), new Vector3(.99f, .01f, .99f));
                }
                Gizmos.color = Color.yellow;
                foreach (var t in OptimizedRuntimeNodes.Values) {
                    foreach (var tt in t.Neighbor) {

                        float dist = Vector2.Distance(t.Id, tt);
                        Vector3 start = new Vector3(t.Id.x, dist*edgeDivider, t.Id.y),
                                end = new Vector3(tt.x, dist*edgeDivider, tt.y);
                        Gizmos.DrawLine(start, end);
                    }
                }
            }
            Gizmos.color = Color.cyan;
            if (LastPath != null && LastPath.Count > 1) {
                for (int i = 1; i < LastPath.Count; i++) {
                    Vector2Int start2 = LastPath[i - 1],
                               end2 = LastPath[i];
                    float dist = Vector2.Distance(start2, end2);
                    Vector3 start = new Vector3(start2.x, dist*edgeDivider, start2.y),
                            end = new Vector3(end2.x, dist*edgeDivider, end2.y);
                    Gizmos.DrawLine(start, end);
                }
            }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(new Vector3(StartNode.x, .1f, StartNode.y), .5f);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(EndNode.x, .1f, EndNode.y), .5f);
        }
        public async Task FindPathAsync() {
            try {
            LastPath.Clear();
            var task = AStarFinder<Vector2Int>.GetPath(this, StartNode, EndNode);
            await task;

            Stack<Vector2Int> ret = task.Result;
            if (ret == null) return;
            while (ret.Count > 0) {
                LastPath.Add(ret.Pop());
            }
            }
            catch (Exception e) {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
            }
        }

        public async Task RefreshAsync() {
            try {
                var toChange = PoiNodeManage<Vector2Int,Vector2, TestingNode, TestingObstacle>.BakeOptimized(this);

                await AStarFinder<Vector2Int>.RefreshFieldAsync(this, () => {
                    OptimizedRuntimeNodes = toChange;
                });
            }
            catch (Exception e) {
                Debug.LogError($"{e.Message}\n{e.StackTrace}");
            }
        }

#endregion

#region Implementation of IAstarGraph<Vector2Int,TestingNode>

        public object TicketLock { get; } = new object();
        public int Ticket { get; set; } = 0;

        public IEnumerable<AStarFinder<Vector2Int>.AstarNodeRuntime> GetAllNodes(Vector2Int start, Vector2Int end) {
            TestingNode tempStart = new TestingNode {
                            Id = start
                        },
                        tempEnd = new TestingNode {
                            Id = end
                        };
            List<AStarFinder<Vector2Int>.AstarNodeRuntime> ret = new List<AStarFinder<Vector2Int>.AstarNodeRuntime>();
            var tempResult = PoiNodeManage<Vector2Int,Vector2, TestingNode, TestingObstacle>.GetWith(this, tempStart, tempEnd);
            foreach (var t in tempResult) {
                ret.Add(t.ToRuntimeNode());
            }
            return ret;
        }

        public float GetCost(Vector2Int from, Vector2Int to) {
            return Vector2Int.Distance(from, to);
        }
        public float GetHeuristic(Vector2Int from, Vector2Int target) {
            return Vector2Int.Distance(from, target);
        }

#endregion

#region Implementation of IPoiManager<Vector2Int,TestingNode>

        public List<TestingObstacle> Generators {
            get => _generators;
            set => _generators = value;
        }
        [SerializeField]
        private List<TestingObstacle> _generators;
        public List<TestingNode> SpecialNodes { get; }
        public Dictionary<Vector2Int, TestingNode> OptimizedRuntimeNodes {
            get => _field;
            set => _field = value;
        }

        public bool CanBeEdge(Vector2Int a, Vector2Int b) {
            
            if(!Possible(a)||!Possible(b))return false;

            for (float t = 0f; t < 1; t += .01f) {
                if (!Possible(Vector2.Lerp(a, b, t))) return false;
            }
            
            return true;
        }

#endregion
    }

    public class TestingNode : IPoiNode<Vector2Int,Vector2> {
#region Implementation of IAstarNode<Vector2Int>

        public bool GoodToBePath = true;
        public Vector2Int Id { get; set; }

        public List<Vector2Int> Neighbors = new List<Vector2Int>();
        public AStarFinder<Vector2Int>.AstarNodeRuntime ToRuntimeNode() {
            AStarFinder<Vector2Int>.AstarNodeRuntime ret = new AStarFinder<Vector2Int>.AstarNodeRuntime() {
                Id = Id,
                GoodToBePath = GoodToBePath,
                Neighbors = new List<Vector2Int>()
            };
            foreach (var t in Neighbors) {
                ret.Neighbors.Add(t);
            }
            return ret;
        }

#endregion

#region Implementation of IPoiNode<Vector2Int>

        public List<Vector2Int> Neighbor {
            get => Neighbors;
            set => Neighbors = value;
        }
        public List<Vector2> Group { get; set; }=new List<Vector2>();
        public IPoiNode<Vector2Int,Vector2> DeepCopy() {
            return new TestingNode() {
                GoodToBePath = GoodToBePath,
                Id = Id,
                Neighbor = new List<Vector2Int>(Neighbor)
            };
        }

#endregion
    }

    [Serializable]
    public class TestingObstacle : IPoiGenerator<Vector2Int,Vector2, TestingNode> {
        public RectInt Area;

#region Implementation of IPoiGenerator<Vector2Int,TestingNode>

        public List<TestingNode> GetPoiNodes() {
            List<TestingNode> ret = new List<TestingNode> {
                new TestingNode {
                    Id = new Vector2Int {
                        x = Area.xMin - 1,
                        y = Area.yMin - 1
                    }
                },
                new TestingNode {
                    Id = new Vector2Int {
                        x = Area.xMax + 1,
                        y = Area.yMin - 1
                    }
                },
                new TestingNode {
                    Id = new Vector2Int {
                        x = Area.xMin - 1,
                        y = Area.yMax + 1
                    }
                },
                new TestingNode {
                    Id = new Vector2Int {
                        x = Area.xMax + 1,
                        y = Area.yMax + 1
                    }
                }
            };

            return ret;
        }

#endregion
    }

}