#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GP;
using STGP_Sharp.Utilities.GeneralCSharp;
using Newtonsoft.Json;
using STGP_Sharp.GpBuildingBlockTypes;

namespace STGP_Sharp
{
    // TODO move to new file
    public class NodeComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node? node1, Node? node2)
        {
            return node1?.Equals(node2) ?? null == node2;
        }

        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Ideally we would leave getting the hashcode of the node up to an overridden method in the node class/a node
        ///     subclass,
        ///     but this won't work because the node symbol and children cannot be readonly as currently written. In the future,
        ///     maybe we will
        ///     refactor the node class to take the node symbol as a parameter to the constructor, and rewrite Node.DeepCopy. For
        ///     now, this is
        ///     simpler.
        /// </remarks>
        public int GetHashCode(Node? node)
        {
            if (null == node) return 0;

            var children = node.children;
            if (!node.DoesChildrenOrderMatter) children = children.OrderBy(n => n.symbol).ToList();

            var childrenHashCodes = children.Select(c => new NodeComparer().GetHashCode(c));
            var childrenHashCode = GeneralCSharpUtilities.CombineHashCodes(childrenHashCodes);

            return GeneralCSharpUtilities.CombineHashCodes(
                new[]
                {
                    node.symbol.GetHashCode(),
                    childrenHashCode,
                    node.returnType.GetHashCode()
                }
            );
        }
    }


    /// <remarks>
    ///     Ideally we would make Node.GetHashCode and Node.Equals a virtual method, so that each Node subclass can define it's
    ///     own method.
    ///     But this cannot be done as currently written because the node symbol and children cannot be readonly as currently
    ///     written.
    ///     Thus, we include the field <see cref="Node.DoesChildrenOrderMatter" /> to allow for one special method.
    ///     In the future, maybe we will
    ///     refactor the node class to take the node symbol as a parameter to the constructor, and rewrite Node.DeepCopy. For
    ///     now, this is
    ///     simpler.
    /// </remarks>
    public class Node // TODO abstract? Won't work with simple deserialization
    {
        public readonly Type returnType;

        public List<Node> children;

        public string symbol;
        public virtual bool DoesChildrenOrderMatter => true;

        public string guid;


        [JsonConstructor]
        public Node(Type returnType, List<Node> children) // TODO protected? Won't work for deserialization
        {
            symbol = GetType().Name;
            this.children = children;
            this.returnType = returnType;
            this.guid = Guid.NewGuid().ToString();
        }

        public Node(Type returnType) // TODO protected? Won't work for deserialization
        {
            symbol = GetType().Name;
            children = new List<Node>();
            this.returnType = returnType;
            this.guid = Guid.NewGuid().ToString();
        }

        public bool Equals(Node? otherNode)
        {
            if (null == otherNode) return false;

            var myNodes = IterateNodes();
            var theirNodes = otherNode.IterateNodes();
            if (!DoesChildrenOrderMatter)
            {
                myNodes = myNodes.OrderBy(n => n.symbol);
                theirNodes = theirNodes.OrderBy(n => n.symbol);
            }

            var myNodeAsArray = myNodes as Node[] ?? myNodes.ToArray();
            var theirNodesAsArray = theirNodes as Node[] ?? theirNodes.ToArray();

            if (myNodeAsArray.Length != theirNodesAsArray.Length) return false;

            for (var i = 0; i < myNodeAsArray.Length; i++)
            {
                var myNode = myNodeAsArray[i];
                var theirNode = theirNodesAsArray[i];
                if (!myNode.ShallowEquals(theirNode)) return false;
            }

            return true;
        }

        public bool ShallowEquals(Node? root)
        {
            if (null == root) return false;
            return symbol == root.symbol && returnType == root.returnType;
        }

        public IEnumerable<Node> IterateTerminals()
        {
            if (children.Count == 0) yield return this;

            foreach (var child1 in children)
            foreach (var child2 in child1.IterateTerminals())
                yield return child2;
        }

        public int GetSize()
        {
            return 1 + this.children.SelectMany(c => c.IterateNodes()).Count();
        }

        // TODO rewrite using linq?
        public IEnumerable<Node> IterateNodes()
        {
            yield return this;

            foreach (var child1 in children)
            foreach (var child2 in child1.IterateNodes())
                yield return child2;
        }

        // TODO rewrite using linq?
        private IEnumerable<NodeWrapper> IterateNodeWrappersHelper()
        {
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                yield return new NodeWrapper(this, child, i);

                foreach (var childNodeWrapper in child.IterateNodeWrappersHelper()) yield return childNodeWrapper;
            }
        }

        public IEnumerable<NodeWrapper> IterateNodeWrappers()
        {
            yield return new NodeWrapper(this);

            foreach (var wrapper in IterateNodeWrappersHelper()) yield return wrapper;
        }

        // Skip 1 because we include the root node in IterateNodeWrappers
        public IEnumerable<NodeWrapper> IterateNodeWrapperWithoutRoot()
        {
            return IterateNodeWrappers().Skip(1);
        }

        public Node DeepCopy()
        {
            var clone = (Node)MemberwiseClone();
            clone.children = children.ToList();
            for (var i = 0; i < clone.children.Count; i++) clone.children[i] = children[i].DeepCopy();
            return clone;
        }

        public IEnumerable<int> GetSymTypeAndFilterLocationsInDescendants(Type descendantReturnType,
            List<FilterAttribute> filters)
        {
            var currentLocation = 1;

            foreach (var descendant in IterateNodes().Skip(1))
            {
                if (descendant.returnType == descendantReturnType &&
                    STGP_Sharp.GpRunner.GetFilterAttributes(descendant.GetType()).SequenceEqual(filters))
                    yield return currentLocation;

                currentLocation++;
            }
        }

        public Node GetNodeAtIndex(int goalIndex)
        {
            var node = IterateNodes().Skip(goalIndex).FirstOrDefault() ??
                       throw new ArgumentOutOfRangeException(nameof(goalIndex));
            return node;
        }

        public NodeWrapper GetNodeWrapperAtIndex(int goalIndex)
        {
            var node = IterateNodeWrappers().Skip(goalIndex).FirstOrDefault() ??
                       throw new ArgumentOutOfRangeException(nameof(goalIndex));
            return node;
        }

        public int GetHeight()
        {
            if (children.Count == 0) return 0; // Leaf node has height 0
            return children.Max(child => child.GetHeight()) + 1;
        }

        public int GetDepthOfNodeAtIndex(int goalIndex)
        {
            var node = GetNodeAtIndex(goalIndex);
            return GetDepthOfNode(node);
        }

        public int GetDepthOfNode(Node node)
        {
            return GetHeight() - node.GetHeight();
        }

        public void PrintAsList(string prefix = "")
        {
            CustomPrinter.PrintLine($"{prefix}{ToStringInListForm()}");
        }

        public string ToStringInListForm()
        {
            var result = new StringBuilder(symbol);
            if (children.Count > 0)
            {
                result.Append("(");
                result.Append(string.Join(", ", children.Select(child => child.ToStringInListForm())));
                result.Append(")");
            }

            return result.ToString();
        }

        public override string ToString()
        {
            return ToStringInListForm();
        }

        public virtual Node Mutate(STGP_Sharp.GpRunner gp, int maxDepth)
        {
            var fullyGrow = gp.rand.NextBool(); // TODO this should be a gui parameter
            var filters = STGP_Sharp.GpRunner.GetFilterAttributes(GetType());
            var returnTypeSpecification = new ReturnTypeSpecification(returnType, filters);
            return gp.GenerateRandomTreeOfReturnType(returnTypeSpecification, maxDepth, fullyGrow);
        }
    }
}