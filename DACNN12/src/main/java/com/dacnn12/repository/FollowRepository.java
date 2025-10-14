package com.dacnn12.repository;

import com.dacnn12.domain.Follow;
import com.dacnn12.domain.FollowId;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface FollowRepository extends JpaRepository<Follow, FollowId> {

    List<Follow> findByFollower_Id(String followerId);

    List<Follow> findByFollowedTeacher_Id(String teacherId);
}
