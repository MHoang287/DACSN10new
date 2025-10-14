package com.dacnn12.service.impl;

import com.dacnn12.domain.RoleNames;
import com.dacnn12.dto.HomepageStatistics;
import com.dacnn12.repository.CourseRepository;
import com.dacnn12.repository.EnrollmentRepository;
import com.dacnn12.repository.LessonRepository;
import com.dacnn12.repository.UserRepository;
import com.dacnn12.service.StatisticsService;
import java.time.LocalDateTime;
import java.time.temporal.TemporalAdjusters;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional(readOnly = true)
public class StatisticsServiceImpl implements StatisticsService {

    private static final String ACTIVE_STATUS = "Active";

    private final CourseRepository courseRepository;
    private final LessonRepository lessonRepository;
    private final EnrollmentRepository enrollmentRepository;
    private final UserRepository userRepository;

    public StatisticsServiceImpl(CourseRepository courseRepository,
                                 LessonRepository lessonRepository,
                                 EnrollmentRepository enrollmentRepository,
                                 UserRepository userRepository) {
        this.courseRepository = courseRepository;
        this.lessonRepository = lessonRepository;
        this.enrollmentRepository = enrollmentRepository;
        this.userRepository = userRepository;
    }

    @Override
    public HomepageStatistics collectHomepageStatistics() {
        HomepageStatistics statistics = new HomepageStatistics();
        statistics.setTotalCourses(courseRepository.count());
        statistics.setPublishedCourses(courseRepository.countByTrangThai(ACTIVE_STATUS));
        statistics.setTotalLessons(lessonRepository.count());
        statistics.setTotalStudents(enrollmentRepository.count());
        statistics.setTotalTeachers(userRepository.findAll().stream()
            .filter(user -> RoleNames.TEACHER.equalsIgnoreCase(user.getLoaiNguoiDung()))
            .count());

        LocalDateTime now = LocalDateTime.now();
        LocalDateTime startOfMonth = now.with(TemporalAdjusters.firstDayOfMonth()).withHour(0).withMinute(0).withSecond(0).withNano(0);
        statistics.setNewCoursesThisMonth(
            courseRepository.countByTrangThaiAndNgayTaoBetween(ACTIVE_STATUS, startOfMonth, now));
        return statistics;
    }
}
