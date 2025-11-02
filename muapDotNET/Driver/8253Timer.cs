using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Driver
{
    /// <summary>
    /// 参考:
    /// http://www.webtech.co.jp/company/doc/undocumented_mem/io_tcu.txt
    /// </summary>

    public class _8253Timer
    {
        private readonly int renderingFreq;
        private readonly int masterClock;

        private class Ch
        {
            public int _stat = 0;
            public int Stat { set { _stat = value; } get { int bk = _stat; _stat = 0; return bk; } }
            public int c = 3;
            public int b = 0;
            public bool a = false;
            public int val = 0;
            public double counter = 0.0;
            public double step = 0.0;
        }
        private Ch[] ch = new Ch[] { new Ch(), new Ch(), new Ch() };
        public int ch0Stat { get { return ch[0].Stat; } }
        public int ch1Stat { get { return ch[1].Stat; } }
        public int ch2Stat { get { return ch[2].Stat; } }

        public _8253Timer(int renderingFreq, int masterClock = 1996800)//2457600)
        {
            this.renderingFreq = renderingFreq;
            // 5/10MHz系のPC98は1996800Hz 8MHz系のPC98は2457600Hzだそうだ
            this.masterClock = masterClock;
        }

        public void Timer()
        {
            ch[0].counter += ch[0].step;
            if (ch[0].counter >= 1.0)
            {
                ch[0]._stat = 1;
                ch[0].counter -= 1.0;
            }
        }

        public bool WriteReg(byte adr, byte data)
        {
            int sc;
            switch (adr)
            {
                case 0x71:
                case 0x73:
                case 0x75:
                    sc = (adr - 0x71) / 2;
                    if (ch[sc].c != 3) return false;
                    if (!ch[sc].a) ch[sc].val = (ch[sc].val & 0xff00) | data;
                    else ch[sc].val = ((byte)ch[sc].val) | (data << 8);
                    ch[sc].a = !ch[sc].a;
                    ch[sc].step = 0;
                    if (ch[sc].val != 0 && renderingFreq != 0)
                    {
                        ch[sc].step = (double)masterClock / ch[sc].val / renderingFreq;// * 160.0;
                    }
                    return true;
                case 0x77:
                    sc = (data & 0b1100_0000) >> 6;
                    if (sc == 3) return false;//マルチプルラッチコマンド未対応
                    int c = (data & 0b0011_0000) >> 4;
                    if (c == 0) return false;//カウントラッチコマンド未対応
                    int m = (data & 0b0000_1110) >> 1;
                    if (m > 5) m -= 4;
                    if (m != 3) return false;//モード3(方形波ジェネレータ)のみ対応
                    int b = (data & 1);
                    if (b != 0) return false;//バイナリカウントのみ対応
                    ch[sc].c = c;
                    ch[sc].b = b;
                    return true;
            }
            return false;
        }

    }
}
