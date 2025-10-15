package com.example.livestreamservice.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class RoomResponse {
    private Long id;
    private String roomId;
    private String roomName;
    private String description;
    private String teacherName;
    private Long teacherId;
    private String status;
    private LocalDateTime createdAt;
    private Integer participantCount;
    private Integer maxParticipants;
}
