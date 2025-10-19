using System.Collections.Generic;
using System.Net.Http;

namespace Emmersive.API;

public interface IExtensionRequestMerger
{
    public void MergeExtensionRequest(IDictionary<string, object> data, HttpRequestMessage request);
}