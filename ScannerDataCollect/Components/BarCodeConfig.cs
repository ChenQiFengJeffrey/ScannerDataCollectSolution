using DevExpress.DataAccess.ObjectBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Components
{
    public struct BarCodeBindData
    {
        public String Content;
        public String Generator;
    }

    [HighlightedClass]
    public class BarCodeConfig
    {
        [HighlightedMember]
        public String Title { get; set; }
        [HighlightedMember]
        public String Content { get; set; }
        [HighlightedMember]
        public String Generator { get; set; }
        [HighlightedMember]
        public double Width { get; set; }
        [HighlightedMember]
        public double Height { get; set; }
        [HighlightedMember]
        public BarCodeBindData Data
        {
            get { return new BarCodeBindData() { Content = this.Content, Generator = this.Generator }; }

        }

        public override string ToString()
        {
            return this.Content;
        }
    }
}
