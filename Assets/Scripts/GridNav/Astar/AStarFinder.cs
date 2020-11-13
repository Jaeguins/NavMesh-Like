using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Scripts.GridNav.Astar {
    /// <summary>
    /// AStar 연산을 위한 정적 클래스
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    public static class AStarFinder<TIdClass> {
        /// <summary>
        /// 비동기 경로 계산
        /// </summary>
        /// <param name="graph">그래프</param>
        /// <param name="startNode">시작노드</param>
        /// <param name="endNode">목표 노드</param>
        /// <returns>경로</returns>
        public static async Task<Stack<TIdClass>> GetPath(IAStarGraph<TIdClass> graph, TIdClass startNode, TIdClass endNode) {
            
            

            Stack<TIdClass> path = new Stack<TIdClass>();
            List<AstarNodeRuntime> openList = new List<AstarNodeRuntime>(),
                                   closedList = new List<AstarNodeRuntime>(),
                                   adjacencies;

            Dictionary<TIdClass, AstarNodeRuntime> runFields = new Dictionary<TIdClass, AstarNodeRuntime>();
            while (graph.Ticket < 0) await Task.Yield();
            lock (graph.TicketLock) {
                graph.Ticket += 1;
            }
            foreach (var t in graph.GetAllNodes(startNode, endNode)) {
                runFields.Add(t.Id, t);
            }
            lock (graph.TicketLock) {
                graph.Ticket -= 1;
            }
            AstarNodeRuntime start = runFields[startNode],
                             current = start;
            start.Parent = null;
            openList.Add(start);
            int infinityBroker = 0;
            while (infinityBroker++ < 1000 && openList.Count != 0 && !closedList.Exists(x => x.Id.Equals(endNode))) {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                List<AstarNodeRuntime> tempAdj = new List<AstarNodeRuntime>();
                foreach (var t in runFields[current.Id].Neighbors) tempAdj.Add(runFields[t]);
                adjacencies = tempAdj;
                foreach (AstarNodeRuntime n in adjacencies) {
                    if (!closedList.Contains(n) && n.GoodToBePath) {
                        if (!openList.Contains(n)) {
                            n.Parent = current;
                            n.DistanceToTarget = n.GetHeuristic(graph, endNode);
                            n.Cost = n.GetCost(graph, n.Parent.Id) + n.Parent.Cost;
                            openList.Add(n);
                            openList = openList.OrderBy(t => t.CalculateF()).ToList();
                        }
                    }
                }
            }


            AstarNodeRuntime temp = closedList[closedList.IndexOf(current)];
            if (!temp.Id.Equals(endNode))
            {
                temp = closedList.OrderBy(t => t.GetHeuristic(graph,endNode)).First();
            }

            
            
            if (temp == null) return null;
            do {
                path.Push(temp.Id);
                temp = temp.Parent;
            } while (temp != null);

            
            return path;
        }
        
        public delegate void ChangeAction();
        /// <summary>
        /// 비동기 그래프 갱신
        /// </summary>
        /// <param name="graph">그래프</param>
        /// <param name="action">갱신 액션</param>
        /// <returns></returns>
        public static async Task RefreshFieldAsync(IAStarGraph<TIdClass> graph,ChangeAction action) {
            while (graph.Ticket > 0) {
                await Task.Yield();
            }
            lock (graph.TicketLock) {
                graph.Ticket = -1;
            }
            action();
            lock (graph.TicketLock) {
                graph.Ticket = 0;
            }
        }
        /// <summary>
        /// 길찾기 자체를 위한 컨테이너 클래스
        /// </summary>
        public class AstarNodeRuntime {
            /// <summary>
            /// 노드 아이디
            /// </summary>
            public TIdClass Id;
            public bool GoodToBePath;

            internal AstarNodeRuntime Parent;
            internal float DistanceToTarget;
            internal float Cost;
            /// <summary>
            /// 현재로부터 비용 검색
            /// </summary>
            /// <param name="graph">대상 그래프</param>
            /// <param name="other">대상 ID</param>
            /// <returns>비용</returns>
            internal float GetCost(IAStarGraph<TIdClass> graph, TIdClass other) {
                return graph.GetCost(Id, other);
            }
            /// <summary>
            /// 기댓값 검색
            /// </summary>
            /// <param name="graph">대상 그래프</param>
            /// <param name="target">대상 ID</param>
            /// <returns>기댓값</returns>
            internal float GetHeuristic(IAStarGraph<TIdClass> graph, TIdClass target) {
                return graph.GetHeuristic(Id, target);
            }
            /// <summary>
            /// 이웃
            /// </summary>
            internal List<TIdClass> Neighbors;
            /// <summary>
            /// 종합 노드 판단 기준 도출
            /// </summary>
            /// <returns>도출값</returns>
            internal float CalculateF() {
                if (DistanceToTarget > 0 && Cost > 0) return DistanceToTarget + Cost;
                return float.PositiveInfinity;
            }
        }
    }

}