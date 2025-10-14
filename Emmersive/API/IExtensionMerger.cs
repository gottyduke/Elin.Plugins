using System.Collections.Generic;

namespace Emmersive.API;

public interface IExtensionMerger
{
    public void MergeExtensionData(IDictionary<string, object> data);
}