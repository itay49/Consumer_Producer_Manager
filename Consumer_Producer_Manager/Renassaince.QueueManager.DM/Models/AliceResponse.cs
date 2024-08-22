using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class AliceResponse
    {
        public ResponseCode ResponseCode { get; set; }
        public string ErrorMessage { get; set; }
        public AliceData Data { get; set; }
    }

    public enum ResponseCode
    {
        OK = 1,
        ERROR_GENERAL = -1,
        ERROR_GetFromDBNoData = -2,
        ERROR_DataParserFailure = -3,
        ERROR_UnAuthorized = -4,
        ERROR_MissingConfig = -5,
        ERROR_BadRequest = -6,
        ERROR_EngineSignFailure = -7
    }

    public class AliceData
    {
        public bool HavePassed { get; set; }
        public List<EngineResult> EnginesResults { get; set; }

        public class EngineResult
        {
            public EngineCode EngineCode { get; set; }
            public string EngineName { get; set; }
            public bool IsSuccessful { get; set; }
            public string Data { get; set; }
            public string ErrorMessage { get; set; }
        }

        public enum EngineCode
        {
            DLP = 1,
            EngineSignature = 2,
            ProjectSignature = 3
        }
    }
}
