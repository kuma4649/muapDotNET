using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Compiler
{
    public class Work
    {
        public CompilerInfo compilerInfo = null;
        public string sourceFileName = "";
        public int row = 0;
        public int col = 0;
        public ushort oldbx;
        public ushort ontei;
        public byte oct;
        public MmlDatum md;
        public int otoLength;//音長
        public string crntChip;
        public int crntChannel;
        public string crntPart;
        public List<MmlDatum> lstMd=new List<MmlDatum>();
        public int mdArgsStep = 0;

        public MmlDatum Copy(MmlDatum md,int dat)
        {
            MmlDatum md2;
            if(md== null)
            {
                md2 = new MmlDatum(dat);
                return md2;
            }
            
            md2 = new MmlDatum(md.type, md.args, md.linePos, dat);
            return md2;
        }

        public MmlDatum FlashLstMd(MmlDatum md)
        {
            mdArgsStep = 0;
            if (md == null) return null;

            if (lstMd.Count <= 0)
            {
                return md;
            }

            List<object> oldArgs = md.args;
            md.args = new List<object>();
            md.args.Add(lstMd);
            md.args.AddRange(oldArgs);
            lstMd = new List<MmlDatum>();
            mdArgsStep = 1;

            return md;
        }
    }
}
