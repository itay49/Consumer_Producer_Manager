using System;
using System.Collections.Generic;
using System.Text;

namespace Renassaince.QueueManager
{
    public enum DataAccessEnum
    {
        UNKNOWN = -1,
        EMS = 0,
        RABBIT = 1,
        MEMORY = 2,
        File = 3,
        SIGN_ADAPTER = 4,
        REST = 5,
        AMQ = 6
    }
}
