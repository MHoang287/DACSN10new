package com.dacnn12.repository;

import com.dacnn12.domain.Lesson;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface LessonRepository extends JpaRepository<Lesson, Integer> {

    List<Lesson> findByCourse_CourseIdOrderByCreatedAtAsc(Integer courseId);
}
