using muapDotNET.Common;
using musicDriverInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace muapDotNET.Compiler
{
    public class Compiler : iCompiler
    {
        private IEncoding enc = null;
        private byte[] srcBuf = null;
        private Work work = null;
        private string tone_path = "TONES.DTA";// + new string((char)0, 55);

        public Compiler(IEncoding enc = null)
        {
            this.enc = enc ?? MyEncoding.Default;
        }

        public MmlDatum[] Compile(Stream sourceMML, Func<string, Stream> appendFileReaderCallback)
        {
            muap98 muap98 = null;
            try
            {
                if (work == null) work = new Work();
                work.compilerInfo = new CompilerInfo();

                srcBuf = ReadAllBytesFromText(sourceMML);
                x86Register r = new x86Register();
                IDictionary envVars = Environment.GetEnvironmentVariables();
                if (envVars.Contains("DTA"))
                {
                    tone_path = Path.Combine(envVars["DTA"].ToString(), tone_path);
                }
                muap98 = new muap98(srcBuf, r,tone_path,work);
                MENU menu = new MENU(r, muap98);
                MUCOM2 mc2 = new MUCOM2(r, menu, muap98, null, work);
                MUCOMSUB mucomcub = new MUCOMSUB(r, mc2, muap98, work);
                mc2.mucomsub = mucomcub;
                mc2.Init();

                mc2.compile();

                AutoExtendList<MmlDatum> obj = muap98.object_Buf;
                if (obj.Count > 0 && obj[0] != null)
                {
                    //ラベルのチェック
                    bool fnd = false;
                    for (int i = 0; i < mc2.mucomsub.labelAdrs.Length; i++)
                    {
                        if (mc2.mucomsub.labelAdrs[i] != 0)
                        {
                            fnd = true;
                            break;
                        }
                    }
                    //音色情報を追加しておく
                    obj[0].args = new List<object>() { muap98.toneBuff, fnd ? mc2.mucomsub.labelAdrs : null };
                }

                return obj.ToArray();
            }
            catch (MusException me)
            {
                //if (work.compilerInfo == null) work.compilerInfo = new CompilerInfo();
                //work.compilerInfo.errorList.Add(new Tuple<int, int, string>(-1, -1, me.Message));
                Log.WriteLine(LogLevel.ERROR, me.Message);
            }
            catch (Exception e)
            {
                if (work.compilerInfo == null) work.compilerInfo = new CompilerInfo();
                work.compilerInfo.errorList.Add(new Tuple<int, int, string>(-1, -1, e.Message));
                Log.WriteLine(LogLevel.ERROR, string.Format(
                    msg.get("E0000")
                    , e.Message
                    , e.StackTrace));
            }

#if DEBUG
            if (muap98 != null && muap98.object_Buf != null)
            {
                for (int j = 0; j < 16 * 16; j++)
                {
                    string hex = "";
                    hex = string.Format("{0:X04}: ", j * 16);
                    for (int i = 0; i < 16; i++)
                    {
                        hex += string.Format("{0:X02} ", muap98.object_Buf[i + j * 16]);
                    }
                    Log.WriteLine(LogLevel.TRACE, hex);
                }
            }
#endif

            return null;
        }

        public bool Compile(FileStream sourceMML, Stream destCompiledBin, Func<string, Stream> appendFileReaderCallback)
        {
            var dat = Compile(sourceMML, appendFileReaderCallback);
            if (dat == null)
            {
                return false;
            }
            foreach (MmlDatum md in dat)
            {
                if (md == null)
                {
                    destCompiledBin.WriteByte(0);
                }
                else
                {
                    destCompiledBin.WriteByte((byte)md.dat);
                }
            }
            return true;
        }

        public CompilerInfo GetCompilerInfo()
        {
            if(work.compilerInfo == null)
            {
                work.compilerInfo = new CompilerInfo();
            }
            return work.compilerInfo;
        }

        public GD3Tag GetGD3TagInfo(byte[] srcBuf)
        {
            if (
                work == null 
                || work.compilerInfo == null
                || !(work.compilerInfo.addtionalInfo is GD3Tag)
                )
                return null;

            return (GD3Tag)work.compilerInfo.addtionalInfo;
        }

        public void Init()
        {
            //throw new NotImplementedException();
        }

        public void SetCompileSwitch(params object[] param)
        {
            if (work == null) work = new Work();

            foreach (var p in param)
            {
                if (p is KeyValuePair<string, string>)
                {
                    KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)p;
                    if (kvp.Key.ToUpper() == "SOURCEFILENAME")
                    {
                        work.sourceFileName = kvp.Value;
                    }
                }
            }
        }



        /// <summary>
        /// ストリームから一括でバイナリを読み込む
        /// </summary>
        private byte[] ReadAllBytes(Stream stream)
        {
            if (stream == null) return null;

            var buf = new byte[8192];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var r = stream.Read(buf, 0, buf.Length);
                    if (r < 1)
                    {
                        break;
                    }
                    ms.Write(buf, 0, r);
                }
                return ms.ToArray();
            }
        }

        private byte[] ReadAllBytesFromText(Stream stream)
        {
            if (stream == null) return null;

            var buf = new byte[8192];
            using (var sr = new StreamReader(stream,((MyEncoding)enc).sjis))
            {
                using (var ms = new MemoryStream())
                {
                    while (!sr.EndOfStream)
                    {
                        var r = sr.ReadLine();
                        ms.Write(enc.GetSjisArrayFromString(r));
                        ms.Write(new byte[] { 0xfe });
                    }
                    ms.Write(new byte[] { 0xff });
                    return ms.ToArray();
                }
            }
        }

    }
}
