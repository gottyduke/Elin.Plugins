namespace ElinTogether.Common.Exceptions;

internal class ENetSourceDataChangedException(string sourceData) : ENetException($"Source data changed: {sourceData}");