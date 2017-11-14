using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitrixAutoAnalysis.pattern
{
    public class ProductVersion
    {
        private string productName;
        private string productVersion;
        private string hotfixLevel;

        public ProductVersion(string name, string version, string hotfix)
        {
            this.productName = name;
            this.productVersion = version;
            this.hotfixLevel = hotfix;
        }

        //added the properties.
        public string ProductName
        {
            get { return this.productName; }
            set { this.productName = value; }
        }
        public string Version
        {
            get { return this.productVersion; }
            set { this.productVersion = value; }
        }
        public string HotfixLevel
        {
            get { return this.hotfixLevel; }
            set { this.hotfixLevel = value; }
        }
        
    }
}
