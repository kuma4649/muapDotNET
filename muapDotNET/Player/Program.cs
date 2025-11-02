using muapDotNET.Common;
using muapDotNET.Driver;
using musicDriverInterface;
using NAudio.Wave;
using System.Collections;

Log.writeLine += WriteLine;
#if DEBUG
//Log.writeLine += WriteLineF;
Log.level = LogLevel.TRACE;
#else
Log.level = LogLevel.INFO;
#endif

Log.WriteLine(LogLevel.INFO, "Hello, muapDotNET!");

int device = 0;
int loop = 0;
int latency = 1000;
MDSound.MDSound mds = null;
short[] emuRenderBuf = new short[2];
uint SamplingRate = 55467;//44100;
uint samplingBuffer = 1024;
DirectSoundOut audioOutput = null;
naudioCallBack callBack = null;
uint opnaMasterClock = 7987200;
//bool isLoadADPCM = true;

int MAXBUF = 18;
int FIFO_SIZE = 128;
byte[] fifoBuf = new byte[FIFO_SIZE * MAXBUF * 2];

int fnIndex = AnalyzeOption(args);
if (args == null || args.Length != fnIndex + 1)
{
    Log.WriteLine(LogLevel.ERROR, "引数(.o/.oyファイル)１個欲しいよぉ");
    Environment.Exit(-1);
}
if (!File.Exists(args[fnIndex]))
{
    Log.WriteLine(LogLevel.ERROR, "ファイルが見つかりません");
    Environment.Exit(-1);
}


List<MmlDatum> bl = [];
byte[] srcBuf = File.ReadAllBytes(args[fnIndex]);
foreach (byte b in srcBuf) bl.Add(new MmlDatum(b));
MmlDatum[] blary = [.. bl];

SineWaveProvider16 waveProvider;
waveProvider = new SineWaveProvider16();
waveProvider.SetWaveFormat((int)SamplingRate, 2);
audioOutput = new DirectSoundOut(latency);
audioOutput.Init(waveProvider);

List<MDSound.MDSound.Chip> lstChips = new List<MDSound.MDSound.Chip>();
MDSound.MDSound.Chip chip = null;

MDSound.ym2608 ym2608 = new MDSound.ym2608();
for (int i = 0; i < 1; i++)
{
    chip = new MDSound.MDSound.Chip
    {
        type = MDSound.MDSound.enmInstrumentType.YM2608,
        ID = (byte)i,
        Instrument = ym2608,
        Update = ym2608.Update,
        Start = ym2608.Start,
        Stop = ym2608.Stop,
        Reset = ym2608.Reset,
        SamplingRate = SamplingRate,
        Clock = opnaMasterClock,
        Volume = 0,
        Option = null// new object[] { GetApplicationFolder() }
    };
    lstChips.Add(chip);
}
MDSound.ym3438 ym3438 = new MDSound.ym3438();
for (int i = 0; i < 1; i++)
{
    chip = new MDSound.MDSound.Chip
    {
        type = MDSound.MDSound.enmInstrumentType.YM3438,
        ID = (byte)i,
        Instrument = ym3438,
        Update = ym3438.Update,
        Start = ym3438.Start,
        Stop = ym3438.Stop,
        Reset = ym3438.Reset,
        SamplingRate = SamplingRate,
        Clock = opnaMasterClock,
        Volume = 0,
        Option = null// new object[] { GetApplicationFolder() }
    };
    lstChips.Add(chip);
}
MDSound.CS4231 cS4231 = new MDSound.CS4231();
for (int i = 0; i < 1; i++)
{
    chip = new MDSound.MDSound.Chip
    {
        type = MDSound.MDSound.enmInstrumentType.CS4231,
        ID = (byte)i,
        Instrument = cS4231,
        Update = cS4231.Update,
        Start = cS4231.Start,
        Stop = cS4231.Stop,
        Reset = cS4231.Reset,
        SamplingRate = SamplingRate,
        Clock = 0,
        Volume = 0,
        Option = null
    };
    lstChips.Add(chip);
}

mds = new MDSound.MDSound(SamplingRate, samplingBuffer
    , lstChips.ToArray());

mds.SetVolumeYM2608PSG(-10);
mds.SetVolumeYM2608Rhythm(5);

//mds.SetVolumeYM2608FM(-127);
//mds.SetVolumeYM2608PSG(0);
//mds.SetVolumeYM2608Adpcm(-127);
//mds.SetVolumeYM2608Rhythm(-127);

