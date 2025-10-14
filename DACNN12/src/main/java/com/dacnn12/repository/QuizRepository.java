package com.dacnn12.repository;

import com.dacnn12.domain.Quiz;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface QuizRepository extends JpaRepository<Quiz, Integer> {

    List<Quiz> findByCourse_CourseId(Integer courseId);
}
