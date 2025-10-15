package com.example.livestreamservice.repository;

import com.example.livestreamservice.model.LivestreamRoom;
import com.example.livestreamservice.model.Teacher;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface LivestreamRoomRepository extends JpaRepository<LivestreamRoom, Long> {
    Optional<LivestreamRoom> findByRoomId(String roomId);
    List<LivestreamRoom> findByTeacherAndStatus(Teacher teacher, String status);
    List<LivestreamRoom> findByStatus(String status);
    boolean existsByRoomId(String roomId);
}
