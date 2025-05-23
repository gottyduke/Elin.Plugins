﻿namespace Cwl.API;

public sealed record SerializableBioData : SerializableBioDataV1;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public record SerializableBioDataV1
{
    public string Background = "";
    public string Background_JP = "";
    public int Birthday = -1;
    public string Birthlocation = "";
    public string Birthlocation_JP = "";
    public int Birthmonth = -1;
    public string Birthplace = "";
    public string Birthplace_JP = "";
    public int Birthyear = -1;
    public string Dad = "";
    public string Dad_JP = "";
    public string Mom = "";
    public string Mom_JP = "";
}
// ReSharper restore All 