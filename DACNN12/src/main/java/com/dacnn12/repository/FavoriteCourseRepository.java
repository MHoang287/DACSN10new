package com.dacnn12.repository;

import com.dacnn12.domain.FavoriteCourse;
import com.dacnn12.domain.FavoriteCourseId;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface FavoriteCourseRepository extends JpaRepository<FavoriteCourse, FavoriteCourseId> {

    List<FavoriteCourse> findByUser_Id(String userId);
}
