package com.dacnn12.service;

import com.dacnn12.domain.Course;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public interface CourseService {

    Page<Course> getActiveCourses(Pageable pageable);

    Page<Course> getPopularCourses(Pageable pageable);

    Page<Course> getNewCourses(Pageable pageable);

    Page<Course> searchByName(String keyword, Pageable pageable);

    Page<Course> searchByTopic(String topic, Pageable pageable);

    Page<Course> searchByCategory(int categoryId, Pageable pageable);

    Course getCourseDetails(int courseId);
}
