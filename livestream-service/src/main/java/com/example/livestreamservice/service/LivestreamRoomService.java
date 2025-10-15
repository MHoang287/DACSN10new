package com.example.livestreamservice.service;

import com.example.livestreamservice.dto.CreateRoomRequest;
import com.example.livestreamservice.dto.RoomResponse;
import com.example.livestreamservice.model.LivestreamRoom;
import com.example.livestreamservice.model.Teacher;
import com.example.livestreamservice.repository.LivestreamRoomRepository;
import com.example.livestreamservice.repository.TeacherRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;
import java.util.stream.Collectors;

@Service
public class LivestreamRoomService {
    
    @Autowired
    private LivestreamRoomRepository roomRepository;
    
    @Autowired
    private TeacherRepository teacherRepository;
    
    public RoomResponse createRoom(CreateRoomRequest request, String username) {
        Teacher teacher = teacherRepository.findByUsername(username)
                .orElseThrow(() -> new RuntimeException("Teacher not found"));
        
        // Generate unique room ID
        String roomId = UUID.randomUUID().toString();
        
        LivestreamRoom room = new LivestreamRoom();
        room.setRoomId(roomId);
        room.setRoomName(request.getRoomName());
        room.setDescription(request.getDescription());
        room.setTeacher(teacher);
        room.setMaxParticipants(request.getMaxParticipants());
        room.setStatus("ACTIVE");
        
        room = roomRepository.save(room);
        
        return toRoomResponse(room);
    }
    
    public RoomResponse joinRoom(String roomId, Long studentId) {
        LivestreamRoom room = roomRepository.findByRoomId(roomId)
                .orElseThrow(() -> new RuntimeException("Room not found"));
        
        if (!"ACTIVE".equals(room.getStatus())) {
            throw new RuntimeException("Room is not active");
        }
        
        if (room.getParticipantIds().size() >= room.getMaxParticipants()) {
            throw new RuntimeException("Room is full");
        }
        
        room.getParticipantIds().add(studentId);
        room = roomRepository.save(room);
        
        return toRoomResponse(room);
    }
    
    public void leaveRoom(String roomId, Long studentId) {
        LivestreamRoom room = roomRepository.findByRoomId(roomId)
                .orElseThrow(() -> new RuntimeException("Room not found"));
        
        room.getParticipantIds().remove(studentId);
        roomRepository.save(room);
    }
    
    public RoomResponse endRoom(String roomId, String username) {
        LivestreamRoom room = roomRepository.findByRoomId(roomId)
                .orElseThrow(() -> new RuntimeException("Room not found"));
        
        Teacher teacher = teacherRepository.findByUsername(username)
                .orElseThrow(() -> new RuntimeException("Teacher not found"));
        
        if (!room.getTeacher().getId().equals(teacher.getId())) {
            throw new RuntimeException("You are not authorized to end this room");
        }
        
        room.setStatus("ENDED");
        room.setEndedAt(LocalDateTime.now());
        room = roomRepository.save(room);
        
        return toRoomResponse(room);
    }
    
    public RoomResponse getRoomById(String roomId) {
        LivestreamRoom room = roomRepository.findByRoomId(roomId)
                .orElseThrow(() -> new RuntimeException("Room not found"));
        return toRoomResponse(room);
    }
    
    public List<RoomResponse> getActiveRooms() {
        return roomRepository.findByStatus("ACTIVE")
                .stream()
                .map(this::toRoomResponse)
                .collect(Collectors.toList());
    }
    
    public List<RoomResponse> getTeacherRooms(String username) {
        Teacher teacher = teacherRepository.findByUsername(username)
                .orElseThrow(() -> new RuntimeException("Teacher not found"));
        
        return roomRepository.findByTeacherAndStatus(teacher, "ACTIVE")
                .stream()
                .map(this::toRoomResponse)
                .collect(Collectors.toList());
    }
    
    private RoomResponse toRoomResponse(LivestreamRoom room) {
        RoomResponse response = new RoomResponse();
        response.setId(room.getId());
        response.setRoomId(room.getRoomId());
        response.setRoomName(room.getRoomName());
        response.setDescription(room.getDescription());
        response.setTeacherName(room.getTeacher().getFullName());
        response.setTeacherId(room.getTeacher().getId());
        response.setStatus(room.getStatus());
        response.setCreatedAt(room.getCreatedAt());
        response.setParticipantCount(room.getParticipantIds().size());
        response.setMaxParticipants(room.getMaxParticipants());
        return response;
    }
}
