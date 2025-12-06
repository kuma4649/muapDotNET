using musicDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using muapDotNET.Common;
using System.Net.Mail;
using System.IO;

namespace muapDotNET.Driver
{
    public class Driver : iDriver
    {
        public static readonly uint cOPNAMasterClock = 7987200;
        //public delegate void dlgEMS_Map(byte handle, ref byte pageMap, ushort srcPageNo, ushort len);
        //public delegate void dlgEMS_GetHandleName(ref byte ah, ushort dx, ref string sbuf);
        //public delegate void dlgEMS_SetHandleName(ref byte ah, ushort dx, string emsname2);
        //public delegate void dlgEMS_AllocMemory(ref byte ah, ref ushort dx, ushort bx);

        private object lockObjInt0BEnt = new object();
        private object lockObjWriteReg = new object();
        private Action<ChipDatum> WriteOPNAP;
        private Action<ChipDatum> WriteOPN2P;
        private Action<ChipDatum> WriteC4231;
        private Func<byte, byte> ReadC4231;
        public Func<byte[]> CS4231EMS_GetCrntMapBuf;
        public iDriver.dlgEMS_Map CS4231EMS_Map;
        public Func<ushort> CS4231EMS_GetPageMap;
        public iDriver.dlgEMS_GetHandleName CS4231EMS_GetHandleName;
        public iDriver.dlgEMS_SetHandleName CS4231EMS_SetHandleName;
        public iDriver.dlgEMS_AllocMemory CS4231EMS_AllocMemory;

        private Func<byte, byte, bool> Write8253;
        private IDictionary envVars;
        private NAX nax;
        public Work work = new Work();
        private int renderingFreq = 44100;
        private int opnaMasterClock = (int)cOPNAMasterClock;
        private MmlDatum[] musicData;
        private byte[] toneBuffFromOutside = null;
        private ushort[] labelPtr = null;
        private string objPath = null;
        private int sdm = 0;


        public short[] sound = new short[] { 0, 0 };

        public Driver(IDictionary envVars)
        {
            this.envVars = envVars;
            work.sound= sound;
        }

        //interface from iDriver

        public void FadeOut()
        {
            throw new NotImplementedException();
        }

        public MmlDatum[] GetDATA()
        {
            throw new NotImplementedException();
        }

        public GD3Tag GetGD3TagInfo(byte[] srcBuf)
        {
            throw new NotImplementedException();
        }

        public int GetNowLoopCounter()
        {
            if (nax == null || nax.play4 == null) return 0;

            int aLoopCnt = nax.play4.init_cnt;

            int lookUpLabel = 0;
            int max0 = 0;
            //int max1 = 255;
            //bool fnd0= false;
            //bool fnd1= false;
            for (int i = 0; i < 17; i++)
            {
                int dmy = nax.play4.labelPassCnt[i * 40 + lookUpLabel];
                if (dmy >= 0)
                {
                    max0 = Math.Max(max0, dmy);
                    //fnd0 = true;
                }
                //if (lookUpLabel == 0)
                //{
                //    dmy = nax.play4.labelPassCnt[i * 40 + 1];
                //    if (dmy >= 0)
                //    {
                //        max1 = Math.Min(max1, dmy);
                //        fnd1 = true;
                //    }
                //}
            }

            //if (!fnd0) max0 = 0;
            //if (!fnd1) max1 = 0;
            return Math.Max(aLoopCnt, max0);
            //return Math.Max(Math.Max(aLoopCnt, max0), max1);
        }

        public byte[] GetPCMFromSrcBuf()
        {
            throw new NotImplementedException();
        }

        public ChipDatum[] GetPCMSendData()
        {
            throw new NotImplementedException();
        }

        public Tuple<string, ushort[]>[] GetPCMTable()
        {
            throw new NotImplementedException();
        }

        public int GetStatus()
        {
            //n>0:演奏中
            //n=0:演奏終了
            //n<0:エラー
            return work.Status;
        }

