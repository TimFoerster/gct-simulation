
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Talk
{

    public float at;
    public float duration;
    public float end => at + duration;
    public int maxNumberOfAttendence;
    public ConferenceRoom room;
    public List<Chair> freeChairs;
    public ConferencePerson speaker;
    public List<ConferencePerson> attendances;
    public float excitement;
    public float probability;

    public Talk(float at, float duration, ConferenceRoom room, float excitement)
    {
        this.at = at;
        this.duration = duration;
        this.maxNumberOfAttendence = room.Chairs.Length + 1;
        this.attendances = new List<ConferencePerson>();
        this.room = room;
        this.freeChairs = new List<Chair>(room.Chairs);
        this.excitement = excitement;
    }

    public bool HasSpeaker => speaker != null;

    public void AddSpeaker(ConferencePerson speaker) => this.speaker = speaker;

    public Transform SpeakerPosition => room.SpeakerPosition;

    public float PullFactor => maxNumberOfAttendence * excitement;
}
