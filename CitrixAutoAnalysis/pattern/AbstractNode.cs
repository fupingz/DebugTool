using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CitrixAutoAnalysis.pattern
{
    public abstract class AbstractNode
    {
        private Guid nodeId; // store the node id
        private List<Context> context = new List<Context>(); // store all context info of current node


        public AbstractNode()
        {
            nodeId = Guid.NewGuid();
        }

        public AbstractNode(Guid id) {
            this.nodeId = id;
        }

        public abstract bool IsMatch(AbstractNode node);// compare if passed-in instance can match with current
        public abstract string ToXml(); //serialize the object into xml format, for the purpose of pattern persistance.

        public Guid NodeId {
            get { return nodeId; }
            set { nodeId = value; } 
        }

        public List<Context> PatternContext {
            get { return context; }
            set { context = value; }
        }

        //find the context via the name
        protected Context GetContextDataByName(string name){
            return context.First(c => c.Name == name);
        }

        public void AddContext(Context cntx)
        {
                context.Add(cntx);
        }
    }
}
