using System;
using System.Text;

namespace muapDotNET.Common
{
    public class MyEncoding : IEncoding
    {
        private static Lazy<MyEncoding> defaultEncoding;
        public Encoding sjis;

        static MyEncoding()
        {
            defaultEncoding = new Lazy<MyEncoding>(() => new MyEncoding(), true);
        }

        public MyEncoding()
        {
            try
            {
                sjis = Encoding.GetEncoding("shift_jis");
            }
            catch
            {
                sjis = Encoding.UTF8;
            }
        }

        public static IEncoding Default => defaultEncoding.Value;

        public byte[] GetSjisArrayFromString(string utfString) => sjis.GetBytes(utfString);
        public string GetStringFromSjisArray(byte[] sjisArray) => sjis.GetString(sjisArray);
        public string GetStringFromSjisArray(byte[] sjisArray, int index, int count) => sjis.GetString(sjisArray, index, count);
        public string GetStringFromUtfArray(byte[] utfArray) => Encoding.UTF8.GetString(utfArray);
        public byte[] GetUtfArrayFromString(string utfString) => Encoding.UTF8.GetBytes(utfString);
    }
}