namespace ElinTogether.Common.Exceptions;

internal class ENetInvalidZoneException(int uid) : ENetException($"Zone state is invalid, uid '{uid}'");