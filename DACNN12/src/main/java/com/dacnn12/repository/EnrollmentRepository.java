package com.dacnn12.repository;

import com.dacnn12.domain.Enrollment;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface EnrollmentRepository extends JpaRepository<Enrollment, Integer> {

    List<Enrollment> findByUser_Id(String userId);

    long countByCourse_CourseId(Integer courseId);
}
