package com.dacnn12.repository;

import com.dacnn12.domain.CourseCategory;
import com.dacnn12.domain.CourseCategoryId;
import java.util.List;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;

public interface CourseCategoryRepository extends JpaRepository<CourseCategory, CourseCategoryId> {

    @EntityGraph(attributePaths = {"course", "course.user", "course.enrollments"})
    List<CourseCategory> findByCategory_CategoryIdAndCourse_TrangThai(int categoryId, String trangThai);
}
