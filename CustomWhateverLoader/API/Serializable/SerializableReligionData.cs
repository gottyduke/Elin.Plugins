using System.Collections.Generic;

namespace Cwl.API;

public sealed class SerializableReligionElement : Dictionary<string, string[]>;

public sealed class SerializableReligionOffering : Dictionary<string, Dictionary<string, int>>;