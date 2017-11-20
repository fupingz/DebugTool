using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitrixAutoAnalysis.pattern;
using CitrixAutoAnalysis.analysis.io;
namespace CitrixAutoAnalysis.analysis.engine
{
    class IssueSummary
    {
        private Log breakPnt;

        private Log errOrExcep;

        private Guid patternId;

        private int jobId;

        private string name;

        private string lcId;

        private string keyWords;

        public IssueSummary(Log brkPnt, Log error, Guid pattern, int job, string lc, string name, HashSet<string> words)
        {
            this.breakPnt = brkPnt;
            this.errOrExcep = error;
            this.patternId = pattern;
            this.jobId = job;
            this.name = name;
            this.lcId = lc;

            if (words == null)
            {
                if (error != null)
                    words.Add(error.Module);
                else
                {
                    words.Add(brkPnt.Module);
                }
            }
            foreach (string word in words)
                this.keyWords += word + "##";
        }

        public void OutputIssueToDB()
        {
            string rootCause = errOrExcep.Text;
            if (rootCause == null || rootCause.Length == 0)
            {
                rootCause = keyWords;
            }
            string sql = "Insert into CadIssues values('" + lcId + "'," + jobId + ", '" + keyWords + "','" + name + "','" + keyWords + "',null,0,'" + patternId.ToString() + "','"+keyWords+"')";

            using (DBHelper helper = new DBHelper())
            {
                helper.UpdateDB(sql);
            }
        }

    }

    //this declares the point where the logic got break, so we isolate the issue to there.
    class BreakPoint
    {
        private Guid mostAppropriateSeg;
        private Guid mostAppropriateLog;
        private string suspect;

        public BreakPoint(Guid seg, Guid log, string spct)
        {
            this.mostAppropriateSeg = seg;
            this.mostAppropriateLog = log;

            this.suspect = spct;
        }
    }
}
