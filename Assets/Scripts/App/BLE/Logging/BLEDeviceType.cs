using System;
using System.Runtime.Serialization;

[Serializable]
public enum BLEDeviceType
{

    [EnumMember(Value = "d")]
    Device,

    [EnumMember(Value = "b")]
    Beacon
}