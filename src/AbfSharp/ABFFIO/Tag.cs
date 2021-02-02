using System;
using System.Runtime.InteropServices;

namespace AbfSharp.ABFFIO
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Tag
    {
        public Int32 lTagTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.ABF_TAGCOMMENTLEN)] public char[] sComment;
        public Int16 nTagType;
        public Int16 nVoiceTagNumber_or_nAnnotationIndex;
    };
}
