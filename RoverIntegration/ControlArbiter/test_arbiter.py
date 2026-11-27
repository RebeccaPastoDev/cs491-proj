"""Unit tests for control arbitration and safety interlocks."""

import unittest

from arbiter_core import ControlArbiter, ControlState, Velocity


class TestControlArbiter(unittest.TestCase):

    def setUp(self):
        # Each test starts with an independent arbiter
        # and a deterministic simulated clock.
        self.now = 0.0
        self.arbiter = ControlArbiter(clock=lambda: self.now)
        self.arbiter.update_odometry()

    def test_startup_stationary(self):
        self.assertEqual(self.arbiter.state, ControlState.IDLE)
        self.assertEqual(self.arbiter.output(), Velocity())

    def test_unauthorized_command(self):
        self.assertFalse(
            self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        )
        self.assertEqual(self.arbiter.output(), Velocity())

    def test_authorized_auto(self):
        self.assertTrue(self.arbiter.authorize_auto())
        self.assertTrue(
            self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        )
        self.assertEqual(self.arbiter.output(), Velocity(0.05, 0))

    def test_authorized_manual(self):
        self.assertTrue(self.arbiter.authorize_manual())
        self.assertTrue(
            self.arbiter.receive_command(ControlState.MANUAL, -0.05, 0)
        )
        self.assertEqual(self.arbiter.output(), Velocity(-0.05, 0))

    def test_manual_takeover(self):
        self.arbiter.authorize_auto()
        self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)

        # Manual takeover must discard the previous auto command.
        self.assertTrue(self.arbiter.authorize_manual())
        self.assertEqual(self.arbiter.output(), Velocity())

        self.assertFalse(
            self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        )
        self.assertEqual(self.arbiter.state, ControlState.MANUAL)

    def test_auto_cannot_override_manual(self):
        self.arbiter.authorize_manual()
        self.assertFalse(self.arbiter.authorize_auto())

    def test_disarm_no_auto_resume(self):
        self.arbiter.authorize_auto()
        self.arbiter.authorize_manual()
        self.arbiter.disarm()

        self.assertEqual(self.arbiter.state, ControlState.IDLE)
        self.assertFalse(
            self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        )
        self.assertEqual(self.arbiter.output(), Velocity())

    def test_stop_latch(self):
        self.arbiter.authorize_auto()
        self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        self.arbiter.stop()

        self.assertEqual(self.arbiter.output(), Velocity())
        self.assertFalse(self.arbiter.authorize_auto())
        self.assertFalse(self.arbiter.authorize_manual())
        self.assertEqual(self.arbiter.state, ControlState.STOP_LATCHED)

    def test_disarm_preserves_stop(self):
        self.arbiter.stop()
        self.arbiter.disarm()
        self.assertEqual(self.arbiter.state, ControlState.STOP_LATCHED)

    def test_stop_reset(self):
        self.arbiter.stop()
        self.assertTrue(self.arbiter.reset_stop())
        self.assertEqual(self.arbiter.state, ControlState.IDLE)
        self.assertEqual(self.arbiter.output(), Velocity())

    def test_command_timeout(self):
        self.arbiter.authorize_auto()
        self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)

        # Advance time beyond the command timeout while
        # keeping odometry fresh to isolate the failure.
        self.now += self.arbiter.COMMAND_TIMEOUT + 0.01
        self.arbiter.update_odometry()

        self.assertEqual(self.arbiter.output(), Velocity())
        self.assertEqual(self.arbiter.state, ControlState.IDLE)
        self.assertEqual(self.arbiter.reason, "command_timeout")

    def test_stale_odometry(self):
        self.arbiter.authorize_auto()
        self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)

        self.now += self.arbiter.ODOM_TIMEOUT + 0.01

        self.assertEqual(self.arbiter.output(), Velocity())
        self.assertEqual(self.arbiter.state, ControlState.IDLE)
        self.assertEqual(self.arbiter.reason, "stale_odometry")

    def test_authorization_without_odometry(self):
        arbiter = ControlArbiter(clock=lambda: self.now)
        self.assertFalse(arbiter.authorize_auto())
        self.assertFalse(arbiter.authorize_manual())

    def test_late_command_cannot_resume(self):
        self.arbiter.authorize_auto()

        self.now += self.arbiter.COMMAND_TIMEOUT + 0.01
        self.arbiter.update_odometry()

        self.assertFalse(
            self.arbiter.receive_command(ControlState.AUTO, 0.05, 0)
        )
        self.assertEqual(self.arbiter.output(), Velocity())

    def test_velocity_limits(self):
        # Test excessive linear and angular velocities
        # independently, including negative directions.
        for linear, angular in [
            (0.11, 0), (-0.11, 0),
            (0, 0.31), (0, -0.31)
        ]:
            with self.subTest(linear=linear, angular=angular):
                self.arbiter.authorize_manual()
                self.assertFalse(
                    self.arbiter.receive_command(
                        ControlState.MANUAL, linear, angular
                    )
                )
                self.assertEqual(self.arbiter.output(), Velocity())
                self.assertEqual(self.arbiter.state, ControlState.IDLE)

    def test_nonfinite_velocity(self):
        for linear, angular in [
            (float("nan"), 0),
            (float("inf"), 0),
            (0, float("-inf"))
        ]:
            with self.subTest(linear=linear, angular=angular):
                self.arbiter.authorize_manual()
                self.assertFalse(
                    self.arbiter.receive_command(
                        ControlState.MANUAL, linear, angular
                    )
                )
                self.assertEqual(self.arbiter.output(), Velocity())
                self.assertEqual(self.arbiter.state, ControlState.IDLE)


if __name__ == "__main__":
    unittest.main(verbosity=2)
