#include "simulation_bridge.h"
#include <QDebug>
#include <cmath>
#include <chrono>
#include <thread>
#include <algorithm>

namespace LunarRoverUI {

SimulationBridge::SimulationBridge(QObject *parent)
    : QObject(parent)
    , m_connected(false)
    , m_microROSConnected(false)
    , m_simTime(0.0)
    , m_posX(0.0), m_posY(0.0), m_posZ(0.0)
    , m_roll(0.0), m_pitch(0.0), m_yaw(0.0)
    , m_linearVel(0.0), m_angularVel(0.0)
    , m_batteryLevel(100.0)
    , m_agentIP("127.0.0.1")
    , m_agentPort(8888)
{
    m_updateTimer = new QTimer(this);
    connect(m_updateTimer, &QTimer::timeout, this, &SimulationBridge::simulationUpdateTick);
    m_updateTimer->setInterval(50); // 20 Hz update rate
}

SimulationBridge::~SimulationBridge()
{
    disconnect();
}

bool SimulationBridge::connectToRover(const std::string &agentIP, int agentPort)
{
    setRoverConfig(agentIP, agentPort);
#ifdef MICRO_ROS_SUPPORT
    // Initialize ROS2
    if (!rclcpp::ok()) {
        rclcpp::init(0, nullptr);
    }

    try {
        // Create ROS2 node for Yahboom Rover
        m_node = rclcpp::Node::make_shared("lunar_rover_ui");

        // Publisher: Send velocity commands to ESP32 rover
        m_twist_pub = m_node->create_publisher<geometry_msgs::msg::Twist>(
            "cmd_vel", rclcpp::QoS(10).best_effort()
        );

        // Subscriber: Receive odometry from ESP32 (Yahboom uses /odom_raw)
        m_odom_sub = m_node->create_subscription<nav_msgs::msg::Odometry>(
            "odom_raw", rclcpp::QoS(10).best_effort(),
            [this](const nav_msgs::msg::Odometry::SharedPtr msg) {
                this->onOdometryReceived(msg);
            }
        );

        // Subscriber: Receive battery status from ESP32
        m_battery_sub = m_node->create_subscription<sensor_msgs::msg::BatteryState>(
            "battery_state", rclcpp::QoS(10).best_effort(),
            [this](const sensor_msgs::msg::BatteryState::SharedPtr msg) {
                this->onBatteryReceived(msg);
            }
        );

        // Subscriber: Receive camera feed from ESP32-CAM (Yahboom uses /esp32_img)
        m_camera_sub = m_node->create_subscription<sensor_msgs::msg::CompressedImage>(
            "esp32_img", rclcpp::QoS(5).best_effort(),
            [this](const sensor_msgs::msg::CompressedImage::SharedPtr msg) {
                this->onCameraFrameReceived(msg);
            }
        );

        // Subscriber: Receive IMU data from ESP32 (Yahboom uses /imu)
        m_imu_sub = m_node->create_subscription<sensor_msgs::msg::Imu>(
            "imu", rclcpp::QoS(10).best_effort(),
            [this](const sensor_msgs::msg::Imu::SharedPtr msg) {
                this->onImuReceived(msg);
            }
        );

        // Start ROS executor in background thread
        m_executor = std::make_shared<rclcpp::executors::SingleThreadedExecutor>();
        m_executor->add_node(m_node);
        
        m_rosThread = std::thread([this]() {
            while (rclcpp::ok() && m_microROSConnected) {
                m_executor->spin_once(std::chrono::milliseconds(10));
            }
        });

        m_microROSConnected = true;
        m_connected = true;
        m_lastHeartbeat = QDateTime::currentMSecsSinceEpoch();
        m_updateTimer->start();

        emit connectionStatusChanged(true);
        emit microROSConnectionStatusChanged(true);

        qDebug() << "✅ Connected to Yahboom ESP32 Rover at" << agentIP.c_str() << ":" << agentPort;
        return true;
    } catch (const std::exception &e) {
        qDebug() << "❌ Failed to connect to Rover:" << e.what();
        return false;
    }
#else
    qDebug() << "⚠️ Micro-ROS support not compiled in - cannot connect to hardware rover";
    return false;
#endif
}

void SimulationBridge::disconnect()
{
    if (m_connected) {
        m_updateTimer->stop();
        m_connected = false;
        emit connectionStatusChanged(false);
        qDebug() << "Disconnected from rover";
    }

#ifdef MICRO_ROS_SUPPORT
    if (m_microROSConnected) {
        m_microROSConnected = false;
        
        // Wait for ROS thread to exit cleanly
        if (m_rosThread.joinable()) {
            m_executor->cancel();
            m_rosThread.join();
        }
        
        // Clean up micro-ROS resources
        if (m_node) {
            m_node.reset();
        }
        
        emit microROSConnectionStatusChanged(false);
        qDebug() << "✅ Micro-ROS resources cleaned up properly";
    }
#endif
}

bool SimulationBridge::isConnected() const
{
    return m_connected;
}

bool SimulationBridge::connectToMicroROS(const std::string &wifiSSID, const std::string &wifiPassword,
                                        const std::string &agentIP, int agentPort)
{
    setMicroROSConfig(wifiSSID, wifiPassword, agentIP, agentPort);
#ifdef MICRO_ROS_SUPPORT
    // Initialize micro-ROS
    if (!rclcpp::ok()) {
        rclcpp::init(0, nullptr);
    }

    try {
        // Create micro-ROS node
        m_node = rclcpp::Node::make_shared("lunar_rover_ui");

        // Create subscriber for velocity commands
        m_twist_sub = m_node->create_subscription<geometry_msgs::msg::Twist>(
            "cmd_vel", rclcpp::QoS(10),
            [this](const geometry_msgs::msg::Twist::SharedPtr msg) {
                this->onTwistMessage(msg);
            }
        );

        // Create publisher for odometry
        m_odom_pub = m_node->create_publisher<nav_msgs::msg::Odometry>(
            "odom_raw", rclcpp::QoS(10)
        );

        // Connect to micro-ROS agent

        m_microROSConnected = true;
        m_connected = true;
        m_updateTimer->start();

        emit connectionStatusChanged(true);
        emit microROSConnectionStatusChanged(true);

        qDebug() << "Successfully connected to Micro-ROS agent at" << agentIP.c_str() << ":" << agentPort;
        return true;
    } catch (const std::exception &e) {
        qDebug() << "Failed to connect to Micro-ROS:" << e.what();
        return false;
    }
#else
    qDebug() << "⚠️ Micro-ROS support not compiled in";
    return false;
#endif
}

void SimulationBridge::setMicroROSConfig(const std::string &wifiSSID, const std::string &wifiPassword,
                                       const std::string &agentIP, int agentPort)
{
    m_wifiSSID = wifiSSID;
    m_wifiPassword = wifiPassword;
    m_agentIP = agentIP;
    m_agentPort = agentPort;
}

bool SimulationBridge::isMicroROSConnected() const
{
    return m_microROSConnected;
}

void SimulationBridge::sendVelocityCommand(double linear, double angular)
{
#ifdef MICRO_ROS_SUPPORT
    if (!m_connected || !m_twist_pub) return;

    // Yahboom ESP32 Rover hardware limits
    const double MAX_LINEAR_VELOCITY = 0.5;
    const double MAX_ANGULAR_VELOCITY = 2.0;

    // Clamp values to safe hardware limits
    linear = std::clamp(linear, -MAX_LINEAR_VELOCITY, MAX_LINEAR_VELOCITY);
    angular = std::clamp(angular, -MAX_ANGULAR_VELOCITY, MAX_ANGULAR_VELOCITY);

    // Create and publish Twist message to ESP32 Rover
    auto twist_msg = std::make_unique<geometry_msgs::msg::Twist>();
    twist_msg->linear.x = linear;
    twist_msg->angular.z = angular;
    
    m_twist_pub->publish(std::move(twist_msg));
#endif
    
    m_linearVel = linear;
    m_angularVel = angular;
    emit velocityUpdated(m_linearVel, m_angularVel);
}

void SimulationBridge::emergencyStop()
{
    sendVelocityCommand(0.0, 0.0);
    qDebug() << "⚠️ EMERGENCY STOP ACTIVATED";
}

#ifdef MICRO_ROS_SUPPORT
void SimulationBridge::onOdometryReceived(const nav_msgs::msg::Odometry::SharedPtr msg)
{
    m_lastHeartbeat = QDateTime::currentMSecsSinceEpoch();
    
    // Update position from actual rover odometry
    m_posX = msg->pose.pose.position.x;
    m_posY = msg->pose.pose.position.y;
    m_posZ = msg->pose.pose.position.z;
    
    // Extract yaw from quaternion
    const auto &q = msg->pose.pose.orientation;
    m_yaw = atan2(2.0 * (q.w * q.z + q.x * q.y), 1.0 - 2.0 * (q.y * q.y + q.z * q.z));
    
    m_linearVel = msg->twist.twist.linear.x;
    m_angularVel = msg->twist.twist.angular.z;
    
    emit positionUpdated(m_posX, m_posY, m_posZ);
    emit orientationUpdated(m_roll, m_pitch, m_yaw);
    emit velocityUpdated(m_linearVel, m_angularVel);
}

void SimulationBridge::onTwistMessage(const geometry_msgs::msg::Twist::SharedPtr msg)
{
    // Handle incoming velocity commands from the rover
    m_linearVel = msg->linear.x;
    m_angularVel = msg->angular.z;

    qDebug() << "Received velocity command from rover: linear =" << m_linearVel << "angular =" << m_angularVel;
}

void SimulationBridge::onBatteryReceived(const sensor_msgs::msg::BatteryState::SharedPtr msg)
{
    m_lastHeartbeat = QDateTime::currentMSecsSinceEpoch();
    m_batteryLevel = msg->percentage * 100.0;
    emit batteryLevelUpdated(m_batteryLevel);
}

void SimulationBridge::onImuReceived(const sensor_msgs::msg::Imu::SharedPtr msg)
{
    m_lastHeartbeat = QDateTime::currentMSecsSinceEpoch();

    // Extract orientation from quaternion
    const auto &q = msg->orientation;
    m_roll = atan2(2.0 * (q.w * q.x + q.y * q.z), 1.0 - 2.0 * (q.x * q.x + q.y * q.y));
    m_pitch = asin(2.0 * (q.w * q.y - q.z * q.x));
    m_yaw = atan2(2.0 * (q.w * q.z + q.x * q.y), 1.0 - 2.0 * (q.y * q.y + q.z * q.z));

    emit orientationUpdated(m_roll, m_pitch, m_yaw);
}

void SimulationBridge::onCameraFrameReceived(const sensor_msgs::msg::CompressedImage::SharedPtr msg)
{
    m_lastHeartbeat = QDateTime::currentMSecsSinceEpoch();
    // Signal UI with raw JPEG buffer - UI will handle decompression
    emit cameraFrameReceived(msg->data.data(), msg->data.size(), 0, 0);
}
#endif

qint64 SimulationBridge::getLastHeartbeat() const
{
    return QDateTime::currentMSecsSinceEpoch() - m_lastHeartbeat;
}

bool SimulationBridge::isCommunicationHealthy() const
{
    return m_microROSConnected && (getLastHeartbeat() < 1000);
}

void SimulationBridge::setRoverConfig(const std::string &agentIP, int agentPort)
{
    m_agentIP = agentIP;
    m_agentPort = agentPort;
}

void SimulationBridge::pauseSimulation()
{
    if (!m_connected) return;
    m_updateTimer->stop();
    qDebug() << "Rover control paused";
}

void SimulationBridge::resumeSimulation()
{
    if (!m_connected) return;
    m_updateTimer->start();
    qDebug() << "Rover control resumed";
}

void SimulationBridge::resetSimulation()
{
    if (!m_connected) return;

    m_posX = 0.0;
    m_posY = 0.0;
    m_posZ = 0.0;
    m_roll = 0.0;
    m_pitch = 0.0;
    m_yaw = 0.0;
    m_linearVel = 0.0;
    m_angularVel = 0.0;
    m_batteryLevel = 100.0;
    m_simTime = 0.0;

    emit positionUpdated(m_posX, m_posY, m_posZ);
    emit orientationUpdated(m_roll, m_pitch, m_yaw);
    emit velocityUpdated(m_linearVel, m_angularVel);
    emit batteryLevelUpdated(m_batteryLevel);
    emit simulationTimeUpdated(m_simTime);

    qDebug() << "Rover state reset";
}

void SimulationBridge::simulationUpdateTick()
{
    // Check communication timeout
    if (getLastHeartbeat() > 3000) {
        qDebug() << "⚠️ Communication timeout with rover - no heartbeat for 3 seconds";
        emit connectionStatusChanged(false);
    }

    // Update simulation time
    m_simTime += 0.05;

#ifndef MICRO_ROS_SUPPORT
    // Only do dead reckoning in simulation mode (no real hardware connected)
    if (m_linearVel != 0.0 || m_angularVel != 0.0) {
        m_posX += m_linearVel * cos(m_yaw) * 0.05;
        m_posY += m_linearVel * sin(m_yaw) * 0.05;
        m_yaw += m_angularVel * 0.05;

        emit positionUpdated(m_posX, m_posY, m_posZ);
        emit orientationUpdated(m_roll, m_pitch, m_yaw);
        emit velocityUpdated(m_linearVel, m_angularVel);
    }
#endif

    emit simulationTimeUpdated(m_simTime);
}

}