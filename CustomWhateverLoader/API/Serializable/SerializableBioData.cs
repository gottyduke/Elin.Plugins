namespace Cwl.API;

public record SerializableBioData : SerializableBioDataV1;

#pragma warning disable CS0649
#pragma warning disable CS0414
// ReSharper disable All 
public record SerializableBioDataV1
{
    public int Birthday = -1;
    public int Birthmonth = -1;
    public int Birthyear = -1;
    public string Dad = "";
    public string Dad_JP = "";
    public string Mom = "";
    public string Mom_JP = "";
    public string Background = "";
    public string Background_JP = "";
    public string Birthlocation = "";
    public string Birthlocation_JP = "";
    public string Birthplace = "";
    public string Birthplace_JP = "";
}
// ReSharper restore All 