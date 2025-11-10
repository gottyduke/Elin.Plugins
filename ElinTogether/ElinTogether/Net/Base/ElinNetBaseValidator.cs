using System.Collections.Generic;
using ElinTogether.Helper;
using ElinTogether.Models;

namespace ElinTogether.Net;

public partial class ElinNetBase
{
    protected Dictionary<SourceListType, byte[]> SourceList = [];

    public void CreateValidation()
    {
        SourceList = SourceValidation.GenerateAll();

        EmpLog.Debug("Created source validation rules");
    }
}