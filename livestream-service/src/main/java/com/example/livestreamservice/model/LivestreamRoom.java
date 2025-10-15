package com.example.livestreamservice.model;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.HashSet;
import java.util.Set;

@Entity
@Table(name = "livestream_rooms")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class LivestreamRoom {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false, unique = true)
    private String roomId;
    
    @Column(nullable = false)
    private String roomName;
    
    @Column(length = 1000)
    private String description;
    
    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "teacher_id", nullable = false)
    private Teacher teacher;
    
    @Column(nullable = false)
    private String status = "ACTIVE"; // ACTIVE, ENDED
    
    @Column(name = "created_at")
    private LocalDateTime createdAt;
    
    @Column(name = "ended_at")
    private LocalDateTime endedAt;
    
    @ElementCollection
    @CollectionTable(name = "room_participants", joinColumns = @JoinColumn(name = "room_id"))
    @Column(name = "student_id")
    private Set<Long> participantIds = new HashSet<>();
    
    @Column(name = "max_participants")
    private Integer maxParticipants = 100;
    
    @PrePersist
    protected void onCreate() {
        createdAt = LocalDateTime.now();
    }
}
