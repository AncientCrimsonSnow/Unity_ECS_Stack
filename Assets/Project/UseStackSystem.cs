using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Project.Extensions;

namespace Project
{
    [AlwaysUpdateSystem]
    public partial class UseStackSystem : SystemBase
    {
        private const int StackSize = 10;
        private const int MaxEntitiesPerChunk = 2040;

        private EndSimulationEntityCommandBufferSystem _ecbs;
        private EntityCommandBuffer.ParallelWriter _ecb;
        
        private readonly ECS_Stack _stack = new ECS_Stack();
        private IEnumerator _textStack;
        private EntityQuery _nodeDataQuery;

        protected override void OnCreate()
        {
            _ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            _textStack = TestStack().GetEnumerator();
            
            _nodeDataQuery = GetEntityQuery(ComponentType.ReadOnly<NodeData>());
        }

        protected override void OnUpdate()
        {
            _ecb = _ecbs.CreateCommandBuffer().AsParallelWriter();
            _textStack.MoveNext();
        }
        
        private IEnumerable TestStack()
        {
            FillStack();
            yield return null;
            PrintStack();
            SelectEntitiesForHead();
            yield return null;
            MoveSelectedEntitiesToHead();
            yield return null;
            PrintStack();
            SwitchNodes(new NodeData{index = 3}, new NodeData{index = 4});
            yield return null;
            PrintStack();
            ClearStack();
            yield return null;
            PrintStack();
        }

        private void FillStack()
        {
            for (var i = 0; i != StackSize - 1; i++)
            {
                var component = new NodeData { index = i};
                _stack.Push(component);
                for (var j = 0; j != MaxEntitiesPerChunk; j++)
                {
                    var entity = _ecb.CreateEntity(j);
                    _ecb.AddSharedComponent(j, entity, component);
                }
            }
        }

        private void SelectEntitiesForHead()
        {
            var lastNodeData = new NodeData { index = StackSize - 1 };
            _stack.Push(lastNodeData);
            
            _nodeDataQuery.ResetFilter();
            var entities = _nodeDataQuery.ToEntityArray(Allocator.Temp);
            var indices = new int[entities.Length];
            for (var i = 0; i != indices.Length; i++)
                indices[i] = i;

            Shuffle(indices);
            for (var i = 0; i != MaxEntitiesPerChunk; i++)
            {
                _ecb.RemoveComponent<NodeData>(indices[i], entities[indices[i]]);
                _ecb.AddComponent<MoveToHeadTag>(indices[i], entities[indices[i]]);
            }
        }

        private void MoveSelectedEntitiesToHead()
        {
            var lastNodeData = new NodeData { index = StackSize - 1 };
            var query = GetEntityQuery(ComponentType.ReadOnly<MoveToHeadTag>());
            var entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i != entities.Length; i++)
            {
                _ecb.RemoveComponent<MoveToHeadTag>(i, entities[i]);
                _ecb.AddSharedComponent(i, entities[i], lastNodeData);
            }
        }

        private void SwitchNodes(NodeData node1, NodeData node2)
        {
            var query = GetEntityQuery(ComponentType.ReadOnly<NodeData>());
            query.SetSharedComponentFilter(node1);
            var entities1 = query.ToEntityArray(Allocator.Temp);
            query.SetSharedComponentFilter(node2);
            var entities2 = query.ToEntityArray(Allocator.Temp);

            _ecb.SetSharedComponent(0, entities1, node2);
            _ecb.SetSharedComponent(1, entities2, node1);
        }
        
        private void ClearStack()
        {
            while (!_stack.IsEmpty)
            {
                var data = _stack.Pop();
                Entities
                    .WithoutBurst()
                    .WithSharedComponentFilter(data)
                    .ForEach((Entity e, int entityInQueryIndex) =>
                    {
                        _ecb.DestroyEntity(entityInQueryIndex, e);
                    }).Run();
            }
        }
        
        private void PrintStack()
        {
            var data = _stack.Traverse();
            var query = GetEntityQuery(ComponentType.ReadOnly<NodeData>());
            Debug.Log($"The Stack has {data.Count} nodes");
            foreach (var nodeData in data)
            {
                query.SetSharedComponentFilter(nodeData);
                Debug.Log($"Node with ID = {nodeData.index} has {query.CalculateEntityCount()} Entities");
            }
            Debug.Log("---------------------------------");
        }
    }
}