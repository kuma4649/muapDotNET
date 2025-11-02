using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Common
{
    public class muapChipAction : ChipAction
    {
        private Action<ChipDatum> _Write;
        private Action<byte[], int, int> _WritePCMData;
        private Action<long, int> _WaitSend;

        public muapChipAction(Action<ChipDatum> Write, Action<byte[], int, int> WritePCMData, Action<long, int> WaitSend)
        {
            _Write = Write;
            _WritePCMData = WritePCMData;
            _WaitSend = WaitSend;
        }

        public override string GetChipName()
        {
            throw new NotImplementedException();
        }

        public override void WaitSend(long t1, int t2)
        {
        }

        public override void WritePCMData(byte[] data, int startAddress, int endAddress)
        {
            _WritePCMData?.Invoke(data, startAddress, endAddress);
        }

        public override void WriteRegister(ChipDatum cd)
        {
            _Write?.Invoke(cd);
        }
    }
}
