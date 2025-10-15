package com.example.livestreamservice.controller;

import com.example.livestreamservice.dto.CreateRoomRequest;
import com.example.livestreamservice.dto.RoomResponse;
import com.example.livestreamservice.security.JwtUtils;
import com.example.livestreamservice.service.LivestreamRoomService;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.Authentication;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/rooms")
@CrossOrigin(origins = "*", maxAge = 3600)
public class LivestreamRoomController {
    
    @Autowired
    private LivestreamRoomService roomService;
    
    @Autowired
    private JwtUtils jwtUtils;
    
    @PostMapping("/create")
    public ResponseEntity<?> createRoom(@Valid @RequestBody CreateRoomRequest request,
                                        Authentication authentication) {
        try {
            String username = authentication.getName();
            RoomResponse response = roomService.createRoom(request, username);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @GetMapping("/active")
    public ResponseEntity<?> getActiveRooms() {
        try {
            List<RoomResponse> rooms = roomService.getActiveRooms();
            return ResponseEntity.ok(rooms);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @GetMapping("/my-rooms")
    public ResponseEntity<?> getMyRooms(Authentication authentication) {
        try {
            String username = authentication.getName();
            List<RoomResponse> rooms = roomService.getTeacherRooms(username);
            return ResponseEntity.ok(rooms);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @GetMapping("/{roomId}")
    public ResponseEntity<?> getRoomById(@PathVariable String roomId) {
        try {
            RoomResponse response = roomService.getRoomById(roomId);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @PostMapping("/{roomId}/join")
    public ResponseEntity<?> joinRoom(@PathVariable String roomId,
                                      @RequestHeader("Authorization") String token) {
        try {
            String jwt = token.substring(7); // Remove "Bearer " prefix
            Long userId = jwtUtils.getUserIdFromToken(jwt);
            RoomResponse response = roomService.joinRoom(roomId, userId);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @PostMapping("/{roomId}/leave")
    public ResponseEntity<?> leaveRoom(@PathVariable String roomId,
                                       @RequestHeader("Authorization") String token) {
        try {
            String jwt = token.substring(7);
            Long userId = jwtUtils.getUserIdFromToken(jwt);
            roomService.leaveRoom(roomId, userId);
            return ResponseEntity.ok("Left room successfully");
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
    
    @PostMapping("/{roomId}/end")
    public ResponseEntity<?> endRoom(@PathVariable String roomId,
                                     Authentication authentication) {
        try {
            String username = authentication.getName();
            RoomResponse response = roomService.endRoom(roomId, username);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
}
