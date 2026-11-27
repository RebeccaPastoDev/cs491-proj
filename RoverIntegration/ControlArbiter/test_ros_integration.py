"""Integration tests for the arbiter in an isolated ROS environment."""

import os
import json
import time

import rclpy
from rclpy.node import Node
from rclpy.qos import qos_profile_sensor_data

from geometry_msgs.msg import Twist
from nav_msgs.msg import Odometry
from std_msgs.msg import String
from std_srvs.srv import Trigger


class IntegrationTest(Node):

    def __init__(self):
        super().__init__("arbiter_integration_test")

        self.state = None
        self.velocity = None
        self.send_odom = False
        self.command_source = None
        self.command = Twist()

        self.odom_pub = self.create_publisher(
            Odometry, "/odom_raw", qos_profile_sensor_data
        )

        self.manual_pub = self.create_publisher(
            Twist, "/cmd_vel_manual", 1
        )

        self.auto_pub = self.create_publisher(
            Twist, "/cmd_vel_auto", 1
        )

        self.create_subscription(
            String, "/control_state", self.on_state, 10
        )

        self.create_subscription(
            Twist, "/cmd_vel_arbiter_test",
            self.on_velocity, 1
        )

        self.control_clients = {
            name: self.create_client(
                Trigger, "/control/" + name
            )
            for name in (
                "authorize_manual",
                "authorize_auto",
                "disarm",
                "stop",
                "reset_stop"
            )
        }

        # Simulate sensor and controller messages at 20 Hz.
        self.create_timer(0.05, self.publish_inputs)

    def on_state(self, msg):
        self.state = json.loads(msg.data)

    def on_velocity(self, msg):
        self.velocity = (
            msg.linear.x, msg.angular.z
        )

    def publish_inputs(self):
        if self.send_odom:
            msg = Odometry()
            msg.header.stamp = self.get_clock().now().to_msg()
            msg.header.frame_id = "odom_frame"
            msg.child_frame_id = "base_footprint"
            msg.pose.pose.orientation.w = 1.0
            self.odom_pub.publish(msg)

        if self.command_source == "auto":
            self.auto_pub.publish(self.command)

        elif self.command_source == "manual":
            self.manual_pub.publish(self.command)

    def wait_for(self, condition, timeout=5.0):
        deadline = time.monotonic() + timeout

        while time.monotonic() < deadline:
            rclpy.spin_once(self, timeout_sec=0.05)

            if condition():
                return

        raise AssertionError("Timed out waiting for expected result")

    def service(self, name):
        client = self.control_clients[name]

        if not client.wait_for_service(timeout_sec=5.0):
            raise RuntimeError("Service unavailable: " + name)

        future = client.call_async(Trigger.Request())
        self.wait_for(lambda: future.done())

        return future.result().success

    def expect(self, state, linear=0.0, angular=0.0,
               reason=None):
        # Check both reported state and actual test-topic output.
        self.wait_for(
            lambda: self.state is not None
            and self.velocity is not None
            and self.state["state"] == state
            and self.state["physical_execution_enabled"] is False
            and self.state["linear_m_s"] == linear
            and self.state["angular_rad_s"] == angular
            and self.velocity == (linear, angular)
            and (
                reason is None
                or self.state["reason"] == reason
            )
        )

    def set_command(self, source, linear=0.0, angular=0.0):
        self.command = Twist()
        self.command.linear.x = linear
        self.command.angular.z = angular
        self.command_source = source

    def run_tests(self):

        self.expect("idle")
        print("PASS 1: Startup output is zero")

        assert not self.service("authorize_auto")
        assert not self.service("authorize_manual")
        print("PASS 2: Authorization denied without odometry")

        self.send_odom = True
        self.wait_for(
            lambda: self.state["odometry_fresh"]
        )

        assert self.service("authorize_auto")
        self.set_command("auto", 0.05)
        self.expect("auto", 0.05)
        print("PASS 3: Authorized autonomous command")

        self.set_command(None)
        assert self.service("authorize_manual")
        self.expect("manual")
        self.set_command("manual", -0.05)
        self.expect("manual", -0.05)
        print("PASS 4: Manual takeover")

        assert not self.service("authorize_auto")
        self.expect("manual", -0.05)
        print("PASS 5: Auto cannot override manual")

        assert self.service("stop")
        self.expect("stop_latched")
        assert not self.service("authorize_manual")
        assert not self.service("authorize_auto")
        self.expect("stop_latched")
        print("PASS 6: STOP latch blocks movement")

        self.set_command(None)
        assert self.service("reset_stop")
        self.expect("idle")
        print("PASS 7: STOP reset does not resume movement")

        assert self.service("authorize_auto")
        self.set_command("auto", 0.05)
        self.expect("auto", 0.05)

        self.set_command(None)
        self.expect("idle", reason="command_timeout")
        print("PASS 8: Command timeout stops output")

        assert self.service("authorize_auto")
        self.set_command("auto", 0.05)
        self.expect("auto", 0.05)

        self.send_odom = False
        self.expect("idle", reason="stale_odometry")
        print("PASS 9: Stale odometry stops output")

        self.set_command(None)
        self.send_odom = True
        self.wait_for(
            lambda: self.state["odometry_fresh"]
        )

        assert self.service("authorize_manual")
        self.set_command("manual", 0.20)
        self.expect("idle", reason="velocity_limit_exceeded")
        print("PASS 10: Excessive velocity rejected")

        self.set_command(None)
        assert self.service("stop")
        self.expect("stop_latched")
        print("PASS 11: Final output stopped")

        print("\nALL 11 ROS INTEGRATION TESTS PASSED")


def main():

    # Refuse execution outside the isolated test environment.
    if (
        os.environ.get("ROS_DOMAIN_ID") != "77"
        or os.environ.get("ROS_LOCALHOST_ONLY") != "1"
    ):
        raise RuntimeError("Isolated ROS environment required")

    rclpy.init()
    node = IntegrationTest()

    try:
        node.run_tests()

    finally:
        # Stop publishing simulated inputs and request STOP
        # even if an integration test fails.
        node.command_source = None
        node.send_odom = False

        try:
            node.service("stop")
        finally:
            node.destroy_node()
            rclpy.shutdown()


if __name__ == "__main__":
    main()
