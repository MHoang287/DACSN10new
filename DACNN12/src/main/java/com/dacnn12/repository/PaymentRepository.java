package com.dacnn12.repository;

import com.dacnn12.domain.Payment;
import com.dacnn12.domain.PaymentStatus;
import java.util.List;
import org.springframework.data.jpa.repository.JpaRepository;

public interface PaymentRepository extends JpaRepository<Payment, Integer> {

    List<Payment> findByUser_Id(String userId);

    List<Payment> findByStatus(PaymentStatus status);
}
