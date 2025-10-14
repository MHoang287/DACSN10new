package com.dacnn12.web;

import com.dacnn12.domain.Course;
import com.dacnn12.service.CourseService;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
@RequestMapping("/courses")
public class CourseController {

    private final CourseService courseService;

    public CourseController(CourseService courseService) {
        this.courseService = courseService;
    }

    @GetMapping
    public String index(@RequestParam(defaultValue = "0") int page,
                        @RequestParam(defaultValue = "12") int size,
                        Model model) {
        Page<Course> result = courseService.getActiveCourses(PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Danh sách khóa học");
        return "courses/index";
    }

    @GetMapping("/popular")
    public String popular(@RequestParam(defaultValue = "0") int page,
                          @RequestParam(defaultValue = "12") int size,
                          Model model) {
        Page<Course> result = courseService.getPopularCourses(PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Khóa học nổi bật");
        return "courses/index";
    }

    @GetMapping("/new")
    public String newest(@RequestParam(defaultValue = "0") int page,
                         @RequestParam(defaultValue = "12") int size,
                         Model model) {
        Page<Course> result = courseService.getNewCourses(PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Khóa học mới nhất");
        return "courses/index";
    }

    @GetMapping("/search/name")
    public String searchByName(@RequestParam String keyword,
                               @RequestParam(defaultValue = "0") int page,
                               @RequestParam(defaultValue = "12") int size,
                               Model model) {
        Page<Course> result = courseService.searchByName(keyword, PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Kết quả tìm kiếm theo tên");
        model.addAttribute("keyword", keyword);
        model.addAttribute("searchType", "name");
        return "courses/search";
    }

    @GetMapping("/search/topic")
    public String searchByTopic(@RequestParam String topic,
                                @RequestParam(defaultValue = "0") int page,
                                @RequestParam(defaultValue = "12") int size,
                                Model model) {
        Page<Course> result = courseService.searchByTopic(topic, PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Kết quả tìm kiếm theo chủ đề");
        model.addAttribute("keyword", topic);
        model.addAttribute("searchType", "topic");
        return "courses/search";
    }

    @GetMapping("/search/category")
    public String searchByCategory(@RequestParam("id") int categoryId,
                                   @RequestParam(defaultValue = "0") int page,
                                   @RequestParam(defaultValue = "12") int size,
                                   Model model) {
        Page<Course> result = courseService.searchByCategory(categoryId, PageRequest.of(page, size));
        model.addAttribute("courses", result);
        model.addAttribute("pageTitle", "Khóa học theo danh mục");
        model.addAttribute("categoryId", categoryId);
        model.addAttribute("searchType", "category");
        return "courses/search";
    }

    @GetMapping("/{id}")
    public String details(@PathVariable int id, Model model) {
        Course course = courseService.getCourseDetails(id);
        if (course == null) {
            return "errors/404";
        }
        model.addAttribute("course", course);
        model.addAttribute("lessons", course.getLessons());
        model.addAttribute("assignments", course.getAssignments());
        model.addAttribute("quizzes", course.getQuizzes());
        model.addAttribute("enrollments", course.getEnrollments());
        model.addAttribute("categories", course.getCourseCategories());
        return "courses/details";
    }
}
