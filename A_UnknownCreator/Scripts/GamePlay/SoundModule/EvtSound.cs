using UnityEngine;

public struct EvtSoundPlayEnd
{
    public string name;
    public float volume;
    public Vector3 position;

    public EvtSoundPlayEnd(string name, float volume, Vector3 position)
    {
        this.name = name;
        this.volume = volume;
        this.position = position;
    }
}
