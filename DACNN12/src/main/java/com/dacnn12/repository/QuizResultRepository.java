package com.dacnn12.repository;

import com.dacnn12.domain.QuizResult;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface QuizResultRepository extends JpaRepository<QuizResult, Integer> {

    List<QuizResult> findByUser_Id(String userId);

    List<QuizResult> findByQuiz_QuizId(Integer quizId);
}
