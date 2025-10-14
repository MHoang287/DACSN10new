package com.dacnn12.repository;

import com.dacnn12.domain.Course;
import java.time.LocalDateTime;
import java.util.Optional;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface CourseRepository extends JpaRepository<Course, Integer> {

    @EntityGraph(attributePaths = {"user", "enrollments"})
    Page<Course> findByTrangThai(String trangThai, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "enrollments"})
    Page<Course> findByTrangThaiOrderByTenKhoaHocAsc(String trangThai, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "enrollments"})
    Page<Course> findByTrangThaiOrderByNgayTaoDesc(String trangThai, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "enrollments"})
    Page<Course> findByTrangThaiAndTenKhoaHocContainingIgnoreCase(String trangThai, String keyword, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "enrollments"})
    Page<Course> findByTrangThaiAndMoTaContainingIgnoreCase(String trangThai, String topic, Pageable pageable);

    @Query("select c from Course c left join c.enrollments e where c.trangThai = :status group by c order by count(e) desc, c.ngayTao desc")
    Page<Course> findPopularCourses(@Param("status") String status, Pageable pageable);

    @EntityGraph(attributePaths = {"user", "lessons", "assignments", "quizzes", "quizzes.questions", "enrollments", "courseCategories.category"})
    Optional<Course> findWithDetailsByCourseId(Integer courseId);

    long countByTrangThai(String trangThai);

    long countByTrangThaiAndTenKhoaHocContainingIgnoreCase(String trangThai, String keyword);

    long countByTrangThaiAndMoTaContainingIgnoreCase(String trangThai, String topic);

    long countByTrangThaiAndNgayTaoBetween(String trangThai, LocalDateTime start, LocalDateTime end);
}
