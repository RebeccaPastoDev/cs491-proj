"""Independent control arbitration and movement authorization."""

import math
import time

from enum import Enum
from dataclasses import dataclass


class ControlState(Enum):
    IDLE = "idle"
    MANUAL = "manual"
    AUTO = "auto"
    STOP_LATCHED = "stop_latched"


@dataclass(frozen=True)
class Velocity:
    linear: float = 0.0
    angular: float = 0.0


class ControlArbiter:

    # Provisional development limits, not hardware-validated.
    MAX_LINEAR = 0.10       # m/s
    MAX_ANGULAR = 0.30      # rad/s
    COMMAND_TIMEOUT = 0.5   # seconds
    ODOM_TIMEOUT = 0.5      # seconds

    def __init__(self, clock=time.monotonic):
        self.clock = clock
        self.state = ControlState.IDLE
        self.reason = "startup"
        self.last_odom = None
        self.last_command = None
        self.velocity = Velocity()

    def update_odometry(self):
        self.last_odom = self.clock()

    def odometry_fresh(self):
        return (
            self.last_odom is not None
            and self.clock() - self.last_odom <= self.ODOM_TIMEOUT
        )

    def clear_command(self):
        self.velocity = Velocity()
        self.last_command = None

    def stop(self):
        """Latch STOP until explicitly reset."""
        self.clear_command()
        self.state = ControlState.STOP_LATCHED
        self.reason = "stop_requested"

    def reset_stop(self):
        # Resetting STOP does not authorize movement.
        if self.state != ControlState.STOP_LATCHED:
            return False

        self.clear_command()
        self.state = ControlState.IDLE
        self.reason = "stop_reset"
        return True

    def disarm(self, reason="disarmed"):
        self.clear_command()

        if self.state != ControlState.STOP_LATCHED:
            self.state = ControlState.IDLE
            self.reason = reason

    def authorize_manual(self):
        if self.state == ControlState.STOP_LATCHED:
            return False

        if not self.odometry_fresh():
            self.disarm("stale_odometry")
            return False

        # Manual takeover revokes autonomous authorization
        # and discards previously received commands.
        self.clear_command()
        self.state = ControlState.MANUAL
        self.reason = "manual_authorized"
        self.last_command = self.clock()
        return True

    def authorize_auto(self):
        if self.state != ControlState.IDLE:
            return False

        if not self.odometry_fresh():
            return False

        self.clear_command()
        self.state = ControlState.AUTO
        self.reason = "auto_authorized"
        self.last_command = self.clock()
        return True

    def receive_command(self, source, linear, angular):
        """Accept commands only from the authorized controller."""

        if source not in (ControlState.MANUAL, ControlState.AUTO):
            return False

        if self.state != source:
            return False

        # Check for expired authorization before accepting
        # another command. Late commands cannot resume motion.
        self.output()

        if self.state != source:
            return False

        if not math.isfinite(linear) or not math.isfinite(angular):
            self.disarm("invalid_velocity")
            return False

        if (
            abs(linear) > self.MAX_LINEAR
            or abs(angular) > self.MAX_ANGULAR
        ):
            self.disarm("velocity_limit_exceeded")
            return False

        self.velocity = Velocity(linear, angular)
        self.last_command = self.clock()
        return True

    def output(self):
        """Evaluate safety interlocks before returning velocity."""

        if self.state not in (
            ControlState.MANUAL,
            ControlState.AUTO
        ):
            return Velocity()

        if not self.odometry_fresh():
            self.disarm("stale_odometry")
            return Velocity()

        if (
            self.last_command is None
            or self.clock() - self.last_command
            > self.COMMAND_TIMEOUT
        ):
            self.disarm("command_timeout")
            return Velocity()

        return self.velocity
