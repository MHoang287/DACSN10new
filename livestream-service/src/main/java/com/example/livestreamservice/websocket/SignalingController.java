package com.example.livestreamservice.websocket;

import com.example.livestreamservice.dto.SignalingMessage;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.messaging.handler.annotation.DestinationVariable;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Controller;

@Controller
public class SignalingController {
    
    private static final Logger logger = LoggerFactory.getLogger(SignalingController.class);
    
    @Autowired
    private SimpMessagingTemplate messagingTemplate;
    
    /**
     * Handle WebRTC signaling messages (offer, answer, ice-candidate)
     * Messages are sent to /app/signal/{roomId}
     * Messages are broadcast to /topic/signal/{roomId}
     */
    @MessageMapping("/signal/{roomId}")
    public void handleSignaling(@DestinationVariable String roomId, @Payload SignalingMessage message) {
        logger.info("Received signaling message: type={}, roomId={}, from={}, to={}", 
                message.getType(), roomId, message.getFrom(), message.getTo());
        
        message.setRoomId(roomId);
        
        // If message has a specific recipient (to field), send to that user only
        if (message.getTo() != null && !message.getTo().isEmpty()) {
            messagingTemplate.convertAndSend("/queue/signal/" + message.getTo(), message);
            logger.info("Sent signaling message to user: {}", message.getTo());
        } else {
            // Otherwise, broadcast to all users in the room
            messagingTemplate.convertAndSend("/topic/signal/" + roomId, message);
            logger.info("Broadcast signaling message to room: {}", roomId);
        }
    }
    
    /**
     * Handle user joining a room
     */
    @MessageMapping("/join/{roomId}")
    public void handleJoin(@DestinationVariable String roomId, @Payload SignalingMessage message) {
        logger.info("User {} joined room {}", message.getFrom(), roomId);
        
        message.setType("join");
        message.setRoomId(roomId);
        
        // Notify all users in the room about the new participant
        messagingTemplate.convertAndSend("/topic/signal/" + roomId, message);
    }
    
    /**
     * Handle user leaving a room
     */
    @MessageMapping("/leave/{roomId}")
    public void handleLeave(@DestinationVariable String roomId, @Payload SignalingMessage message) {
        logger.info("User {} left room {}", message.getFrom(), roomId);
        
        message.setType("leave");
        message.setRoomId(roomId);
        
        // Notify all users in the room about the participant leaving
        messagingTemplate.convertAndSend("/topic/signal/" + roomId, message);
    }
}
