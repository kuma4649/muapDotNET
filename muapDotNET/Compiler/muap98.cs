using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Compiler
{
    public class muap98
    {
        public byte[] m_mode = new byte[] { 0, 0, 0, 8 };
        public ushort object_ = 0xc000;
        public AutoExtendList<MmlDatum> object_Buf = new AutoExtendList<MmlDatum>();
        public ushort source = 0xb000;
        public byte[] source_Buf = null;// new byte[0x10000];
        public ushort text = 0xa800;//熊:TONEOFSのバッファは別定義してます
        public byte[] text_Buf = new byte[0x8000];
        public ushort tone = 0;// $124 tone data buffer segment
        public byte[] toneBuff = null;//熊:Tone保存用バッファ
        public ushort buflens = 0x8000;// $126 buffer length (source)
        public ushort bufleno = 0x8000;// $128 buffer length (object)
        public ushort sor_len = 0;// $12c source data length
        public ushort obj_len = 0;// $12e object data length
        public byte[] bufbuf = new byte[128];
        private x86Register r;

        public muap98(byte[] srcBuf, x86Register reg, string tone_path, Work work)
        {
            for (int i = 0; i < 0x10000; i++)
                object_Buf[i] = new MmlDatum();

            source_Buf = srcBuf;
            sor_len = (ushort)srcBuf.Length;
            r = reg;

            if (!File.Exists(tone_path))
            {
                string msg = string.Format("{0}が見つからない",tone_path);
                if (work.compilerInfo == null) work.compilerInfo = new CompilerInfo();
                work.compilerInfo.errorList.Add(new Tuple<int, int, string>(-1, -1, msg));
                throw new MusException(msg);
            }
            byte[] buf = File.ReadAllBytes(tone_path);
            toneBuff = new byte[6400];
            Array.Copy(buf, 0, toneBuff, 0, buf.Length > 6400 ? 6400 : buf.Length);
        }

        public void call_func()
        {
            if ((m_mode[1] & 1) == 0)
            {
                //je skip_call;
                r.carry = true;
                return;
            }
            //call_add(); //熊:TBD
            r.carry = false;
            return;
        }

    }
}