        public List<Tuple<string, string>> GetTags()
        {
            if (nax == null) return null;
            try
            {
                List<Tuple<string, string>> tags = new List<Tuple<string, string>>();
                tags.Add(new Tuple<string, string>("", nax.lyric));
                tags.Add(new Tuple<string, string>("", nax.comlength.ToString()));
                return tags;
            }
            catch
            {
                return null;
            }
        }

        public object GetWork()
        {
            object[] wrk = new object[]
            {
                work.fifoBuf,
                (object)(Action)Int0bEnt
            };
            return (object)wrk;
        }


        public void Init(List<ChipAction> chipsAction, MmlDatum[] srcBuf, Func<string, Stream> appendFileReaderCallback, object addtionalOption)
        {
            WriteOPNAP = chipsAction[0].WriteRegister;
            WriteOPN2P = chipsAction[1].WriteRegister;
            WriteC4231 = chipsAction[2].WriteRegister;
            object[] addOptions = (object[])addtionalOption;
            if (addOptions != null)
            {
                if (addOptions.Length > 0) ReadC4231 = (Func<byte, byte>)addOptions[0];
                if (addOptions.Length > 1) CS4231EMS_GetCrntMapBuf = (Func<byte[]>)addOptions[1];
                if (addOptions.Length > 2) CS4231EMS_Map = (iDriver.dlgEMS_Map)addOptions[2];
                if (addOptions.Length > 3) CS4231EMS_GetPageMap = (Func<ushort>)addOptions[3];
                if (addOptions.Length > 4) CS4231EMS_GetHandleName = (iDriver.dlgEMS_GetHandleName)addOptions[4];
                if (addOptions.Length > 5) CS4231EMS_SetHandleName = (iDriver.dlgEMS_SetHandleName)addOptions[5];
                if (addOptions.Length > 6) CS4231EMS_AllocMemory = (iDriver.dlgEMS_AllocMemory)addOptions[6];
                if (addOptions.Length > 7) toneBuffFromOutside = (byte[])addOptions[7];
                if (addOptions.Length > 8) sdm = (int)addOptions[8];
                if (addOptions.Length > 9) labelPtr = (ushort[])addOptions[9];
                if (addOptions.Length > 10) objPath = (string)addOptions[10];
            }

            musicData = srcBuf;
        }

        public void MusicSTART(int musicNumber)
        {
            //string opt = "/f0 /L1A /V0b /Y0288,0388 /I /OFF /P /T /M2 /BFF /6 /Q /2 /3 /(A /A8 /8";
            string opt = "/F0 /L60 /V0B             /I /OFF /P    /M2 /BFF /Q       /(A     /8 ";
            Log.writeLine(LogLevel.INFO, string.Format("Regist NAX3 (option:{0})", opt));
            x86Register reg = new x86Register();
            Pc98 pc98 = new Pc98(work,reg, WriteOPNAPRegister, WriteOPN2PRegister,WriteC4231Register,ReadC4231Register, Write8253Register,sdm);
            EMS ems = new EMS(
                CS4231EMS_GetCrntMapBuf,
                CS4231EMS_Map,
                CS4231EMS_GetPageMap,
                CS4231EMS_GetHandleName,
                CS4231EMS_SetHandleName,
                CS4231EMS_AllocMemory);
            nax = new NAX(work, reg, envVars, pc98, ems, opt, toneBuffFromOutside, labelPtr, objPath);
            //byte[] o = File.ReadAllBytes("INIT.O");
            nax.function(10, musicData);
            work.Status = 1;
        }

        public void MusicSTOP()
        {
            if (nax == null) return;
            nax.function(2, null);
        }

