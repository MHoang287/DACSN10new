using System;

namespace DACSN10.Models
{
    public class RoomResponse
    {
        public Guid roomId { get; set; }
        public string roomCode { get; set; }
        public long teacherId { get; set; }
        public bool active { get; set; }
    }

    public class JoinRoomResponse
    {
        public Guid roomId { get; set; }
        public Guid participantId { get; set; }
        public Guid? teacherParticipantId { get; set; }
    }

    public class CreateRoomRequest
    {
        public long teacherId { get; set; }
    }

    public enum ParticipantRole
    {
        TEACHER,
        STUDENT
    }

    public class JoinRoomRequest
    {
        public long userId { get; set; }
        public ParticipantRole role { get; set; }
        public string displayName { get; set; }
    }
}
