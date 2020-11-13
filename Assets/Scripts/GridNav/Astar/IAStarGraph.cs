using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Assets.Scripts.GridNav.Astar {

    

    public interface IAStarGraph<TIdClass>{
        object TicketLock { get; }
        int Ticket { get; set; }

        float GetCost(TIdClass id, TIdClass other);
        float GetHeuristic(TIdClass id, TIdClass target);

        IEnumerable<AStarFinder<TIdClass>.AstarNodeRuntime> GetAllNodes(TIdClass start,TIdClass end);
    }
    
}