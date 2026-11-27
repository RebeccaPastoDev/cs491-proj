#ifndef MANUAL_DRIVE_CONTROLLER_H
#define MANUAL_DRIVE_CONTROLLER_H

#include <algorithm>
#include <cmath>

namespace LunarRoverUI {

class ManualDriveController {
public:
    enum class State {
        Disarmed,
        Armed,
        StopLatched
    };

    enum class Direction {
        None,
        Forward,
        Reverse,
        RotateLeft,
        RotateRight
    };

    struct Velocity {
        double linear;
        double angular;
    };

    static constexpr double MaxLinear = 0.10;
    static constexpr double MaxAngular = 0.30;

    bool arm(bool interlocksReady) {
        if (!interlocksReady ||
            direction_ != Direction::None)
            return false;

        state_ = State::Armed;
        return true;
    }

    void disarm() {
        direction_ = Direction::None;

        if (state_ != State::StopLatched)
            state_ = State::Disarmed;
    }


    void resetStop() {
        if (state() == State::StopLatched) {
            *this = ManualDriveController{};
        }
    }

    void stop() {
        direction_ = Direction::None;
        state_ = State::StopLatched;
    }

    void press(Direction direction) {
        if (state_ == State::Armed &&
            direction_ == Direction::None)
            direction_ = direction;
    }

    void release() {
        direction_ = Direction::None;
    }

    void setSpeed(double linear, double angular) {
        linear_ = std::isfinite(linear)
            ? std::clamp(linear, 0.0, MaxLinear)
            : 0.0;

        angular_ = std::isfinite(angular)
            ? std::clamp(angular, 0.0, MaxAngular)
            : 0.0;
    }

    Velocity command(bool interlocksReady) {
        if (!interlocksReady) {
            disarm();
            return {0.0, 0.0};
        }

        if (state_ != State::Armed)
            return {0.0, 0.0};

        switch (direction_) {
            case Direction::Forward:
                return {linear_, 0.0};

            case Direction::Reverse:
                return {-linear_, 0.0};

            case Direction::RotateLeft:
                return {0.0, angular_};

            case Direction::RotateRight:
                return {0.0, -angular_};

            default:
                return {0.0, 0.0};
        }
    }

    State state() const {
        return state_;
    }

    Direction direction() const {
        return direction_;
    }

private:
    State state_ = State::Disarmed;
    Direction direction_ = Direction::None;

    double linear_ = 0.05;
    double angular_ = 0.15;
};

}

#endif
