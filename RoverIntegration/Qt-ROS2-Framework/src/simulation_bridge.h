#ifndef SIMULATION_BRIDGE_H
#define SIMULATION_BRIDGE_H

#include <QObject>
#include <QVector>
#include <QTimer>
#include <QDateTime>
#include <string>
#include <mutex>

#ifdef MICRO_ROS_SUPPORT
#include <rclcpp/rclcpp.hpp>
#include <std_msgs/msg/u_int16.hpp>
#include <std_msgs/msg/string.hpp>
#include <std_srvs/srv/trigger.hpp>
#include <nav_msgs/msg/odometry.hpp>
#include <geometry_msgs/msg/twist.hpp>
#include <sensor_msgs/msg/battery_state.hpp>
#include <sensor_msgs/msg/compressed_image.hpp>
#include <sensor_msgs/msg/imu.hpp>
#include <sensor_msgs/msg/laser_scan.hpp>
#endif

namespace LunarRoverUI {

class SimulationBridge : public QObject
{
    Q_OBJECT
public:
    explicit SimulationBridge(QObject *parent = nullptr);
    ~SimulationBridge();

    bool connectToRover(const std::string &agentIP = "127.0.0.1", int agentPort = 8888);
    void disconnect();
    bool isConnected() const;
    
    bool connectToMicroROS(const std::string &wifiSSID, const std::string &wifiPassword,
                          const std::string &agentIP, int agentPort = 8888);
    void setMicroROSConfig(const std::string &wifiSSID, const std::string &wifiPassword,
                          const std::string &agentIP, int agentPort = 8888);
    bool isMicroROSConnected() const;
    
    void setRoverConfig(const std::string &agentIP, int agentPort = 8888);
    void emergencyStop();
    
    qint64 getLastHeartbeat() const;
    bool isCommunicationHealthy() const;


    // Asynchronous arbiter service requests.
    void requestManualAuthorization(quint64 requestId);
    void requestDisarm(quint64 requestId);
    void requestStop(quint64 requestId);
    void requestResetStop(quint64 requestId);

public slots:
    void sendVelocityCommand(double linear, double angular);
    void pauseSimulation();
    void resumeSimulation();
    void resetSimulation();

signals:

    // Feedback forwarded to the Qt GUI thread.
    void controlStateReceived(QString stateJson);

    void controlServiceResult(quint64 requestId,
                              QString operation,
                              bool success,
                              QString message);


    void headingReceived(double yaw);
    void imuOrientationReceived(double roll, double pitch);
    void batteryRawReceived(double raw);

    void laserScanReceived(QVector<float> ranges,
                           float angleMin,
                           float angleIncrement,
                           float rangeMin,
                           float rangeMax);

    void connectionStatusChanged(bool connected);
    void positionUpdated(double x, double y, double z);
    void odometryReceived(double x, double y, double z, double linear, double angular);
    void orientationUpdated(double roll, double pitch, double yaw);
    void velocityUpdated(double linear, double angular);
    void commandedVelocityUpdated(double linear, double angular);
    void batteryLevelUpdated(double percentage);
    void cameraFrameReceived(const unsigned char *data, size_t dataSize, int width, int height);
    void simulationTimeUpdated(double timeSeconds);
    void microROSConnectionStatusChanged(bool connected);

private slots:
    void simulationUpdateTick();

private:
#ifdef MICRO_ROS_SUPPORT
    void onOdometryReceived(const nav_msgs::msg::Odometry::SharedPtr msg);
    void onTwistMessage(const geometry_msgs::msg::Twist::SharedPtr msg);
    void onBatteryReceived(const std_msgs::msg::UInt16::SharedPtr msg);
    void onCameraFrameReceived(const sensor_msgs::msg::CompressedImage::SharedPtr msg);
    void onImuReceived(const sensor_msgs::msg::Imu::SharedPtr msg);
#endif

private:
    bool m_connected;
    QTimer *m_updateTimer;

#ifdef MICRO_ROS_SUPPORT
    // ROS2 / Micro-ROS members
    rclcpp::Node::SharedPtr m_node;
    rclcpp::Publisher<geometry_msgs::msg::Twist>::SharedPtr m_twist_pub;
    rclcpp::Subscription<nav_msgs::msg::Odometry>::SharedPtr m_odom_sub;
    rclcpp::Subscription<std_msgs::msg::UInt16>::SharedPtr m_battery_sub;

    // Arbiter feedback and movement authorization services.
    rclcpp::Subscription<std_msgs::msg::String>::SharedPtr m_controlStateSub;

    rclcpp::Client<std_srvs::srv::Trigger>::SharedPtr m_authorizeManualClient;
    rclcpp::Client<std_srvs::srv::Trigger>::SharedPtr m_disarmClient;
    rclcpp::Client<std_srvs::srv::Trigger>::SharedPtr m_stopClient;
    rclcpp::Client<std_srvs::srv::Trigger>::SharedPtr m_resetStopClient;

    rclcpp::Subscription<sensor_msgs::msg::CompressedImage>::SharedPtr m_camera_sub;
    rclcpp::Subscription<sensor_msgs::msg::Imu>::SharedPtr m_imu_sub;
    rclcpp::Subscription<sensor_msgs::msg::LaserScan>::SharedPtr m_scan_sub;
    
    // Generic micro-ROS publishers/subscribers
    rclcpp::Subscription<geometry_msgs::msg::Twist>::SharedPtr m_twist_sub;
    rclcpp::Publisher<nav_msgs::msg::Odometry>::SharedPtr m_odom_pub;
    
    rclcpp::executors::SingleThreadedExecutor::SharedPtr m_executor;
    std::thread m_rosThread;
#endif

    // Connection state
    bool m_microROSConnected;
    std::string m_wifiSSID;
    std::string m_wifiPassword;
    std::string m_agentIP;
    int m_agentPort;
    qint64 m_lastHeartbeat;

    // Simulation state
    double m_simTime;
    double m_posX, m_posY, m_posZ;
    double m_roll, m_pitch, m_yaw;
    double m_linearVel, m_angularVel;
    double m_batteryLevel;
};

}

#endif // SIMULATION_BRIDGE_H