        public void Rendering()
        {
            if (work.Status < 0) return;

            try
            {
                //if (work.Status == 0) return;
                if (nax.functionList.Count > 0)
                {
                    nax.functionF(nax.functionList[0]);
                    nax.functionList.RemoveAt(0);   
                }

                lock (work.SystemInterrupt)
                {

                    //if (work.resetPlaySync)
                    //{
                    //    work.resetPlaySync = false;
                    //    ChipDatum dat = new ChipDatum(-1, -1, -1, 0, new MmlDatum(enmMMLType.ResetPlaySync, null, null, 0));
                    //    WriteRegister(0, dat);
                    //}

                    if ((nax?.pc98.InportB(2) & 1) == 0)
                        work._8253timer.Timer();
                    //work.cs4231.Timer();
                    work.timerOPNA1.timer();

                    //Console.WriteLine("CurrentTimer:{0}", work.currentTimer);

                    work.timeCounter++;
                    bool flg = false;
                    if (work._8253timer.ch0Stat != 0)
                    {
                        nax?.Int08Entry();
                    }


                    switch (work.currentTimer)
                    {
                        case 0:
                            flg = (work.timerOPNA1.StatReg & 3) != 0;
                            break;
                    }
                    if (flg)
                    {
                        nax?.TimerEntry();
                    }

                }
            }
            catch(Exception ex)
            {
                Log.WriteLine(LogLevel.FATAL, "Fatal error Message:{0} StackTrace:{1}",ex.Message,ex.StackTrace);
                work.Status = -1;
                throw;
            }
        }

        public void SetDriverSwitch(params object[] param)
        {
            throw new NotImplementedException();
        }

        public int SetLoopCount(int loopCounter)
        {
            int aLoopCnt = nax.play4.init_cnt;

            return aLoopCnt;
        }

        public void ShotEffect()
        {
            throw new NotImplementedException();
        }

        public void StartRendering(int renderingFreq, Tuple<string, int>[] chipsMasterClock)
        {
            lock (work.SystemInterrupt)
            {

                work.timeCounter = 0L;
                this.renderingFreq = renderingFreq <= 0 ? 44100 : renderingFreq;
                this.opnaMasterClock = 7987200;
                if (chipsMasterClock != null && chipsMasterClock.Length > 0)
                {
                    this.opnaMasterClock = chipsMasterClock[0].Item2 <= 0 ? 7987200 : chipsMasterClock[0].Item2;
                }

                work.timerOPNA1 = new OPNATimer(renderingFreq, opnaMasterClock);
                work._8253timer = new _8253Timer(renderingFreq);
                //work.dma = new DMA(work);
                //work.cs4231 = new CS4231(renderingFreq, work.sound, work.dma);
                Log.WriteLine(LogLevel.TRACE, string.Format("OPNA MasterClock {0}", opnaMasterClock));
                Log.WriteLine(LogLevel.TRACE, "Start rendering.");

            }
        }

        public void StopRendering()
        {
            lock (work.SystemInterrupt)
            {
                if (work.Status > 0) work.Status = 0;
                Log.WriteLine(LogLevel.TRACE, "Stop rendering.");
            }
        }

        public void WriteRegister(ChipDatum reg)
        {
            throw new NotImplementedException();
        }

        // interface

        public static List<Tuple<string, string>> GetTags(byte[] buf)
        {
            throw new NotImplementedException();
        }


        public void WriteOPNAPRegister(ChipDatum reg)
        {
            lock (lockObjWriteReg)
            {
                if (reg.port == 0)
                {
                    bool? ret = work.timerOPNA1?.WriteReg((byte)reg.address, (byte)reg.data);
                    if (ret != null && (bool)ret)
                        work.currentTimer = 0;
                }

                WriteOPNAP?.Invoke(reg);
            }
        }

        public void WriteOPN2PRegister(ChipDatum reg)
        {
            lock (lockObjWriteReg)
            {
                //if (reg.port == 0)
                //{
                //    bool? ret = work.timerOPNA1?.WriteReg((byte)reg.address, (byte)reg.data);
                //    if (ret != null && (bool)ret)
                //        work.currentTimer = 0;
                //}

                WriteOPN2P?.Invoke(reg);
            }
        }

        public void WriteC4231Register(ChipDatum reg)
        {
            lock (lockObjWriteReg)
            {
                WriteC4231?.Invoke(reg);
            }
        }

        public byte ReadC4231Register(byte reg)
        {
            lock (lockObjWriteReg)
            {
                return ReadC4231(reg);
            }
        }

        private bool Write8253Register(byte arg1, byte arg2)
        {
            lock (lockObjWriteReg)
            {
                return work._8253timer.WriteReg(arg1, arg2);
            }
        }

        public void Int0bEnt()
        {
            nax.Int0bEntry();
        }
    }
}
