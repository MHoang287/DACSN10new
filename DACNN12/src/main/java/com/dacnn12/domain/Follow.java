package com.dacnn12.domain;

import jakarta.persistence.EmbeddedId;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.MapsId;
import jakarta.persistence.Table;

@Entity
@Table(name = "follows")
public class Follow {

    @EmbeddedId
    private FollowId id = new FollowId();

    @ManyToOne(fetch = FetchType.LAZY)
    @MapsId("followerId")
    @JoinColumn(name = "follower_id")
    private User follower;

    @ManyToOne(fetch = FetchType.LAZY)
    @MapsId("followedTeacherId")
    @JoinColumn(name = "followed_teacher_id")
    private User followedTeacher;

    public FollowId getId() {
        return id;
    }

    public void setId(FollowId id) {
        this.id = id;
    }

    public User getFollower() {
        return follower;
    }

    public void setFollower(User follower) {
        this.follower = follower;
    }

    public User getFollowedTeacher() {
        return followedTeacher;
    }

    public void setFollowedTeacher(User followedTeacher) {
        this.followedTeacher = followedTeacher;
    }
}
