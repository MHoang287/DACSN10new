package com.example.livestreamservice.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class CreateRoomRequest {
    
    @NotBlank(message = "Room name is required")
    private String roomName;
    
    private String description;
    
    private Integer maxParticipants = 100;
}
