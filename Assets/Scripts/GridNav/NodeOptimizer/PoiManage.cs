using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GridNav.NodeOptimizer {

    public interface IPoiGenerator<TIdClass,TOptGroup, TNode> where TNode : IPoiNode<TIdClass,TOptGroup> {
        List<TNode> GetPoiNodes();
    }

    public interface IPoiManager<TIdClass,TOptGroup, TNode, TNodeGenerator> where TNode : IPoiNode<TIdClass,TOptGroup> where TNodeGenerator : IPoiGenerator<TIdClass,TOptGroup, TNode> {
        List<TNodeGenerator> Generators { get; }
        List<TNode> SpecialNodes { get; }
        Dictionary<TIdClass, TNode> OptimizedRuntimeNodes { get; set; }
        bool CanBeEdge(TIdClass a, TIdClass b);
        bool Possible(TIdClass a);
        TOptGroup GenerateGroup(TNode start, TNode end);
        bool IsOptimizeNeeded(TIdClass start, TIdClass sameGroup, TIdClass end);
        TOptGroup InverseGroup(TOptGroup group);
    }
    
    public interface IPoiNode<TIdClass,TOptGroup> {
        TIdClass Id { get; }
        List<TIdClass> Neighbor { get; set; }
        List<TOptGroup> Group { get; set; }
        IPoiNode<TIdClass,TOptGroup> DeepCopy();
    }

    public static class PoiNodeManage<TIdClass,TOptGroup, TNode, TNodeGenerator> where TNode : IPoiNode<TIdClass,TOptGroup> where TNodeGenerator : IPoiGenerator<TIdClass,TOptGroup, TNode> {
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