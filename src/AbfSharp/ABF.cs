using System;
using System.IO;
using System.Runtime.InteropServices;
using AbfSharp.ABFFIO;

namespace AbfSharp
{
    /// <summary>
    /// Provides a C# interface to methods in ABFFIO.dll
    /// </summary>
    public class ABF : IDisposable
    {
        private readonly string AbfFilePath;
        private readonly UInt32 SweepCount;
        private readonly UInt32 MaxSamples;
        public readonly float[] DataBuffer;

        public Header Header;

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABF_IsABFFile(String szFileName, ref Int32 pnDataFormat, ref Int32 pnError);

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABF_ReadOpen(String szFileName, ref Int32 phFile, UInt32 fFlags, ref Header pFH, ref UInt32 puMaxSamples, ref UInt32 pdwMaxEpi, ref Int32 pnError);

        public ABF(string abfFilePath)
        {
            if (!File.Exists(abfFilePath))
                throw new ArgumentException($"file does not exist: {abfFilePath}");

            AbfFilePath = System.IO.Path.GetFullPath(abfFilePath);

            // ensure this file is a valid ABF
            Int32 dataFormat = 0;
            Int32 errorCode = 0;
            ABF_IsABFFile(abfFilePath, ref dataFormat, ref errorCode);
            if (errorCode != 0)
                throw new ArgumentException($"ABFFIO says not an ABF file: {abfFilePath}");

            // open the file and read its header
            Int32 fileHandle = 0;
            uint loadFlags = 0;
            ABF_ReadOpen(abfFilePath, ref fileHandle, loadFlags, ref Header, ref MaxSamples, ref SweepCount, ref errorCode);
            Error.AssertSuccess(errorCode);

            // create the sweep buffer in memory
            DataBuffer = new float[MaxSamples];
        }

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABF_Close(Int32 nFile, ref Int32 pnError);

        public void Dispose()
        {
            Int32 fileHandle = 0;
            Int32 errorCode = 0;
            ABF_Close(fileHandle, ref errorCode);
            Error.AssertSuccess(errorCode);
        }

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABF_ReadTags(Int32 nFile, ref Header pFH, UInt32 dwFirstTag, ref Tag pTagArray, UInt32 uNumTags, ref Int32 pnError);

        public Tag[] ReadTags()
        {
            Int32 fileHandle = 0;
            Int32 errorCode = 0;
            Tag[] abfTags = new Tag[(UInt32)Header.lNumTagEntries];
            for (uint i = 0; i < abfTags.Length; i++)
            {
                ABF_ReadTags(fileHandle, ref Header, i, ref abfTags[i], 1, ref errorCode);
                Error.AssertSuccess(errorCode);
            }
            return abfTags;
        }

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABF_ReadChannel(Int32 nFile, ref Header pFH, Int32 nChannel, Int32 dwEpisode, ref float pfBuffer, ref UInt32 puNumSamples, ref Int32 pnError);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sweepNumber">sweep number (starting at 1)</param>
        /// <param name="channelNumber">channel number (starting at 0)</param>
        public void ReadChannel(int sweepNumber, int channelNumber)
        {
            Int32 errorCode = 0;
            Int32 fileHandle = 0;
            UInt32 sampleCount = 0;
            int physicalChannel = Header.nADCSamplingSeq[channelNumber];
            ABF_ReadChannel(fileHandle, ref Header, physicalChannel, sweepNumber, ref DataBuffer[0], ref sampleCount, ref errorCode);
            Error.AssertSuccess(errorCode);
        }

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern int ABFH_GetEpochDuration(ref Header pFH, Int32 nChannel, Int32 dwEpisode, Int32 nEpoch);
        public int GetEpochDuration(int channelNumber, int sweepNumber, int epochNumber)
        {
            return ABFH_GetEpochDuration(ref Header, channelNumber, sweepNumber, epochNumber);
        }

        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern float ABFH_GetEpochLevel(ref Header pFH, Int32 nChannel, Int32 dwEpisode, Int32 nEpoch);
        public float GetEpochLevel(int channelNumber, int sweepNumber, int epochNumber)
        {
            return ABFH_GetEpochLevel(ref Header, channelNumber, sweepNumber, epochNumber);
        }

        // Return the bounds of a given epoch in a given episode. 
        // Values returned are ZERO relative (not relative to start of sweep)
        [DllImport("ABFFIO/ABFFIO.dll", CharSet = CharSet.Ansi)]
        private static extern bool ABFH_GetEpochLimits(ref Header pFH,
            Int32 nADCChannel, Int32 uDACChannel, Int32 dwEpisode, Int32 nEpoch,
            ref UInt32 puEpochStart, ref UInt32 puEpochEnd, ref Int32 pnError);
        public (bool valid, int start, int end) GetEpochLimits(int channelNumber, int sweepNumber, int epochNumber)
        {
            UInt32 puEpochStart = 0;
            UInt32 puEpochEnd = 0;
            Int32 pnError = 0;
            bool valid = ABFH_GetEpochLimits(ref Header,
                channelNumber, channelNumber, sweepNumber, epochNumber,
                ref puEpochStart, ref puEpochEnd, ref pnError);

            return (valid, (int)puEpochStart, (int)puEpochEnd);
        }

        // Get the duration of the first/last holding period.
        [Obsolete("read this using the epoch module")]
        public int GetHoldingLength()
        {
            int nSweepLength = Header.lNumSamplesPerEpisode;
            int nNumChannels = Header.nADCNumChannels;

            int nHoldingCount = nSweepLength / Constants.ABFH_HOLDINGFRACTION;
            nHoldingCount -= nHoldingCount % nNumChannels;
            if (nHoldingCount < nNumChannels)
                nHoldingCount = nNumChannels;
            return nHoldingCount;
        }

        public double[] ReadAllSweeps(int channel = 0)
        {
            double[] data = new double[SweepCount * MaxSamples];
            uint offset = 0;
            for (int i=0; i<SweepCount; i++)
            {
                // TODO: account for variable sweep lengths
                uint sweepPointCount = MaxSamples;
                ReadChannel(i + 1, channel);
                Array.Copy(DataBuffer, 0, data, offset, sweepPointCount);
                offset += sweepPointCount;
            }
            return data;
        }
    }
}
