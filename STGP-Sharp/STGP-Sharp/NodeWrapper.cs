#nullable enable

using STGP_Sharp;

namespace GP
{
    public class NodeWrapper
    {
        private readonly int? _childIndex;
        public readonly Node? parent;
        public Node child;

        public NodeWrapper(Node? parent, Node child, int? childIndex)
        {
            this.parent = parent;
            this.child = child;
            _childIndex = childIndex;
        }

        public NodeWrapper(Node child)
        {
            this.child = child;
        }

        public void ReplaceWith(Node newChild)
        {
            if (parent != null && _childIndex != null) parent.children[(int)_childIndex] = newChild;
            child = newChild;
        }
    }
}