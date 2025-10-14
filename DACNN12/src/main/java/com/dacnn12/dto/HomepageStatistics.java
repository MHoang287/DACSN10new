package com.dacnn12.dto;

public class HomepageStatistics {

    private long totalStudents;
    private long totalCourses;
    private long totalLessons;
    private long totalTeachers;
    private long publishedCourses;
    private long newCoursesThisMonth;

    public long getTotalStudents() {
        return totalStudents;
    }

    public void setTotalStudents(long totalStudents) {
        this.totalStudents = totalStudents;
    }

    public long getTotalCourses() {
        return totalCourses;
    }

    public void setTotalCourses(long totalCourses) {
        this.totalCourses = totalCourses;
    }

    public long getTotalLessons() {
        return totalLessons;
    }

    public void setTotalLessons(long totalLessons) {
        this.totalLessons = totalLessons;
    }

    public long getTotalTeachers() {
        return totalTeachers;
    }

    public void setTotalTeachers(long totalTeachers) {
        this.totalTeachers = totalTeachers;
    }

    public long getPublishedCourses() {
        return publishedCourses;
    }

    public void setPublishedCourses(long publishedCourses) {
        this.publishedCourses = publishedCourses;
    }

    public long getNewCoursesThisMonth() {
        return newCoursesThisMonth;
    }

    public void setNewCoursesThisMonth(long newCoursesThisMonth) {
        this.newCoursesThisMonth = newCoursesThisMonth;
    }
}
