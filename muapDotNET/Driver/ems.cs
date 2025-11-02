using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace muapDotNET.Driver
{
    public class EMS
    {
        //private int crntEmsHandle = 0;
        //private int crntPageMap = 0;
        //private int pPageNo = 0;
        //private int lPageNo = 0;
        //private Dictionary<int, bool> useEMSList;
        //private Dictionary<int,string> handleName;
        //private Dictionary<int, byte[][]> emsBuff;
        //private Dictionary<int, int[]> mappedPage;

        public Func<byte[]> cS4231EMS_GetCrntMapBuf;
        public iDriver.dlgEMS_Map cS4231EMS_Map;
        public Func<ushort> cS4231EMS_GetPageMap;
        public iDriver.dlgEMS_GetHandleName cS4231EMS_GetHandleName;
        public iDriver.dlgEMS_SetHandleName cS4231EMS_SetHandleName;
        public iDriver.dlgEMS_AllocMemory cS4231EMS_AllocMemory;

        //public EMS()
        //{
        //    crntEmsHandle=0;
        //    useEMSList = new Dictionary<int, bool>();
        //    handleName = new Dictionary<int,string>();
        //    emsBuff = new Dictionary<int, byte[][]>();
        //    mappedPage = new Dictionary<int, int[]>();
        //}

        public EMS(Func<byte[]> cS4231EMS_GetCrntMapBuf,
            iDriver.dlgEMS_Map cS4231EMS_Map,
            Func<ushort> cS4231EMS_GetPageMap,
            iDriver.dlgEMS_GetHandleName cS4231EMS_GetHandleName,
            iDriver.dlgEMS_SetHandleName cS4231EMS_SetHandleName,
            iDriver.dlgEMS_AllocMemory cS4231EMS_AllocMemory)
        {
            this.cS4231EMS_GetCrntMapBuf = cS4231EMS_GetCrntMapBuf;
            this.cS4231EMS_Map = cS4231EMS_Map;
            this.cS4231EMS_GetPageMap = cS4231EMS_GetPageMap;
            this.cS4231EMS_GetHandleName = cS4231EMS_GetHandleName;
            this.cS4231EMS_SetHandleName = cS4231EMS_SetHandleName;
            this.cS4231EMS_AllocMemory = cS4231EMS_AllocMemory;
        }

        //public void GetHandleName(x86Register reg, ref string sbuf)
        //{
        //    reg.ah = 0;
        //    if (handleName.ContainsKey(reg.dx))
        //    {
        //        sbuf = handleName[reg.dx];
        //        return;
        //    }

        //    sbuf = "";//熊:一致するものがなくてもahは0になるっぽい
        //}

        //public void SetHandleName(x86Register reg, string emsname2)
        //{
        //    reg.ah = 0;
        //    if (!handleName.ContainsKey(reg.dx))
        //        handleName.Add(reg.dx, emsname2);
        //    else
        //        handleName[reg.dx] = emsname2;
        //}

        //public void AllocMemory(x86Register reg)
        //{
        //    //未使用のハンドルを探す
        //    int cnt = 0;
        //    while (cnt < 0x10000)
        //    {
        //        if (!useEMSList.ContainsKey(crntEmsHandle) || !useEMSList[crntEmsHandle]) break;
        //        crntEmsHandle++;
        //        crntEmsHandle &= 0xffff;
        //        cnt++;
        //    }

        //    if (cnt == 0x10000)
        //    {
        //        reg.ah = 1;
        //        return;
        //    }

        //    reg.dx = (ushort)crntEmsHandle;
        //    if (!useEMSList.ContainsKey(crntEmsHandle)) useEMSList.Add(crntEmsHandle, true);
        //    else useEMSList[crntEmsHandle] = true;
        //    if (!emsBuff.ContainsKey(crntEmsHandle)) emsBuff.Add(crntEmsHandle, null);
        //    emsBuff[crntEmsHandle] = new byte[reg.bx][];
        //    if (!mappedPage.ContainsKey(crntEmsHandle)) mappedPage.Add(crntEmsHandle, null);
        //    mappedPage[crntEmsHandle] = new int[reg.bx];

        //    for (int i = 0; i < reg.bx; i++)
        //    {
        //        emsBuff[crntEmsHandle][i] = new byte[16 * 1024];//alloc 16Kbyte
        //        for (int j = 0; j < 16 * 1024; j++) emsBuff[crntEmsHandle][i][j] = 0x80;
        //        mappedPage[crntEmsHandle][i] = 0xffff;//ummap状態
        //    }
        //    reg.ah = 0;
        //}

        //public ushort GetPageMap()//x86Register reg, byte[] pemsbuf)
        //{
        //    //reg.bx = (ushort)crntPageMap;
        //    return (ushort)crntPageMap;
        //}

        //public void Map(x86Register reg)
        //{
        //    pPageNo = reg.al;//物理ページ番号
        //    lPageNo = reg.bx;//論理ページ番号

        //    try
        //    {
        //        //マップ
        //        mappedPage[reg.dx][pPageNo] = lPageNo;//0xffffの場合はアンマップ状態

        //        reg.ah = 0x00;//正常実行
        //    }
        //    catch
        //    {
        //        reg.ah = 0x80;
        //    }
        //}

        //public byte Map(byte al,ushort bx,ushort dx)
        //{
        //    pPageNo = al;//物理ページ番号
        //    lPageNo = bx;//論理ページ番号

        //    try
        //    {
        //        //マップ
        //        mappedPage[dx][pPageNo] = lPageNo;//0xffffの場合はアンマップ状態

        //        return 0x00;//正常実行
        //        //ahにいれて
        //    }
        //    catch
        //    {
        //        return 0x80;
        //        //ahにいれて
        //    }
        //}

        //public void SetPageMap(ushort si, byte[] pemsbuf)
        //{
        //    crntPageMap = pemsbuf[si];// reg.bx;
        //}

        //public byte[] GetCrntMapBuf()
        //{
        //    return emsBuff[crntEmsHandle][mappedPage[crntEmsHandle][crntPageMap]];
        //}

        //public byte[] GetEmsArray(int stPage,int endPage)
        //{
        //    List<byte> lst = new List<byte>();
        //    for(int i = stPage; i < endPage; i++)
        //    {
        //        lst.AddRange(emsBuff[crntEmsHandle][i]);
        //    }
        //    return lst.ToArray();
        //}
    }
}
