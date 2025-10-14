package com.dacnn12.domain;

import java.io.Serializable;
import java.util.Objects;

import jakarta.persistence.Column;
import jakarta.persistence.Embeddable;

@Embeddable
public class FollowId implements Serializable {

    @Column(name = "follower_id")
    private String followerId;

    @Column(name = "followed_teacher_id")
    private String followedTeacherId;

    public String getFollowerId() {
        return followerId;
    }

    public void setFollowerId(String followerId) {
        this.followerId = followerId;
    }

    public String getFollowedTeacherId() {
        return followedTeacherId;
    }

    public void setFollowedTeacherId(String followedTeacherId) {
        this.followedTeacherId = followedTeacherId;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) {
            return true;
        }
        if (!(o instanceof FollowId that)) {
            return false;
        }
        return Objects.equals(followerId, that.followerId)
            && Objects.equals(followedTeacherId, that.followedTeacherId);
    }

    @Override
    public int hashCode() {
        return Objects.hash(followerId, followedTeacherId);
    }
}
