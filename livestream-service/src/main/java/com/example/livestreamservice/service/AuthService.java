package com.example.livestreamservice.service;

import com.example.livestreamservice.dto.AuthResponse;
import com.example.livestreamservice.dto.LoginRequest;
import com.example.livestreamservice.dto.RegisterRequest;
import com.example.livestreamservice.model.Student;
import com.example.livestreamservice.model.Teacher;
import com.example.livestreamservice.repository.StudentRepository;
import com.example.livestreamservice.repository.TeacherRepository;
import com.example.livestreamservice.security.JwtUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Service
public class AuthService {
    
    @Autowired
    private TeacherRepository teacherRepository;
    
    @Autowired
    private StudentRepository studentRepository;
    
    @Autowired
    private PasswordEncoder passwordEncoder;
    
    @Autowired
    private JwtUtils jwtUtils;
    
    public AuthResponse register(RegisterRequest request) {
        // Check if username exists
        if (teacherRepository.existsByUsername(request.getUsername()) || 
            studentRepository.existsByUsername(request.getUsername())) {
            throw new RuntimeException("Username already exists");
        }
        
        // Check if email exists
        if (teacherRepository.existsByEmail(request.getEmail()) || 
            studentRepository.existsByEmail(request.getEmail())) {
            throw new RuntimeException("Email already exists");
        }
        
        String encodedPassword = passwordEncoder.encode(request.getPassword());
        
        if ("TEACHER".equalsIgnoreCase(request.getRole())) {
            Teacher teacher = new Teacher();
            teacher.setUsername(request.getUsername());
            teacher.setPassword(encodedPassword);
            teacher.setEmail(request.getEmail());
            teacher.setFullName(request.getFullName());
            teacher.setRole("TEACHER");
            
            teacher = teacherRepository.save(teacher);
            
            String token = jwtUtils.generateToken(teacher.getUsername(), teacher.getRole(), teacher.getId());
            return new AuthResponse(token, teacher.getId(), teacher.getUsername(), 
                    teacher.getEmail(), teacher.getFullName(), teacher.getRole());
        } else {
            Student student = new Student();
            student.setUsername(request.getUsername());
            student.setPassword(encodedPassword);
            student.setEmail(request.getEmail());
            student.setFullName(request.getFullName());
            student.setRole("STUDENT");
            
            student = studentRepository.save(student);
            
            String token = jwtUtils.generateToken(student.getUsername(), student.getRole(), student.getId());
            return new AuthResponse(token, student.getId(), student.getUsername(), 
                    student.getEmail(), student.getFullName(), student.getRole());
        }
    }
    
    public AuthResponse login(LoginRequest request) {
        // Try to find teacher
        var teacherOpt = teacherRepository.findByUsername(request.getUsername());
        if (teacherOpt.isPresent()) {
            Teacher teacher = teacherOpt.get();
            if (passwordEncoder.matches(request.getPassword(), teacher.getPassword())) {
                String token = jwtUtils.generateToken(teacher.getUsername(), teacher.getRole(), teacher.getId());
                return new AuthResponse(token, teacher.getId(), teacher.getUsername(), 
                        teacher.getEmail(), teacher.getFullName(), teacher.getRole());
            }
        }
        
        // Try to find student
        var studentOpt = studentRepository.findByUsername(request.getUsername());
        if (studentOpt.isPresent()) {
            Student student = studentOpt.get();
            if (passwordEncoder.matches(request.getPassword(), student.getPassword())) {
                String token = jwtUtils.generateToken(student.getUsername(), student.getRole(), student.getId());
                return new AuthResponse(token, student.getId(), student.getUsername(), 
                        student.getEmail(), student.getFullName(), student.getRole());
            }
        }
        
        throw new RuntimeException("Invalid username or password");
    }
}
