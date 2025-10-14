package com.dacnn12.repository;

import com.dacnn12.domain.UserLessonProgress;
import java.util.Optional;
import org.springframework.data.jpa.repository.JpaRepository;

public interface UserLessonProgressRepository extends JpaRepository<UserLessonProgress, Integer> {

    Optional<UserLessonProgress> findByUser_IdAndLesson_LessonId(String userId, Integer lessonId);
}
