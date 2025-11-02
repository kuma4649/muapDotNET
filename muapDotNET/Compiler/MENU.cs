using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Compiler
{
    public class MENU
    {
        private muapDotNET.Common.x86Register r;
        private muap98 muap98;
        public MENU(muapDotNET.Common.x86Register reg,muap98 muap98) 
        {
            r= reg;
            this.muap98= muap98;
        }

        public byte crflag = 0;
        public byte inmode = 0;

        public void check_calplay()
        {
            r.zero = ((muap98.m_mode[0] & 2) == 0);
        }

        public void check_visualplay()
        {
            r.zero = ((inmode & 8) == 0);
        }
    }
}
