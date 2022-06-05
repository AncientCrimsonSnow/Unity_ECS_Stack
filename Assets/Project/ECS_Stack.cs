using System;
using System.Collections.Generic;

namespace Project
{
    public class ECS_Stack
    {
        public bool IsEmpty => _head == null;
        public NodeData Head => _head.data;
        
        private ECS_Node _head;

        public void Push(NodeData data)
        {
            ECS_Node node = new ECS_Node(data);
            if (_head != null)
                node.next = _head;
            else
            {
                node.next = null;
            }
            _head = node;
        }

        public NodeData Pop()
        {
            if(IsEmpty)
                throw new Exception("the stack is empty");

            ECS_Node node = new ECS_Node(_head.data);
            _head = _head.next;
            return node.data;
        }

        public List<NodeData> Traverse()
        {
            var result = new List<NodeData>();

            var crrNode = _head;

            while (crrNode != null)
            {
                result.Add(crrNode.data);
                crrNode = crrNode.next;
            }

            return result;
        }
        
        
        private class ECS_Node
        {
            public ECS_Node next;
            public NodeData data;

            public ECS_Node(NodeData data)
            {
                this.data = data;
            }
        }
    }
}