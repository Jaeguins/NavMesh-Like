using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Assets.Scripts.GridNav.Astar {

    

    public interface IAStarGraph<TIdClass> {
        object TicketLock { get; }
        int Ticket { get; set; }

        AStarFinder<TIdClass>.AstarNodeRuntime GetRuntimeNode(TIdClass id);
        float GetCost(TIdClass id, TIdClass other);
        float GetHeuristic(TIdClass id, TIdClass target);
        void ChangeField(Dictionary<TIdClass, IAStarNode<TIdClass>> newField);
    }
    public interface IAStarNode<TIdClass> {
        TIdClass Id { get; }
    }
}