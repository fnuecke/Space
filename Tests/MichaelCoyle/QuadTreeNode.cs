using System.Collections.Generic;

// Adjust these as necessary, they just have to share a compatible
// interface with the XNA types.
using System.Diagnostics;
using TRectangle = Engine.Math.RectangleF;

namespace Tests.MichaelCoyle
{
    /// <summary>
    /// The QuadTreeNode
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MCQuadTreeNode<T>
    {
        /// <summary>
        /// Construct a quadtree node with the given bounds 
        /// </summary>
        /// <param name="bounds"></param>
        public MCQuadTreeNode(TRectangle bounds)
        {
            _bounds = bounds;
        }

        /// <summary>
        /// The area of this node
        /// </summary>
        private TRectangle _bounds;

        /// <summary>
        /// The contents of this node.
        /// Note that the contents have no limit: this is not the standard way to impement a QuadTree
        /// </summary>
        private readonly List<MCQuadTree<T>.Wrapper> _contents = new List<MCQuadTree<T>.Wrapper>();

        /// <summary>
        /// The child nodes of the QuadTree
        /// </summary>
        private readonly List<MCQuadTreeNode<T>> _nodes = new List<MCQuadTreeNode<T>>(4);

        /// <summary>
        /// Is the node empty
        /// </summary>
        public bool IsEmpty
        {
            get { return _bounds.IsEmpty || _nodes.Count == 0; }
        }

        /// <summary>
        /// Area of the quadtree node
        /// </summary>
        public TRectangle Bounds
        {
            get { return _bounds; }
        }

        /// <summary>
        /// Total number of nodes in the this node and all SubNodes
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;

                foreach (var node in _nodes)
                    count += node.Count;

                count += Contents.Count;

                return count;
            }
        }

        /// <summary>
        /// Return the contents of this node and all subnodes in the true below this one.
        /// </summary>
        public void AddSubTreeContents(ref ICollection<T> list)
        {
            foreach (var node in _nodes)
                node.AddSubTreeContents(ref list);

            foreach (var content in Contents)
            {
                list.Add(content.Value);
            }
        }

        public List<MCQuadTree<T>.Wrapper> Contents
        {
            get { return _contents; }
        }

        /// <summary>
        /// Query the QuadTree for items that are in the given area
        /// </summary>
        /// <param name="queryArea"></param>
        /// <param name="list"> </param>
        /// <returns></returns>
        public void Query(ref TRectangle queryArea, ref ICollection<T> list)
        {
            // this quad contains items that are not entirely contained by
            // it's four sub-quads. Iterate through the items in this quad 
            // to see if they intersect.
            foreach (var item in this.Contents)
            {
                if (queryArea.Intersects(item.Rectangle))
                    list.Add(item.Value);
            }

            foreach (var node in _nodes)
            {
                if (node.IsEmpty)
                    continue;

                // Case 1: search area completely contained by sub-quad
                // if a node completely contains the query area, go down that branch
                // and skip the remaining nodes (break this loop)
                if (node.Bounds.Contains(queryArea))
                {
                    node.Query(ref queryArea, ref list);
                    break;
                }

                // Case 2: Sub-quad completely contained by search area 
                // if the query area completely contains a sub-quad,
                // just add all the contents of that quad and it's children 
                // to the result set. You need to continue the loop to test 
                // the other quads
                if (queryArea.Contains(node.Bounds))
                {
                    node.AddSubTreeContents(ref list);
                    continue;
                }

                // Case 3: search area intersects with sub-quad
                // traverse into this quad, continue the loop to search other
                // quads
                if (node.Bounds.Intersects(queryArea))
                {
                    node.Query(ref queryArea, ref list);
                }
            }
        }

        /// <summary>
        /// Insert an item to this node
        /// </summary>
        /// <param name="item"></param>
        public void Insert(MCQuadTree<T>.Wrapper item)
        {
            // if the item is not contained in this quad, there's a problem
            if (!_bounds.Contains(item.Rectangle))
            {
                Trace.TraceWarning("feature is out of the bounds of this quadtree node");
                return;
            }

            // if the subnodes are null create them. may not be sucessfull: see below
            // we may be at the smallest allowed size in which case the subnodes will not be created
            if (_nodes.Count == 0)
                CreateSubNodes();

            // for each subnode:
            // if the node contains the item, add the item to that node and return
            // this recurses into the node that is just large enough to fit this item
            foreach (var node in _nodes)
            {
                if (node.Bounds.Contains(item.Rectangle))
                {
                    node.Insert(item);
                    return;
                }
            }

            // if we make it to here, either
            // 1) none of the subnodes completely contained the item. or
            // 2) we're at the smallest subnode size allowed 
            // add the item to this node's contents.
            Contents.Add(item);
        }

        public void ForEach(MCQuadTree<T>.QTAction action)
        {
            action(this);

            // draw the child quads
            foreach (var node in this._nodes)
                node.ForEach(action);
        }

        /// <summary>
        /// Internal method to create the subnodes (partitions space)
        /// </summary>
        private void CreateSubNodes()
        {
            // the smallest subnode has an area 
            if ((_bounds.Height * _bounds.Width) <= 10)
                return;

            var halfWidth = (_bounds.Width / 2);
            var halfHeight = (_bounds.Height / 2);

            _nodes.Add(new MCQuadTreeNode<T>(new TRectangle(_bounds.Left, _bounds.Top, halfWidth, halfHeight)));
            _nodes.Add(new MCQuadTreeNode<T>(new TRectangle(_bounds.Left, _bounds.Top + halfHeight, halfWidth, halfHeight)));
            _nodes.Add(new MCQuadTreeNode<T>(new TRectangle(_bounds.Left + halfWidth, _bounds.Top, halfWidth, halfHeight)));
            _nodes.Add(new MCQuadTreeNode<T>(new TRectangle(_bounds.Left + halfWidth, _bounds.Top + halfHeight, halfWidth, halfHeight)));
        }

    }
}
