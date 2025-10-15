package com.example.livestreamservice.repository;

import com.example.livestreamservice.model.Teacher;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface TeacherRepository extends JpaRepository<Teacher, Long> {
    Optional<Teacher> findByUsername(String username);
    boolean existsByUsername(String username);
    boolean existsByEmail(String email);
}
