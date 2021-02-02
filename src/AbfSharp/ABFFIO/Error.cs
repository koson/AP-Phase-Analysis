using System;

namespace AbfSharp.ABFFIO
{
    public class Error
    {
        public static void AssertSuccess(Int32 errorCode, bool exceptOnError = true)
        {
            if (exceptOnError && errorCode != 0)
                throw new Exception($"ABFFIO error code: {errorCode} ({GetDescription(errorCode)})");
        }

        public static string GetDescription(Int32 errorCode) => errorCode switch
        {
            0 => "ABF_SUCCESS",
            1001 => "ABF_EUNKNOWNFILETYPE",
            1002 => "ABF_EBADFILEINDEX",
            1003 => "ABF_TOOMANYFILESOPEN",
            1004 => "ABF_EOPENFILE - could not open file",
            1005 => "ABF_EBADPARAMETERS",
            1006 => "ABF_EREADDATA",
            1008 => "ABF_OUTOFMEMORY",
            1009 => "ABF_EREADSYNCH",
            1010 => "ABF_EBADSYNCH",
            1011 => "ABF_EEPISODERANGE - invalid sweep number",
            1012 => "ABF_EINVALIDCHANNEL",
            1013 => "ABF_EEPISODESIZE",
            1014 => "ABF_EREADONLYFILE",
            1015 => "ABF_EDISKFULL",
            1016 => "ABF_ENOTAGS",
            1017 => "ABF_EREADTAG",
            1018 => "ABF_ENOSYNCHPRESENT",
            1019 => "ABF_EREADDACEPISODE",
            1020 => "ABF_ENOWAVEFORM",
            1021 => "ABF_EBADWAVEFORM",
            1022 => "ABF_BADMATHCHANNEL",
            1023 => "ABF_BADTEMPFILE",
            1025 => "ABF_NODOSFILEHANDLES",
            1026 => "ABF_ENOSCOPESPRESENT",
            1027 => "ABF_EREADSCOPECONFIG",
            1028 => "ABF_EBADCRC",
            1029 => "ABF_ENOCOMPRESSION",
            1030 => "ABF_EREADDELTA",
            1031 => "ABF_ENODELTAS",
            1032 => "ABF_EBADDELTAID",
            1033 => "ABF_EWRITEONLYFILE",
            1034 => "ABF_ENOSTATISTICSCONFIG",
            1035 => "ABF_EREADSTATISTICSCONFIG",
            1036 => "ABF_EWRITERAWDATAFILE",
            1037 => "ABF_EWRITEMATHCHANNEL",
            1038 => "ABF_EWRITEANNOTATION",
            1039 => "ABF_EREADANNOTATION",
            1040 => "ABF_ENOANNOTATIONS",
            1041 => "ABF_ECRCVALIDATIONFAILED",
            1042 => "ABF_EWRITESTRING",
            1043 => "ABF_ENOSTRINGS",
            1044 => "ABF_EFILECORRUPT",
            _ => throw new ArgumentException($"unknown error code {errorCode}")
        };
    }
}
