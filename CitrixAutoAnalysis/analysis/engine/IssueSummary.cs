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
        private string rootCause;

        private Guid patternId;

        private int jobId;

        private string name;

        private string lcId;

        private string keyWords;

        public IssueSummary(string root, Guid pattern, int job, string lc, string name, HashSet<string> words)
        {
            this.rootCause = root;
            this.patternId = pattern;
            this.jobId = job;
            this.name = name;
            this.lcId = lc;
            this.keyWords = "";
            foreach (string word in words)
            {
                if(this.keyWords.Length+word.Length <= 100)
                       this.keyWords += word + "#";//keywords are hardcoded in database to be within 100. normall this would be find, but GroupPolicy has very long func names
                else
                    break;
            }
        }

        public void OutputIssueToDB()
        {
            string sql = "Insert into CadIssues values('" + lcId + "'," + jobId + ", null,'" + name + "','" + rootCause.Replace('\'','"') + "',null,0,'" + patternId.ToString() + "','"+keyWords+"',null)";

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
