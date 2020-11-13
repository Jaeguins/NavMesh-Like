using System.Collections.Generic;

namespace Assets.Scripts.GridNav.Astar {

    
    /// <summary>
    /// AStar 계산을 위한 인터페이스
    /// </summary>
    /// <typeparam name="TIdClass">키 클래스</typeparam>
    public interface IAStarGraph<TIdClass>{
        /// <summary>
        /// 참조 카운트 락
        /// </summary>
        object TicketLock { get; }
        /// <summary>
        /// 참조 카운트
        /// </summary>
        int Ticket { get; set; }
        /// <summary>
        /// 비용 계산
        /// </summary>
        /// <param name="from">출발노드</param>
        /// <param name="to">도착노드</param>
        /// <returns>비용</returns>
        float GetCost(TIdClass from, TIdClass to);
        /// <summary>
        /// 기댓값 계산
        /// </summary>
        /// <param name="from">기준노드</param>
        /// <param name="target">목표노드</param>
        /// <returns>기댓값</returns>
        float GetHeuristic(TIdClass from, TIdClass target);
        /// <summary>
        /// 모든 노드 얻기
        /// </summary>
        /// <param name="start">출발노드</param>
        /// <param name="end">도착노드</param>
        /// <returns>모든 노드를 담은 컨테이너</returns>
        IEnumerable<AStarFinder<TIdClass>.AstarNodeRuntime> GetAllNodes(TIdClass start,TIdClass end);
    }
    
}