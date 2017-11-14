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

            foreach (Segment seg in graph.Segments)
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

            for (int index = 1; index <= graph.Segments.Count; index++)
            {
                allNodes.Where(node => ((Segment)node).IndexInPattern == index).ToList().ForEach(node => AddToGraph(graphs, (Segment)node));
            }


            return graphs;// group the segments into graphs
        }

        private void AddToGraph(List<Graph> graphs, Segment node)
        {
            Graph target = null;
            foreach (Graph g in graphs)
            {
                if(node.BelongsTo((Graph) g))
                {
                    target = g;
                }
            }

            if (target == null)
            {
                target = new Graph();
                graphs.Add(target);
            }

            target.AddSegment(node);
            node.Log.ToList().ForEach(l => target.AddLog(l));
            node.PatternContext.ForEach(c => target.AddContext(c));
        }

    }
}
