package com.dacnn12.service.impl;

import com.dacnn12.domain.Course;
import com.dacnn12.repository.CourseCategoryRepository;
import com.dacnn12.repository.CourseRepository;
import com.dacnn12.service.CourseService;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.stream.Collectors;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional(readOnly = true)
public class CourseServiceImpl implements CourseService {

    private static final String ACTIVE_STATUS = "Active";

    private final CourseRepository courseRepository;
    private final CourseCategoryRepository courseCategoryRepository;

    public CourseServiceImpl(CourseRepository courseRepository, CourseCategoryRepository courseCategoryRepository) {
        this.courseRepository = courseRepository;
        this.courseCategoryRepository = courseCategoryRepository;
    }

    @Override
    public Page<Course> getActiveCourses(Pageable pageable) {
        return courseRepository.findByTrangThaiOrderByTenKhoaHocAsc(ACTIVE_STATUS, pageable);
    }

    @Override
    public Page<Course> getPopularCourses(Pageable pageable) {
        return courseRepository.findPopularCourses(ACTIVE_STATUS, pageable);
    }

    @Override
    public Page<Course> getNewCourses(Pageable pageable) {
        return courseRepository.findByTrangThaiOrderByNgayTaoDesc(ACTIVE_STATUS, pageable);
    }

    @Override
    public Page<Course> searchByName(String keyword, Pageable pageable) {
        return courseRepository.findByTrangThaiAndTenKhoaHocContainingIgnoreCase(ACTIVE_STATUS, keyword, pageable);
    }

    @Override
    public Page<Course> searchByTopic(String topic, Pageable pageable) {
        return courseRepository.findByTrangThaiAndMoTaContainingIgnoreCase(ACTIVE_STATUS, topic, pageable);
    }

    @Override
    public Page<Course> searchByCategory(int categoryId, Pageable pageable) {
        List<Course> courses = courseCategoryRepository
            .findByCategory_CategoryIdAndCourse_TrangThai(categoryId, ACTIVE_STATUS)
            .stream()
            .map(courseCategory -> courseCategory.getCourse())
            .collect(Collectors.collectingAndThen(
                Collectors.toMap(Course::getCourseId, course -> course, (first, second) -> first, java.util.LinkedHashMap::new),
                map -> List.copyOf(map.values())));

        int start = (int) pageable.getOffset();
        int end = Math.min((start + pageable.getPageSize()), courses.size());
        List<Course> content = start > end ? List.of() : courses.subList(start, end);
        return new PageImpl<>(content, pageable, courses.size());
    }

    @Override
    public Course getCourseDetails(int courseId) {
        return courseRepository.findWithDetailsByCourseId(courseId).orElse(null);
    }
}
