package com.dacnn12.repository;

import com.dacnn12.domain.Question;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface QuestionRepository extends JpaRepository<Question, Integer> {

    List<Question> findByQuiz_QuizId(Integer quizId);
}
