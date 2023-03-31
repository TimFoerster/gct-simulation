public interface IHasRecorder<T>
{
    public BLERecorder<T> Recorder { get; }
    public int DeviceId { get; }
    public BLEDeviceType DeviceType { get; }   

    public string GlobalName { get; }

    public int GlobalIndex { get; }

    public int LocalIndex { get; }
    public string LocalName { get; }

    public bool Synced { get; set; }

    public void DisableRecording();

    public bool IsRecording { get; }
}