List<ChipAction> lca = new List<ChipAction>();
muapChipAction ca;
ca = new muapChipAction(OPNAWriteP, null, null); lca.Add(ca);
ca = new muapChipAction(OPN2WriteP, null, null); lca.Add(ca);
ca = new muapChipAction(CS4231Write, null, null); lca.Add(ca);

IDictionary envVars = Environment.GetEnvironmentVariables();
muapDotNET.Driver.Driver drv = new(envVars);
//drv.work.fifoBuf = fifoBuf;
drv.Init(lca, blary, null, (object)(new object[] {
    (Func<byte,byte>)CS4231Read,
    (Func<byte[]>)CS4231EMS_GetCrntMapBuf,
    (iDriver.dlgEMS_Map)CS4231EMS_Map,
    (Func<ushort>)CS4231EMS_GetPageMap,
    (iDriver.dlgEMS_GetHandleName)CS4231EMS_GetHandleName,
    (iDriver.dlgEMS_SetHandleName)CS4231EMS_SetHandleName,
    (iDriver.dlgEMS_AllocMemory)CS4231EMS_AllocMemory,
    null,
    0 ,
    null
}));
callBack = EmuCallback;
waveProvider.callback = callBack;



System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

Log.WriteLine(LogLevel.INFO, "終了する場合は何かキーを押してください");

drv.StartRendering((int)SamplingRate,
    [
        new Tuple<string, int>("YM2608",(int)opnaMasterClock)
    ]
);
drv.MusicSTART(0);
cS4231.setFIFOBuf(0, drv.work.fifoBuf);
//cS4231.setInt0bEnt(0, drv.Int0bEnt);

audioOutput.Play();

while (true)
{
    System.Threading.Thread.Sleep(1);
    if (Console.KeyAvailable) break;
    //ステータスが0(終了)又は0未満(エラー)の場合はループを抜けて終了
    if (drv.GetStatus() <= 0)
    {
        if (drv.GetStatus() == 0)
        {
            System.Threading.Thread.Sleep((int)(latency * 2.0));//実際の音声が発音しきるまでlatency*2の分だけ待つ
        }
        break;
    }
    if(drv.GetNowLoopCounter()>0)
    {
        System.Threading.Thread.Sleep((int)(latency * 2.0));//実際の音声が発音しきるまでlatency*2の分だけ待つ
        break;
    }
}

System.Threading.Thread.Sleep(2000);
drv.MusicSTOP();
drv.StopRendering();





int AnalyzeOption(string[] args)
{
    int i = 0;

    device = 0;
    loop = 0;
    //isLoadADPCM = true;

    while (i < args.Length && args[i] != null && args[i].Length > 0 && args[i][0] == '-')
    {
        string op = args[i].Substring(1).ToUpper();
        if (op == "D=EMU") device = 0;
        if (op == "D=GIMIC") device = 1;
        if (op == "D=SCCI") device = 2;
        if (op == "D=WAVE") device = 3;

        //if (op.Length > 2 && op.Substring(0, 2) == "L=")
        //{
        //    if (!int.TryParse(op.Substring(2), out loop))
        //    {
        //        loop = 0;
        //    }
        //}

        //if (op.Length > 10 && op.Substring(0, 10) == "LOADADPCM=")
        //{
        //    if (op.Substring(10) == "ONLY")
        //    {
        //        loadADPCMOnly = true;
        //        isLoadADPCM = true;
        //    }
        //    else
        //    {
        //        loadADPCMOnly = false;
        //        if (!bool.TryParse(op.Substring(10), out isLoadADPCM))
        //        {
        //            isLoadADPCM = true;
        //        }
        //    }
        //}

        i++;
    }

    if (device == 3 && loop == 0) loop = 1;

    return i;
}

static void WriteLine(LogLevel level, string msg)
{
    Console.WriteLine("[{0,-7}] {1}", level, msg);
}

static void WriteLineF(LogLevel level, string msg)
{
    try
    {
        File.AppendAllText(@"C:\Users\kuma\Desktop\new.log",
            string.Format("[{0,-7}] {1}" + Environment.NewLine, level, msg));
    }
    catch { }
}

int EmuCallback(short[] buffer, int offset, int count)
{
    try
    {
        long bufCnt = count / 2;

        for (int i = 0; i < bufCnt; i++)
        {
            mds.Update(emuRenderBuf, 0, 2, OneFrame);

            emuRenderBuf[0] = (short)Math.Min(Math.Max(emuRenderBuf[0] + drv.sound[0], short.MinValue), short.MaxValue);
            emuRenderBuf[1] = (short)Math.Min(Math.Max(emuRenderBuf[1] + drv.sound[1], short.MinValue), short.MaxValue);

            buffer[offset + i * 2 + 0] = emuRenderBuf[0];
            buffer[offset + i * 2 + 1] = emuRenderBuf[1];

        }
    }
    catch//(Exception ex)
    {
        //Log.WriteLine(LogLevel.FATAL, string.Format("{0} {1}", ex.Message, ex.StackTrace));
    }

    return count;
}

