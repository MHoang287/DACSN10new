package com.dacnn12.repository;

import com.dacnn12.domain.CourseFollow;
import com.dacnn12.domain.CourseFollowId;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface CourseFollowRepository extends JpaRepository<CourseFollow, CourseFollowId> {

    List<CourseFollow> findByUser_Id(String userId);

    List<CourseFollow> findByCourse_CourseId(Integer courseId);
}
