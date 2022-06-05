using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Project
{
    [AlwaysUpdateSystem]
    public partial class UseStackSystem : SystemBase
    {
        private const int StackSize = 20;
        private const int MaxEntitiesPerChunk = 2040;

        private EndSimulationEntityCommandBufferSystem _ecbs;
        private EntityCommandBuffer.ParallelWriter _ecb;
        
        private readonly ECS_Stack _stack = new ECS_Stack();
        private IEnumerator _textStack;

        protected override void OnCreate()
        {
            _ecbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            _textStack = TestStack().GetEnumerator();
        }

        protected override void OnUpdate()
        {
            _textStack.MoveNext();
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

        private void SplitHead()
        {
            var lastNodeData = new NodeData { index = StackSize - 1};
            
            var query = GetEntityQuery(ComponentType.ReadOnly<NodeData>());
            query.SetSharedComponentFilter(_stack.Head);
            
            _stack.Push(lastNodeData);
            
            var entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i != entities.Length / 2; i++)
                _ecb.SetSharedComponent(i, entities[i], lastNodeData);
            
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

        private IEnumerable TestStack()
        {
            _ecb = _ecbs.CreateCommandBuffer().AsParallelWriter();
            FillStack();
            yield return null;
            PrintStack();
            _ecb = _ecbs.CreateCommandBuffer().AsParallelWriter();
            SplitHead();
            yield return null;
            PrintStack();
            _ecb = _ecbs.CreateCommandBuffer().AsParallelWriter();
            ClearStack();
            yield return null;
            PrintStack();
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
                query.ResetFilter();
            }
            Debug.Log("---------------------------------");
        }
    }
}