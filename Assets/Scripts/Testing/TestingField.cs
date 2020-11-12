using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.GridNav.Astar;
using UnityEngine;

namespace Assets.Scripts.Testing {

    public class TestingField : MonoBehaviour, IAStarGraph<Vector2Int> {
        public Collider Col;

#region Implementation of IAstarGraph<Vector2Int>

        public object TicketLock { get; } = new object();
        public int Ticket { get; set; } = 0;
        public Dictionary<Vector2Int, TestingNode> Field = new Dictionary<Vector2Int, TestingNode>();
        public void ChangeField(Dictionary<Vector2Int, IAStarNode<Vector2Int>> newField) {
            Field.Clear();
            foreach (var t in newField) Field.Add(t.Key, (TestingNode) t.Value);
        }
        public TestingNode GetNode(Vector2Int id) {
            if (!Field.ContainsKey(id)) {
                Field.Add(id, new TestingNode() {
                    Id = id,
                    Neighbors = new List<Vector2Int>(new[] {id + Vector2Int.up, id + Vector2Int.down, id + Vector2Int.right, id + Vector2Int.left})
                });
            }
            return Field[id];
        }
        public AStarFinder<Vector2Int>.AstarNodeRuntime GetRuntimeNode(Vector2Int id) {
            return GetNode(id).ToRuntimeNode();
        }

        public float GetCost(Vector2Int id, Vector2Int other) {
            return Vector2Int.Distance(id, other);
        }
        public float GetHeuristic(Vector2Int id, Vector2Int target) {
            return Vector2Int.Distance(id, target);
        }
        public bool FindPath = false;
        public Vector2Int StartNode,
                          EndNode;
        public List<Vector2Int> LastPath = new List<Vector2Int>();
        public Vector2Int Size;
        private Vector2Int _lastClick = -Vector2Int.one;
        public void Start() {
            for (int i = 0; i <= Size.x; i++) {
                GetNode(new Vector2Int(i, 0)).GoodToBePath = false;
                GetNode(new Vector2Int(i, Size.y)).GoodToBePath = false;
            }
            for (int i = 0; i <= Size.y; i++) {
                GetNode(new Vector2Int(0, i)).GoodToBePath = false;
                GetNode(new Vector2Int(Size.x, i)).GoodToBePath = false;
            }
        }
        public void Update() {
            if (Input.GetMouseButton(0)) {
                RaycastHit hit;
                Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000f);
                if (hit.collider == Col) {
                    Vector2Int clickedPos = new Vector2Int((int) hit.point.x, (int) hit.point.z);
                    if (_lastClick != clickedPos) {
                        TestingNode t = GetNode(clickedPos);
                        t.GoodToBePath = !t.GoodToBePath;
                        _lastClick = clickedPos;
                    }
                }
            }
            if (FindPath) {
                FindPathAsync();
                FindPath = false;
            }
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
        public void OnDrawGizmos() {
            foreach (var t in Field) {
                Gizmos.color = t.Value.GoodToBePath ? Color.blue : Color.red;
                Gizmos.DrawCube(new Vector3(t.Key.x, 0, t.Key.y), new Vector3(.99f, .01f, .99f));
            }
            Gizmos.color = Color.cyan;
            if (LastPath != null && LastPath.Count > 1) {
                for (int i = 1; i < LastPath.Count; i++) {
                    Vector2Int start2 = LastPath[i - 1],
                               end2 = LastPath[i];
                    Vector3 start = new Vector3(start2.x, .1f, start2.y),
                            end = new Vector3(end2.x, .1f, end2.y);
                    Gizmos.DrawLine(start, end);
                }
            }
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(new Vector3(StartNode.x, .1f, StartNode.y), .5f);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(EndNode.x, .1f, EndNode.y), .5f);
        }

#endregion
    }

    public class TestingNode : IAStarNode<Vector2Int> {
#region Implementation of IAstarNode<Vector2Int>

        public bool GoodToBePath = true;
        public Vector2Int Id { get; set; }

        public List<Vector2Int> Neighbors;
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
    }

}