package com.dacnn12.web;

import com.dacnn12.dto.HomepageStatistics;
import com.dacnn12.service.CourseService;
import com.dacnn12.service.StatisticsService;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;

@Controller
@RequestMapping("/")
public class HomeController {

    private final CourseService courseService;
    private final StatisticsService statisticsService;

    public HomeController(CourseService courseService, StatisticsService statisticsService) {
        this.courseService = courseService;
        this.statisticsService = statisticsService;
    }

    @GetMapping
    public String index(Model model) {
        HomepageStatistics statistics = statisticsService.collectHomepageStatistics();
        model.addAttribute("statistics", statistics);
        model.addAttribute("featuredCourses", courseService.getPopularCourses(PageRequest.of(0, 6)).getContent());
        model.addAttribute("newCourses", courseService.getNewCourses(PageRequest.of(0, 6)).getContent());
        return "home/index";
    }
}
