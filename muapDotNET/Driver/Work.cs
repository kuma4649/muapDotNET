using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Driver
{
    public class Work
    {
        public object lockObj = new object();
        public object SystemInterrupt = new object();
        public bool resetPlaySync = false;
        private int _status = 0;
        public int Status
        {
            get { lock (lockObj) { return _status; } }
            set { lock (lockObj) { _status = value; } }
        }

        public byte[] fifoBuf;
        public Action int0bEnt;

        public OPNATimer timerOPNA1 = null;
        public ulong timeCounter = 0L;
        public int currentTimer = 0;
        public short[] sound;
        public _8253Timer _8253timer = null;
        public MmlDatum crntMmlDatum = null;
        //public DMA dma=null;
        //public CS4231 cs4231 = null;

        public MmlDatum[] mData = null;

        public Work()
        {
            Init();
        }

        private void Init()
        {
        }
    }
}

