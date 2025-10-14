package com.dacnn12.repository;

import com.dacnn12.domain.Assignment;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface AssignmentRepository extends JpaRepository<Assignment, Integer> {

    List<Assignment> findByCourse_CourseIdOrderByDeadlineAsc(Integer courseId);
}
