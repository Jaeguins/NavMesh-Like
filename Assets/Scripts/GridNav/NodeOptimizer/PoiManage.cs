using System.Collections.Generic;

namespace Assets.Scripts.GridNav.NodeOptimizer {
    /// <summary>
    /// 최적화된 노드 생성 클래스
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    /// <typeparam name="TOptGroup">그룹 기준 클래스</typeparam>
    /// <typeparam name="TNode">노드 클래스</typeparam>
    public interface IPoiGenerator<TIdClass,TOptGroup, TNode> where TNode : IPoiNode<TIdClass,TOptGroup> {
        /// <summary>
        /// 생성하는 노드 조회
        /// </summary>
        /// <returns>노드들</returns>
        List<TNode> GetPoiNodes();
    }
    /// <summary>
    /// 최적화된 노드 그래프 클래스
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    /// <typeparam name="TOptGroup">그룹 기준 클래스</typeparam>
    /// <typeparam name="TNode">노드 클래스</typeparam>
    /// <typeparam name="TNodeGenerator">노드 생성 클래스</typeparam>
    public interface IPoiManager<TIdClass,TOptGroup, TNode, TNodeGenerator> where TNode : IPoiNode<TIdClass,TOptGroup> where TNodeGenerator : IPoiGenerator<TIdClass,TOptGroup, TNode> {
        /// <summary>
        /// 노드 생성기
        /// </summary>
        List<TNodeGenerator> Generators { get; }
        /// <summary>
        /// 생성기에 의존하지 않는 노드들
        /// </summary>
        List<TNode> SpecialNodes { get; }
        /// <summary>
        /// 최적화된 노드 그래프
        /// </summary>
        Dictionary<TIdClass, TNode> OptimizedRuntimeNodes { get; }
        /// <summary>
        /// 간선 연결 가능성 판단
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>두 노드간의 간선 연결 가능 여부</returns>
        bool CanBeEdge(TIdClass a, TIdClass b);
        /// <summary>
        /// 노드 이용 가능성 판단
        /// </summary>
        /// <param name="a">대상 노드</param>
        /// <returns>이용 가능성</returns>
        bool Possible(TIdClass a);
        /// <summary>
        /// 간선 그룹 조회(간선이 없다면 이용되지 않음)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>생성된 두 노드간 간선의 그룹</returns>
        TOptGroup GenerateGroup(TNode start, TNode end);
        /// <summary>
        /// 최적화 필요 여부 판단. sameGroup이 없어지고 end가 들어가야 되는지에 대한 판단
        /// </summary>
        /// <param name="start">시작 노드</param>
        /// <param name="sameGroup">시작 노드에 연결된 같은 그룹의 노드</param>
        /// <param name="end">비교 대상 노드</param>
        /// <returns>가능성</returns>
        bool IsOptimizeNeeded(TIdClass start, TIdClass sameGroup, TIdClass end);
        /// <summary>
        /// 반대 그룹으로 변경-> from->to 그룹에서 to->from 그룹으로
        /// </summary>
        /// <param name="group">그룹</param>
        /// <returns>반대 그룹</returns>
        TOptGroup InverseGroup(TOptGroup group);
    }
    /// <summary>
    /// 최적화된 노드
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    /// <typeparam name="TOptGroup">그룹 기준 클래스</typeparam>
    public interface IPoiNode<TIdClass,TOptGroup> {
        /// <summary>
        /// 키
        /// </summary>
        TIdClass Id { get; }
        /// <summary>
        /// 이웃
        /// </summary>
        List<TIdClass> Neighbor { get; set; }
        /// <summary>
        /// 이웃에 대한 그룹
        /// </summary>
        List<TOptGroup> Group { get; set; }
        /// <summary>
        /// 깊은 복사-그래프의 데이터는 유지하면서 길찾기를 수행하기 위함
        /// </summary>
        /// <returns>복사된 자리</returns>
        IPoiNode<TIdClass,TOptGroup> DeepCopy();
    }
    /// <summary>
    /// 노드 최적화를 위한 정적 클래스
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    /// <typeparam name="TOptGroup">그룹 클래스</typeparam>
    /// <typeparam name="TNode">노드 클래스</typeparam>
    /// <typeparam name="TNodeGenerator">노드 생성 클래스</typeparam>
    public static class PoiNodeManage<TIdClass,TOptGroup, TNode, TNodeGenerator> where TNode : IPoiNode<TIdClass,TOptGroup> where TNodeGenerator : IPoiGenerator<TIdClass,TOptGroup, TNode> {
        /// <summary>
        /// 최적화된 그래프 생성
        /// </summary>
        /// <param name="manager">그래프</param>
        /// <returns>최적화된 그래프</returns>
        public static Dictionary<TIdClass, TNode> BakeOptimized(IPoiManager<TIdClass,TOptGroup, TNode, TNodeGenerator> manager) {
            Dictionary<TIdClass, TNode> ret = new Dictionary<TIdClass, TNode>();
            if (manager.Generators != null)
                foreach (TNodeGenerator generator in manager.Generators) {
                    foreach (TNode node in generator.GetPoiNodes()) {
                        if (ret.ContainsKey(node.Id)) continue;
                        if (!manager.Possible(node.Id)) continue;
                        ret.Add(node.Id, node);
                    }
                }
            if (manager.SpecialNodes != null)
                foreach (var specialNode in manager.SpecialNodes) {
                    if (ret.ContainsKey(specialNode.Id)) continue;
                    if (!manager.Possible(specialNode.Id)) continue;
                    ret.Add(specialNode.Id, specialNode);
                }
            foreach (var start in ret.Values) {
                foreach (var end in ret.Values) {
                    if (start.Id.Equals(end.Id)) continue;
                    if (start.Neighbor.Contains(end.Id)) continue;
                    if (manager.CanBeEdge(start.Id, end.Id)) {
                        TOptGroup group = manager.GenerateGroup(start, end);
                        if (start.Group.Contains(group)) {
                            TIdClass sameGroup = start.Neighbor[start.Group.IndexOf(group)];
                            bool needOptimize= manager.IsOptimizeNeeded(start.Id,sameGroup , end.Id);
                            if (needOptimize) {
                                int index = ret[sameGroup].Neighbor.IndexOf(start.Id);
                                ret[sameGroup].Group.RemoveAt(index);
                                ret[sameGroup].Neighbor.RemoveAt(index);
                                index = start.Neighbor.IndexOf(sameGroup);
                                start.Group.RemoveAt(index);
                                start.Neighbor.RemoveAt(index);
                            } else {
                                continue;
                            }
                        }
                        start.Neighbor.Add(end.Id);
                        start.Group.Add(group);
                        end.Neighbor.Add(start.Id);
                        end.Group.Add(manager.InverseGroup(group));
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// 시작과 끝이 정해진 최적화 그래프 생성
        /// </summary>
        /// <param name="manager">그래프</param>
        /// <param name="start">시작점</param>
        /// <param name="end">도착점</param>
        /// <returns>그래프 노드들</returns>
        public static IEnumerable<TNode> GetWith(IPoiManager<TIdClass,TOptGroup, TNode, TNodeGenerator> manager, TNode start, TNode end) {
            Dictionary<TIdClass, TNode> ret = new Dictionary<TIdClass, TNode>();
            foreach (var t in manager.OptimizedRuntimeNodes.Values) ret.Add(t.Id, (TNode) t.DeepCopy());
            if (!ret.ContainsKey(start.Id)) {
                foreach (var t in ret.Values) {
                    if (manager.CanBeEdge(start.Id, t.Id)) {
                        start.Neighbor.Add(t.Id);
                        t.Neighbor.Add(start.Id);
                    }
                }
                ret.Add(start.Id, start);
            }
            if (!ret.ContainsKey(end.Id)) {
                foreach (var t in ret.Values) {
                    if (manager.CanBeEdge(end.Id, t.Id)) {
                        end.Neighbor.Add(t.Id);
                        t.Neighbor.Add(end.Id);
                    }
                }
                ret.Add(end.Id, end);
            }
            return ret.Values;
        }
    }

}