package com.example.livestreamservice.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class SignalingMessage {
    private String type; // offer, answer, ice-candidate, join, leave
    private String roomId;
    private String from;
    private String to;
    private Object data; // SDP or ICE candidate data
}
