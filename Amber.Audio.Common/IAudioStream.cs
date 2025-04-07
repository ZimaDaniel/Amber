namespace Amber.Audio;

public delegate void DataStreamEventHandler(byte[] data, bool endOfStream);

public interface IAudioStream
{
    event DataStreamEventHandler? DataStreamed;

    void Reset();
}
