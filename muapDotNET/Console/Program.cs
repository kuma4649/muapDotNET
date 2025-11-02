using muapDotNET.Common;
using muapDotNET.Compiler;
using musicDriverInterface;
using System;
using System.Text;
using System.Xml.Serialization;

namespace muapDotNET.Console
{
    class Program
    {
        private static string srcFile;

        static void Main(string[] args)
        {
            Log.writeLine = WriteLine;
#if DEBUG
            Log.level = LogLevel.INFO;//.INFO;
            Log.off = 0;
#else
            Log.level = LogLevel.INFO;
#endif
            Log.WriteLine(LogLevel.INFO, "Hello, muapDotNET!");

            int fnIndex = AnalyzeOption(args);

            if (args == null || args.Length < 1 + fnIndex)
            {
                WriteLine(LogLevel.INFO, msg.get("I0600"));
                return;
            }

            try
            {
#if NETCOREAPP
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif

                Compile(args[fnIndex], (args.Length > fnIndex + 1 ? args[fnIndex + 1] : null));

            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.FATAL, ex.Message);
                Log.WriteLine(LogLevel.FATAL, ex.StackTrace);
            }

        }

        private static void Compile(string srcFile, string destFile = null)
        {
            try
            {
                if (Path.GetExtension(srcFile) == "" && !File.Exists(srcFile))
                    srcFile = Path.Combine(
                    Path.GetDirectoryName(Path.GetFullPath(srcFile))
                    , string.Format("{0}.mus", Path.GetFileNameWithoutExtension(srcFile))
                    );

                srcFile = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(srcFile))
                , srcFile
                );

                Program.srcFile = srcFile;
                MyEncoding mye = new MyEncoding();
                Compiler.Compiler compiler = new Compiler.Compiler(mye);
                compiler.Init();

                //compiler.SetCompileSwitch("IDE");
                //compiler.SetCompileSwitch("SkipPoint=R19:C30");

                string destFileName = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(srcFile)), string.Format("{0}.o", Path.GetFileNameWithoutExtension(srcFile)));
                if (destFile != null)
                {
                    destFileName = destFile;
                }

                if (!File.Exists(srcFile))
                {
                    Log.WriteLine(LogLevel.ERROR, string.Format(msg.get("E0601"), srcFile));
                    return;
                }

                bool isSuccess = false;
                using (FileStream sourceMML = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream destCompiledBin = new MemoryStream())
                using (Stream bufferedDestStream = new BufferedStream(destCompiledBin))
                {
                    isSuccess = compiler.Compile(sourceMML, bufferedDestStream, appendFileReaderCallback);

                    if (isSuccess)
                    {
                        bufferedDestStream.Flush();
                        byte[] destbuf = destCompiledBin.ToArray();
                        File.WriteAllBytes(destFileName, destbuf);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.FATAL, ex.Message);
                Log.WriteLine(LogLevel.FATAL, ex.StackTrace);
            }
            finally
            {
            }

        }

        private static Stream appendFileReaderCallback(string arg)
        {

            string fn = Path.Combine(
                Path.GetDirectoryName(srcFile)
                , arg
                );

            if (!File.Exists(fn)) return null;

            FileStream strm;
            try
            {
                strm = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                strm = null;
            }

            return strm;
        }

        private static int AnalyzeOption(string[] args)
        {
            if (args.Length < 1) return 0;

            int i = 0;
            while (i < args.Length && args[i] != null && args[i].Length > 0 && args[i][0] == '-')
            {
                string op = args[i].Substring(1).ToUpper();
                if (op == "LOGLEVEL=FATAL")
                {
                    Log.level = LogLevel.FATAL;
                }
                else if (op == "LOGLEVEL=ERROR")
                {
                    Log.level = LogLevel.ERROR;
                }
                else if (op == "LOGLEVEL=WARNING")
                {
                    Log.level = LogLevel.WARNING;
                }
                else if (op == "LOGLEVEL=INFO")
                {
                    Log.level = LogLevel.INFO;
                }
                else if (op == "LOGLEVEL=DEBUG")
                {
                    Log.level = LogLevel.DEBUG;
                }
                else if (op == "LOGLEVEL=TRACE")
                {
                    Log.level = LogLevel.TRACE;
                }

                if (op == "OFFLOG=WARNING")
                {
                    Log.off = (int)LogLevel.WARNING;
                }

                i++;
            }

            return i;
        }

        static void WriteLine(LogLevel level, string msg)
        {
            if (level > Log.level) return;
            System.Console.WriteLine("[{0,-7}] {1}", level, msg);
        }
    }
}