void OneFrame()
{
    drv.Rendering();
}

void OPNAWriteP(ChipDatum dat)
{
    //Log.WriteLine(LogLevel.TRACE, string.Format("Write OPNA : Prt:${0:X02} Adr:${1:X02} Dat:${2:X02}", dat.port, dat.address, dat.data));
    OPNAWrite(0, dat);
}
void OPN2WriteP(ChipDatum dat)
{
    //Log.WriteLine(LogLevel.TRACE, string.Format("Write OPN2 : Prt:${0:X02} Adr:${1:X02} Dat:${2:X02}", dat.port, dat.address, dat.data));
    OPN2Write(0, dat);
}

void OPNAWrite(int chipId, ChipDatum dat)
{
    if (dat != null && dat.addtionalData != null)
    {
        MmlDatum md = (MmlDatum)dat.addtionalData;
        if (md.linePos != null)
        {
            //Log.WriteLine(LogLevel.TRACE, string.Format("! OPNA i{0} r{1} c{2}"
            //, chipId
            //, md.linePos.row
            //, md.linePos.col
            //));
        }
    }

    if (dat.address == -1) return;
    if(dat.port == 0 && dat.address >= 0x08 && dat.address <= 0x0a)
    {
//        Log.WriteLine(LogLevel.TRACE, string.Format("Out ChipA:{0} Port:{1} Adr:[{2:x02}] val[{3:x02}]", chipId, dat.port, (int)dat.address, (int)dat.data));
    }

    mds.WriteYM2608((byte)chipId, (byte)dat.port, (byte)dat.address, (byte)dat.data);
}

void OPN2Write(int chipId, ChipDatum dat)
{
    if (dat != null && dat.addtionalData != null)
    {
        MmlDatum md = (MmlDatum)dat.addtionalData;
        if (md.linePos != null)
        {
            //Log.WriteLine(LogLevel.TRACE, string.Format("! OPNA i{0} r{1} c{2}"
            //, chipId
            //, md.linePos.row
            //, md.linePos.col
            //));
        }
    }

    if (dat.address == -1) return;
    //Log.WriteLine(LogLevel.TRACE, string.Format("Out ChipA:{0} Port:{1} Adr:[{2:x02}] val[{3:x02}]", chipId, dat.port, (int)dat.address, (int)dat.data));
    if (dat.port > 0 && dat.address >= 0x90 && dat.address <= 0x9e)
    {
        ;
    }
    if (dat.port > 0)
    {
        ;
    }

    mds.WriteYM3438((byte)chipId, (byte)dat.port, (byte)dat.address, (byte)dat.data);
}

void CS4231Write(ChipDatum dat)
{
    mds.WriteCS4231((byte)0, (byte)dat.port, (byte)dat.address, (byte)dat.data);
}

byte CS4231Read(byte adr)
{
    return mds.ReadCS4231((byte)0, (byte)adr);
}

byte[] CS4231EMS_GetCrntMapBuf()
{
    return cS4231.EMS_GetCrntMapBuf(0);
}

void CS4231EMS_Map(byte al, ref byte ah, ushort bx, ushort dx)
{
    cS4231.EMS_Map(0, al, ref ah, bx, dx);
}

ushort CS4231EMS_GetPageMap()
{
    return cS4231.EMS_GetPageMap(0);
}

void CS4231EMS_GetHandleName(ref byte ah, ushort dx, ref string sbuf)
{
    cS4231.EMS_GetHandleName(0, ref ah, dx, ref sbuf);
}

void CS4231EMS_SetHandleName(ref byte ah, ushort dx, string emsname2)
{
    cS4231.EMS_SetHandleName(0, ref ah, dx, emsname2);
}

void CS4231EMS_AllocMemory(ref byte ah, ref ushort dx, ushort bx)
{
    cS4231.EMS_AllocMemory(0, ref ah, ref dx, bx);
}



public class SineWaveProvider16 : WaveProvider16
{
    public naudioCallBack callback;

    public SineWaveProvider16()
    {
    }

    public override int Read(short[] buffer, int offset, int sampleCount)
    {

        return callback(buffer, offset, sampleCount);

    }

}

public delegate int naudioCallBack(short[] buffer, int offset, int sampleCount);

