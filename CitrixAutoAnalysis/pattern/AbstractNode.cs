using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CitrixAutoAnalysis.analysis.io;
using System.Xml.Linq;

namespace CitrixAutoAnalysis.pattern
{
    public abstract class AbstractNode
    {
        private Guid nodeId; // store the node id
        private AbstractNode parent;
        protected AbstractNode root;// the root of the pattern graph, so we can access some data from derived node
        protected List<AbstractNode> childNodes = new List<AbstractNode>(); // store all context info of current node
        private string nodeName;
        private int indexInparent;

        public AbstractNode(AbstractNode prnt, string name)
        {
            nodeId = Guid.NewGuid();
            this.parent = prnt;
            this.nodeName = name;
            this.root = prnt.root;
        }

        public AbstractNode(Guid id, AbstractNode prnt, string name, int index) {
            this.nodeId = id;
            this.parent = prnt;
            this.nodeName = name;
            this.indexInparent = index;
        }

        //public abstract bool IsMatch(AbstractNode node);// compare if passed-in instance can match with current
        public abstract string ToXml(); //serialize the object into xml format, for the purpose of pattern persistance.

        public Guid NodeId {
            get { return nodeId; }
            set { nodeId = value; } 
        }

        public AbstractNode Root
        {
            get 
            { 
                if(root != null)
                    return root;
                else if (parent != null)
                {
                    return parent.Root;
                }

                //we hope it never gets here. so please do remember set the "root" from pattern node or graph node.
                return null;
            }
        }

        public List<AbstractNode> ChildNodes
        {
            get { return childNodes; }
            set { childNodes = value; }
        }
        public static AbstractNode FromXml(XElement elem, AbstractNode parent)
        { 
            //please do replace me, though I am static and cannot be abstact
            throw new NotImplementedException();
        }

        public void AddChildNode(AbstractNode child)
        {
            this.ChildNodes.Add(child);
        }

        public List<Log> LogInCurrent()
        {
            List<Log> log = new List<Log>();

            if(this.GetType() == typeof(CitrixAutoAnalysis.pattern.Context))
            {
                return log;
            }

            if (this.GetType() == typeof(CitrixAutoAnalysis.pattern.Log))
            {
                log.Add((Log)this);
                return log;
            }

            this.childNodes.ForEach(child => log.AddRange(child.LogInCurrent()));

            return log;
        }

        public List<AbstractNode> SegInCurrent() 
        {
            if (this.GetType() == typeof(CitrixAutoAnalysis.pattern.Pattern))
            {
                List<AbstractNode> segment = new List<AbstractNode>();
                this.ChildNodes.ForEach(child => segment.AddRange(child.ChildNodes));
                return segment;
            }
            else if (this.GetType() == typeof(CitrixAutoAnalysis.pattern.Graph))
            {
                return this.ChildNodes;
            }

            return null;
        }

        public List<Context> ContextInCurrent()
        {
            List<Context> context = new List<Context>();

            if (this.GetType() == typeof(CitrixAutoAnalysis.pattern.Context))
            {
                context.Add((Context)this);
                return context;
            }

            this.childNodes.ForEach(child => context.AddRange(child.ContextInCurrent()));

            return context;
        }

        public AbstractNode Parent
        {
            get{return parent;}
            set{parent = value;}
        }

        public string NodeName
        {
            get{return nodeName;}
            set{nodeName = value;}
        }

        public int IndexInParent
        {
            get { return indexInparent; }
            set { indexInparent = value; }
        }

        //find the context via the name
        protected Context GetContextDataByName(string name){
            List<Context> Con = ContextInCurrent();

            if(Con.Any(c => c.NodeName == name))
            {
                return Con.First(c => c.NodeName == name);
            }
            return null;
        }

        //let the derived classes to fill in the sql
        public abstract string ConstructSql();

        public void ToDB()
        {
            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(ConstructSql());
            }    
        }
    }
}
