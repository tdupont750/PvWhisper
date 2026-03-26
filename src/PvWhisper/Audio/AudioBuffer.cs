namespace PvWhisper.Audio;

/// <summary>
/// Wraps a buffer of 16-bit PCM audio samples captured from the microphone.
/// </summary>
public sealed record AudioBuffer(short[] Samples);
