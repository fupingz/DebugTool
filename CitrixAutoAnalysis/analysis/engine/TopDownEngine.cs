using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.scheduler;

namespace CitrixAutoAnalysis.analysis.engine
{
    class TopDownEngine
    {
        private CDFHelper helper;
        private Graph graph;

        public TopDownEngine(CDFHelper helper, AbstractNode node)
        {
            this.helper = helper;
            this.graph = (Graph) node;
        }
        public List<Graph> ExtractFromCDF()
        {
            HashSet<AbstractNode> allNodeInstances = new HashSet<AbstractNode>();

            foreach (Segment seg in graph.ChildNodes)
            {
                MileStoneEngine engine = new MileStoneEngine(seg, helper);
                engine.ExtractFromCDF().ForEach(node => allNodeInstances.Add(node));
            }

            List<Graph> extractedGraphs = new List<Graph>();

            return AssembleGraphs(allNodeInstances);
        }

        public List<Graph> AssembleGraphs(HashSet<AbstractNode> allNodes)
        {
            //now I am implementing the first simple one, that allows a single sequence in the log

            List<Graph> graphs = new List<Graph>();

            for (int index = 1; index <= graph.ChildNodes.Count; index++)
            {
                allNodes.Where(node => ((Segment)node).IndexInParent == index).ToList().ForEach(node => AddToGraph(graphs, allNodes, (Segment)node));
            }

            return graphs;// group the segments into graphs
        }

        private void AddToGraph(List<Graph> graphs, HashSet<AbstractNode> allNodes, Segment node)
        {
            Graph target = null;
            foreach (Graph g in graphs)
            {
                bool OnlyOneNode = node.IndexInParent > 1 && graphs.Count == 1 && allNodes.ToList().FindAll(seg => seg.IndexInParent == node.IndexInParent).ToList().Count == 1;

                if(OnlyOneNode || node.BelongsTo((Graph) g))
                {
                    target = g;
                    break;
                }
            }

            if (target == null)
            {
                target = new Graph(Guid.NewGuid(), null, "default graph");
                graphs.Add(target);
            }
            node.Parent = target;
            target.AddChildNode(node);
        }

    }
}
