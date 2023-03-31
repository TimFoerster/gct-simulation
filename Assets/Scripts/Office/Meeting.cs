using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class Meeting
{
    public float at;
    public float duration;
    public float end => at + duration;
    public int maxNumberOfAttendence;
    public int currentNumberOfAttendence;
    public MeetingRoom room;
    public List<Chair> freeChairs;

    public Meeting(float at, float duration, int maxNumberOfAttendence, int currentNumberOfAttendence, MeetingRoom room)
    {
        this.at = at;
        this.duration = duration;
        this.maxNumberOfAttendence = maxNumberOfAttendence;
        this.currentNumberOfAttendence = currentNumberOfAttendence;
        this.room = room;
        this.freeChairs = room.Chairs.ToList();
    }

}
