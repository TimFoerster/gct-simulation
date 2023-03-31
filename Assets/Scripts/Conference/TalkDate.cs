using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class TalkDate
{
    public float at;
    public float duration;
    public Talk talk;
    public float end;
    public Transform position;
    public bool speaker;

    public TalkDate(float at, float duration, Talk talk, Transform position, bool speaker = false)
    {
        this.at = at;
        this.duration = duration;
        this.talk = talk;
        this.position = position;
        this.speaker = speaker;
        this.end = at + duration;
    }
}
