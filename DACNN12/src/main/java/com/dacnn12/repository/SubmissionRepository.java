package com.dacnn12.repository;

import com.dacnn12.domain.Submission;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface SubmissionRepository extends JpaRepository<Submission, Integer> {

    List<Submission> findByAssignment_AssignmentIdAndUser_Id(Integer assignmentId, String userId);
}
