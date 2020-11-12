using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Scripts.GridNav.Astar {

    public static class AStarFinder<TIdClass> {
        public static async Task<Stack<TIdClass>> GetPath(IAStarGraph<TIdClass> graph, TIdClass startNode, TIdClass endNode) {
            while (graph.Ticket < 0) await Task.Yield();
            lock (graph.TicketLock) {
                graph.Ticket += 1;
            }



            AstarNodeRuntime start = graph.GetRuntimeNode(startNode),
                             current = start;
            TIdClass end = endNode;

            Stack<TIdClass> path = new Stack<TIdClass>();
            List<AstarNodeRuntime> openList = new List<AstarNodeRuntime>(),
                                   closedList = new List<AstarNodeRuntime>(),
                                   adjacencies;
            Dictionary<TIdClass, AstarNodeRuntime> runFields = new Dictionary<TIdClass, AstarNodeRuntime>();
            
            openList.Add(start);
            int infinityBroker = 0;
            while (infinityBroker++<1000&&openList.Count != 0 && !closedList.Exists(x => x.Id.Equals(end))) {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                adjacencies = GetNeighbors(graph, runFields, current.Id);
                foreach (AstarNodeRuntime n in adjacencies) {
                    if (!closedList.Contains(n) && n.GoodToBePath) {
                        if (!openList.Contains(n)) {
                            n.Parent = current;
                            n.DistanceToTarget = n.GetHeuristic(graph, end);
                            n.Cost = n.GetCost(graph, n.Parent.Id) + n.Parent.Cost;
                            openList.Add(n);
                            openList = openList.OrderBy(t=>t.CalculateF()).ToList();
                        }
                    } else {
                        infinityBroker -= 1;
                    }
                }
            }


            // construct path, if end was not closed return null
            if (!closedList.Exists(x => x.Id.Equals(end))) {
                return null;
            }

            // if all good, return path
            AstarNodeRuntime temp = closedList[closedList.IndexOf(current)];
            if (temp == null) return null;
            do {
                path.Push(temp.Id);
                temp = temp.Parent;
            } while (temp != start && temp != null);

            lock (graph.TicketLock) {
                graph.Ticket -= 1;
            }
            return path;
        }
        private static List<AstarNodeRuntime> GetNeighbors(IAStarGraph<TIdClass> graph, Dictionary<TIdClass, AstarNodeRuntime> runtimeField, TIdClass currentId) {
            if (!runtimeField.ContainsKey(currentId)) runtimeField.Add(currentId, graph.GetRuntimeNode(currentId));
            List<AstarNodeRuntime> ret = new List<AstarNodeRuntime>();
            foreach (var t in runtimeField[currentId].Neighbors) {
                if (!runtimeField.ContainsKey(t)) runtimeField.Add(t, graph.GetRuntimeNode(t));
                ret.Add(runtimeField[t]);
            }
            return ret;
        }
        

        public static async Task RefreshFieldAsync(IAStarGraph<TIdClass> graph, Dictionary<TIdClass, IAStarNode<TIdClass>> newField) {
            while (graph.Ticket > 0) {
                await Task.Yield();
            }
            lock (graph.TicketLock) {
                graph.Ticket = -1;
            }
            graph.ChangeField(newField);
            lock (graph.TicketLock) {
                graph.Ticket = 0;
            }
        }

        public class AstarNodeRuntime {
            public TIdClass Id;
            public bool GoodToBePath;


            public AstarNodeRuntime Parent;
            public float DistanceToTarget;
            public float Cost;
            public float GetCost(IAStarGraph<TIdClass> graph, TIdClass other) {
                return graph.GetCost(Id, other);
            }
            public float GetHeuristic(IAStarGraph<TIdClass> graph, TIdClass target) {
                return graph.GetHeuristic(Id, target);
            }
            public List<TIdClass> Neighbors;
            public float CalculateF() {
                if (DistanceToTarget > 0 && Cost > 0) return DistanceToTarget + Cost;
                return -1;
            }
        }
    }

}